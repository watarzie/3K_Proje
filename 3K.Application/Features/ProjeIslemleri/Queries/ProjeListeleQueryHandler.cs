using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.ProjeIslemleri.DTOs;
using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Features.ProjeIslemleri.Queries
{
    public class ProjeListeleQueryHandler : IRequestHandler<ProjeListeleQuery, Result<IEnumerable<ProjeDto>>>
    {
        private readonly IProjeRepository _projeRepository;

        public ProjeListeleQueryHandler(IProjeRepository projeRepository)
        {
            _projeRepository = projeRepository;
        }

        public async Task<Result<IEnumerable<ProjeDto>>> Handle(ProjeListeleQuery request, CancellationToken cancellationToken)
        {
            var projeler = await _projeRepository.GetAllWithDetailsAsync(cancellationToken);

            var result = projeler.Select(p =>
            {
                var sandiklar = p.Sandiklar ?? new List<Sandik>();
                var cekiSatirlari = p.Cekiler?.SelectMany(c => c.CekiSatirlari).ToList()
                                    ?? new List<CekiSatiri>();

                var toplamSandik = sandiklar.Count;
                var hazirSandik = sandiklar.Count(s => s.Durum == StatusConstants.SandikDurum.Hazir);
                var toplamUrun = cekiSatirlari.Count;
                // Tamamlanan ürün: Grid veya 3K'da işlem görmüş
                var tamamlananUrun = cekiSatirlari.Count(cs =>
                    cs.GridDurumu != StatusConstants.UrunDurum.Bekliyor || cs.UcKKarsilamaTipi != StatusConstants.UrunDurum.Bekliyor);

                // Durum hesaplama
                string durum;
                if (toplamUrun == 0)
                    durum = StatusConstants.ProjeDurum.Hazirlaniyor;
                else if (hazirSandik == toplamSandik && toplamSandik > 0)
                    durum = StatusConstants.ProjeDurum.Tamamlandi;
                else if (tamamlananUrun > 0)
                    durum = StatusConstants.ProjeDurum.Devam;
                else
                    durum = p.Durum;

                return new ProjeDto
                {
                    Id = p.Id,
                    ProjeNo = p.ProjeNo,
                    Musteri = p.Musteri,
                    Durum = durum,
                    PlanlananSevkTarihi = p.PlanlananSevkTarihi,
                    SorumluKisi = p.SorumluKisi,
                    SandikSayisi = toplamSandik,
                    HazirSandikSayisi = hazirSandik,
                    ToplamUrunSayisi = toplamUrun,
                    TamamlananUrunSayisi = tamamlananUrun,
                    FBNo = p.FBNo,
                    Guc = p.Guc,
                    Gerilim = p.Gerilim,
                    Lokasyon = p.Lokasyon,
                    OlcuResmiNo = p.OlcuResmiNo,
                    NakilOlcuResmiNo = p.NakilOlcuResmiNo,
                    SonMontajResmiNo = p.SonMontajResmiNo,
                    ProjeMuduru = p.ProjeMuduru
                };
            });

            return Result<IEnumerable<ProjeDto>>.Success(result);
        }
    }
}
