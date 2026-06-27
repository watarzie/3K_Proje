using MediatR;
using _3K.Core.Enums;
using _3K.Application.Common;
using _3K.Core.Entities;
using _3K.Core.Interfaces;
using _3K.Core.Helpers;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    public class SahaYedekMalzemeEkleCommandHandler : IRequestHandler<SahaYedekMalzemeEkleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHareketService _hareketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISahaTamamlamaService _sahaTamamlamaService;

        public SahaYedekMalzemeEkleCommandHandler(
            IUnitOfWork unitOfWork,
            IHareketService hareketService,
            ICurrentUserService currentUserService,
            ISahaTamamlamaService sahaTamamlamaService)
        {
            _unitOfWork = unitOfWork;
            _hareketService = hareketService;
            _currentUserService = currentUserService;
            _sahaTamamlamaService = sahaTamamlamaService;
        }

        public async Task<Result> Handle(SahaYedekMalzemeEkleCommand request, CancellationToken cancellationToken)
        {
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();

            var sandik = await sandikRepo.GetByIdAsync(request.SandikId);
            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result.Failure("Proje bulunamadı.", 404);
            if (sandik == null || sandik.ProjeId != request.ProjeId)
                return Result.Failure("Sandık bulunamadı veya projeye ait değil.", 404);

            // Sandık "Sevk Edildi" durumundaysa ekleme yapılamaz
            if (SandikSevkKilidiHelper.SandikKilitliMi(sandik))
                return Result.Failure("Sevk edilmiş sandığa malzeme eklenemez.");

            if (request.CekiSatiriId.HasValue && request.CekiSatiriId.Value > 0)
            {
                var tamamlamaResult = await EksikTamamlamaSatiriEkleAsync(request, sandik, cancellationToken);
                if (tamamlamaResult != null)
                    return tamamlamaResult;
            }

            var icerik = new SandikIcerik
            {
                SandikId = sandik.Id,
                CekiSatiriId = null,
                BarkodNo = request.BarkodNo,
                Isim = request.Isim,
                Miktar = request.Miktar,
                BirimId = request.BirimId,
                KonulanAdet = request.Miktar,
                EksikAdet = 0,
                KaynakProjeNo = request.KaynakProjeNo,
                Aciklama = request.Aciklama
            };

            await icerikRepo.AddAsync(icerik);

            // Sandık durumu Boş ise Hazırlanıyor'a geçir
            if (sandik.DurumId == (int)SandikDurum.Bos)
            {
                sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                sandikRepo.Update(sandik);
            }

            await _unitOfWork.SaveChangesAsync();

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "SandikIcerik",
                ReferansId = icerik.Id.ToString(),
                Islem = "Saha/Yedek Malzeme Eklendi",
                IslemTipiId = (int)IslemTipi.SahaYedekMalzemeEklendi,
                Aciklama = $"'{request.Isim}' — {request.Miktar} {(request.BirimId.HasValue ? ((Birim)request.BirimId.Value).ToString() : "Adet")} — Sandık {sandik.SandikNo}"
            });

            return Result.Success();
        }

        private async Task<Result?> EksikTamamlamaSatiriEkleAsync(
            SahaYedekMalzemeEkleCommand request,
            Sandik sandik,
            CancellationToken cancellationToken)
        {
            var cekiSatiriRepo = _unitOfWork.GetRepository<CekiSatiri>();
            var cekiRepo = _unitOfWork.GetRepository<Ceki>();
            var projeRepo = _unitOfWork.GetRepository<Proje>();
            var sandikRepo = _unitOfWork.GetRepository<Sandik>();
            var icerikRepo = _unitOfWork.GetRepository<SandikIcerik>();
            var sahaAktarimRepo = _unitOfWork.GetRepository<SahaAktarim>();
            var sahaAktarimKalemiRepo = _unitOfWork.GetRepository<SahaAktarimKalemi>();

            var kaynakSatir = await cekiSatiriRepo.GetByIdAsync(request.CekiSatiriId!.Value);
            if (kaynakSatir == null)
                return Result.Failure("Kaynak çeki satırı bulunamadı.", 404);

            var kaynakCeki = await cekiRepo.GetByIdAsync(kaynakSatir.CekiId);
            if (kaynakCeki == null)
                return Result.Failure("Kaynak çeki bulunamadı.", 404);

            var proje = await projeRepo.GetByIdAsync(request.ProjeId);
            if (proje == null)
                return Result.Failure("Proje bulunamadı.", 404);

            var hedefNormalProjeMi = proje.ProjeTipiId == (int)ProjeTipi.Normal && kaynakCeki.ProjeId == request.ProjeId;
            var hedefSahaProjesiMi = proje.ProjeTipiId == (int)ProjeTipi.Saha && kaynakCeki.ProjeId != request.ProjeId;

            if (!hedefNormalProjeMi && !hedefSahaProjesiMi)
                return null;

            if (kaynakSatir.KaynakCekiSatiriId.HasValue)
                return Result.Failure("Eksik tamamlama satırından tekrar tamamlama oluşturulamaz.");

            var kaynakProje = await projeRepo.GetByIdAsync(kaynakCeki.ProjeId);
            if (kaynakProje == null)
                return Result.Failure("Kaynak proje bulunamadı.", 404);

            if (hedefSahaProjesiMi && kaynakProje.ProjeTipiId != (int)ProjeTipi.Normal)
                return Result.Failure("Saha projesine yalnızca normal proje eksikleri eklenebilir.");

            if (request.Miktar <= 0)
                return Result.Failure("Tamamlama adedi 0'dan büyük olmalıdır.");

            var mevcutTamamlamaSatirlari = (await cekiSatiriRepo.FindAsync(cs => cs.KaynakCekiSatiriId == kaynakSatir.Id)).ToList();
            var planlananTamamlamaAdedi = mevcutTamamlamaSatirlari.Sum(cs => cs.IstenenAdet);
            var kalanPlanlanabilirAdet = Math.Max(kaynakSatir.KalanMiktar - planlananTamamlamaAdedi, 0);

            if (kalanPlanlanabilirAdet <= 0)
                return Result.Failure("Bu ürün için eksik tamamlama adedi zaten planlanmış.");

            if (request.Miktar > kalanPlanlanabilirAdet)
                return Result.Failure($"Tamamlama adedi kalan planlanabilir adetten büyük olamaz. Kalan: {FormatAdet(kalanPlanlanabilirAdet)}");

            var tamamlamaCekileri = (await cekiRepo.FindAsync(c =>
                    c.ProjeId == request.ProjeId &&
                    c.CekiTipiId == (int)CekiTipi.EksikTamamlama))
                .OrderByDescending(c => c.TamamlamaNo ?? 0)
                .ThenByDescending(c => c.Id)
                .ToList();

            var tamamlamaCeki = tamamlamaCekileri.FirstOrDefault();
            if (tamamlamaCeki == null)
            {
                tamamlamaCeki = new Ceki
                {
                    ProjeId = request.ProjeId,
                    OrijinalDosyaYolu = string.Empty,
                    YuklemeTarihi = TurkeyTime.Now,
                    CekiTipiId = (int)CekiTipi.EksikTamamlama,
                    KaynakCekiId = hedefSahaProjesiMi ? null : kaynakCeki.Id,
                    TamamlamaNo = 1,
                    Aciklama = hedefSahaProjesiMi
                        ? $"Saha eksik tamamlama çekisi - Kaynak: {kaynakProje.ProjeNo}"
                        : "Eksik tamamlama çekisi"
                };

                await cekiRepo.AddAsync(tamamlamaCeki);
                await _unitOfWork.SaveChangesAsync();
            }

            var projeSatirlari = await cekiSatiriRepo.FindAsync(cs => cs.Ceki.ProjeId == request.ProjeId);
            var yeniSiraNo = projeSatirlari.Any() ? projeSatirlari.Max(cs => cs.SiraNo) + 1 : 1;

            var yeniSatir = new CekiSatiri
            {
                CekiId = tamamlamaCeki.Id,
                KaynakCekiSatiriId = kaynakSatir.Id,
                SiraNo = yeniSiraNo,
                OlcuResmiPozNo = kaynakSatir.OlcuResmiPozNo,
                BarkodNo = kaynakSatir.BarkodNo,
                Aciklama = kaynakSatir.Aciklama,
                IstenenAdet = request.Miktar,
                BirimId = kaynakSatir.BirimId,
                CekideGecenSandikNo = sandik.SandikNo,
                FiiliSandikNo = sandik.SandikNo,
                Remarks = kaynakSatir.Remarks,
                DurumId = (int)UrunDurum.Bekliyor,
                GridDurumuId = (int)GridDurum.Gelmedi,
                GridSevkDurumuId = (int)GridSevkDurum.SevkEdilmedi,
                UcKDurumuId = (int)UcKDurum.Bekliyor,
                UcKKarsilamaTipiId = (int)UcKDurum.Bekliyor,
                KaynakHedefProjeNo = kaynakProje.ProjeNo,
                IsManuelEklenen = true,
                EklemeNedeni = hedefSahaProjesiMi ? "Eksik saha tamamlama" : "Eksik tamamlama",
                UcKAciklama = request.Aciklama
            };

            await cekiSatiriRepo.AddAsync(yeniSatir);
            await _unitOfWork.SaveChangesAsync();

            var yeniIcerik = new SandikIcerik
            {
                SandikId = sandik.Id,
                CekiSatiriId = yeniSatir.Id,
                KonulanAdet = hedefSahaProjesiMi ? 0 : request.Miktar,
                EksikAdet = 0,
                KaynakProjeNo = kaynakProje.ProjeNo,
                Aciklama = request.Aciklama
            };

            await icerikRepo.AddAsync(yeniIcerik);

            if (hedefSahaProjesiMi)
            {
                var kaynakSandikId = (await icerikRepo.FindAsync(si => si.CekiSatiriId == kaynakSatir.Id))
                    .Select(si => (int?)si.SandikId)
                    .FirstOrDefault();

                var sahaAktarim = new SahaAktarim
                {
                    SahaProjeId = request.ProjeId,
                    KaynakProjeId = kaynakProje.Id,
                    KullaniciId = _currentUserService.UserId ?? 0,
                    Aciklama = request.Aciklama
                };

                await sahaAktarimRepo.AddAsync(sahaAktarim);
                await _unitOfWork.SaveChangesAsync();

                await sahaAktarimKalemiRepo.AddAsync(new SahaAktarimKalemi
                {
                    SahaAktarimId = sahaAktarim.Id,
                    KaynakProjeId = kaynakProje.Id,
                    SahaProjeId = request.ProjeId,
                    KaynakCekiSatiriId = kaynakSatir.Id,
                    SahaCekiSatiriId = yeniSatir.Id,
                    KaynakSandikId = kaynakSandikId,
                    SahaSandikId = sandik.Id,
                    Miktar = request.Miktar,
                    AktarimTipiId = (int)SahaAktarimTipi.UrunBazli,
                    DurumId = (int)SahaAktarimDurum.Planlandi,
                    Aciklama = request.Aciklama
                });
            }

            if (sandik.DurumId == (int)SandikDurum.Bos || sandik.DurumId == (int)SandikDurum.Kapandi)
            {
                sandik.DurumId = (int)SandikDurum.Hazirlaniyor;
                sandikRepo.Update(sandik);
            }

            await SandikLokasyonHelper.VarsayilanUcKDepoLokasyonuAtaAsync(_unitOfWork, new[] { yeniIcerik });
            await _unitOfWork.SaveChangesAsync();

            if (hedefSahaProjesiMi)
                await _sahaTamamlamaService.SenkronizeKaynakProjelerAsync(new[] { kaynakSatir.Id }, cancellationToken);

            await _hareketService.HareketKaydetAsync(new HareketGecmisi
            {
                ProjeId = request.ProjeId,
                KullaniciId = _currentUserService.UserId ?? 0,
                ReferansTipi = "CekiSatiri",
                ReferansId = yeniSatir.Id.ToString(),
                Islem = "Eksik Tamamlama Satırı Oluşturuldu",
                IslemTipiId = (int)IslemTipi.SahaYedekMalzemeEklendi,
                EskiDeger = kaynakSatir.Id.ToString(),
                YeniDeger = yeniSatir.Id.ToString(),
                Aciklama = $"{FormatAdet(request.Miktar)} adet '{kaynakSatir.Aciklama}', Sandık {sandik.SandikNo} için eksik tamamlama satırı olarak oluşturuldu."
            });

            return Result.Success();
        }

        private static string FormatAdet(decimal value)
        {
            return decimal.Truncate(value) == value
                ? decimal.Truncate(value).ToString("0")
                : value.ToString("0.####");
        }
    }
}
