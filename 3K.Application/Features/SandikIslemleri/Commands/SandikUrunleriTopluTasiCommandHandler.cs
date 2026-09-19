using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.SandikIslemleri.Services;
using _3K.Application.Features.SandikIslemleri.Validators;
using _3K.Core.Entities;
using _3K.Core.Exceptions;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.SandikIslemleri.Commands;

public sealed class SandikUrunleriTopluTasiCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ISahaTamamlamaService sahaTamamlamaService) : IRequestHandler<SandikUrunleriTopluTasiCommand, Result>
{
    private const string IslemAnahtariIndex = "IX_SandikTopluTasimaIslemleri_IslemAnahtari";

    public async Task<Result> Handle(SandikUrunleriTopluTasiCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not int kullaniciId || kullaniciId <= 0)
            return Result.Failure("Taşıma işlemi için geçerli bir kullanıcı oturumu bulunamadı.", 401);

        var dogrulama = await new SandikUrunleriTopluTasiCommandValidator().ValidateAsync(request, cancellationToken);
        if (!dogrulama.IsValid)
            return Result.Failure(string.Join(" ", dogrulama.Errors.Select(e => e.ErrorMessage).Distinct()));

        var hash = IstekHashOlustur(request);
        var islemRepo = unitOfWork.GetRepository<SandikTopluTasimaIslemi>();
        var tekrar = await TekrariDogrulaAsync();
        if (tekrar != null)
            return tekrar;

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var transactionTekrari = await TekrariDogrulaAsync();
                if (transactionTekrari != null)
                    return transactionTekrari;
                var icerikRepo = unitOfWork.GetRepository<SandikIcerik>();
                var tasima = new SandikUrunTasimaIslemi(unitOfWork, sahaTamamlamaService);
                var seciliCekiSatirlari = new HashSet<int>();
                // Kimlik kontrolü ve tüm satır hareketleri aynı serializable transaction içindedir.
                // Ayrı içerikleri barkod veya ürün adına göre gruplamayız.
                foreach (var satir in request.Satirlar)
                {
                    var kaynak = await icerikRepo.GetByIdAsync(satir.KaynakSandikIcerikId);
                    if (kaynak == null || kaynak.SandikId != request.KaynakSandikId)
                        throw SatirHatasi(satir.KaynakSandikIcerikId,
                            Result.Failure("İçerik seçilen kaynak sandığa ait değil veya bulunamadı.", 409));
                    if (kaynak.CekiSatiriId is int cekiSatiriId && !seciliCekiSatirlari.Add(cekiSatiriId))
                        throw SatirHatasi(satir.KaynakSandikIcerikId, Result.Failure(
                            "Kaynak sandıkta aynı çeki satırına ait birden fazla içerik var. Ayrı kayıtlar birleştirilemez; belirsiz tahsis önce düzeltilmelidir.", 409));
                }

                await islemRepo.AddAsync(new SandikTopluTasimaIslemi
                {
                    IslemAnahtari = request.IslemAnahtari,
                    IstekHash = hash,
                    KullaniciId = kullaniciId,
                    ProjeId = request.ProjeId,
                    KaynakSandikId = request.KaynakSandikId,
                    HedefSandikId = request.HedefSandikId,
                    SatirSayisi = request.Satirlar.Count
                });
                // Aynı anahtarlı eşzamanlı istek miktar değiştirmeden önce benzersiz kayıtta yarışır.
                await unitOfWork.SaveChangesAsync(ct);

                foreach (var satir in request.Satirlar.OrderBy(s => s.KaynakSandikIcerikId))
                {
                    ct.ThrowIfCancellationRequested();
                    var sonuc = await tasima.TasiAsync(new SandikUrunTasiCommand
                    {
                        ProjeId = request.ProjeId,
                        KaynakSandikIcerikId = satir.KaynakSandikIcerikId,
                        HedefSandikId = request.HedefSandikId,
                        TasinanAdet = satir.TasinanAdet,
                        IslemAnahtari = SatirAnahtariOlustur(request.IslemAnahtari, satir.KaynakSandikIcerikId)
                    }, kullaniciId, ct);
                    if (!sonuc.IsSuccess)
                        throw SatirHatasi(satir.KaynakSandikIcerikId, sonuc);
                }

                return Result.Success();
            }, cancellationToken);
        }
        catch (SandikTasimaReddedildiException ex)
        {
            return ex.Sonuc;
        }
        catch (ConcurrencyConflictException)
        {
            return await TekrariDogrulaAsync() ?? Result.Failure(
                "Taşıma sırasında miktar veya sandık başka bir işlem tarafından değiştirildi. Hiçbir satır taşınmadı; güncel veriyi yükleyip tekrar deneyin.", 409);
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == IslemAnahtariIndex)
        {
            return await TekrariDogrulaAsync() ?? Result.Failure("Toplu taşıma eşzamanlı bir istekle çakıştı. Tekrar deneyin.", 409);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure("Taşıma sırasında çakışan bir tahsis oluştu. Hiçbir satır taşınmadı; güncel veriyi yükleyip tekrar deneyin.", 409);
        }

        async Task<Result?> TekrariDogrulaAsync()
        {
            var onceki = (await islemRepo.FindAsync(x => x.IslemAnahtari == request.IslemAnahtari)).SingleOrDefault();
            if (onceki == null)
                return null;
            return onceki.KullaniciId == kullaniciId && string.Equals(onceki.IstekHash, hash, StringComparison.Ordinal)
                ? Result.Success()
                : Result.Failure("İşlem anahtarı başka bir kullanıcı veya farklı bir toplu taşıma için kullanılmış.", 409);
        }
    }

    private static SandikTasimaReddedildiException SatirHatasi(int id, Result sonuc) =>
        new(Result.Failure($"İçerik #{id}: {sonuc.Error!.Message} Toplu taşıma uygulanmadı.", sonuc.StatusCode,
            new[] { new { SandikIcerikId = id, Mesaj = sonuc.Error.Message } }));

    private static string IstekHashOlustur(SandikUrunleriTopluTasiCommand request)
    {
        var metin = FormattableString.Invariant($"v1|{request.ProjeId}|{request.KaynakSandikId}|{request.HedefSandikId}|") +
            string.Join(";", request.Satirlar.OrderBy(s => s.KaynakSandikIcerikId)
                .Select(s => s.KaynakSandikIcerikId.ToString(CultureInfo.InvariantCulture) + ":" +
                    s.TasinanAdet.ToString("G29", CultureInfo.InvariantCulture)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(metin)));
    }

    private static Guid SatirAnahtariOlustur(Guid islemAnahtari, int icerikId)
    {
        var metin = FormattableString.Invariant($"sandik-toplu-v1:{islemAnahtari:D}:{icerikId}");
        return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(metin)).AsSpan(0, 16));
    }
}
