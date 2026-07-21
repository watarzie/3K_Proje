using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;

namespace _3K.Application.Features.SandikIslemleri.Commands
{
    /// <summary>
    /// Yalnızca sunucu tarafından oluşturulan ve mevcut onay motoru tarafından
    /// saklanıp çalıştırılan iç komuttur. Dışarı açık bir HTTP endpoint'i yoktur.
    /// </summary>
    public sealed class SandikLokasyonOnayliUygulaCommand
        : IRequest<Result<bool>>, IConfigurableApproval, IApprovalReference
    {
        public int ProjeId { get; set; }
        public string ProjeNo { get; set; } = string.Empty;
        public int DepoLokasyonId { get; set; }
        public string DepoLokasyonAdi { get; set; } = string.Empty;
        public List<SandikLokasyonDegisiklikKalemi> Kalemler { get; set; } = new();

        public string GetApprovalOperationCode() => OnayIslemKodlari.SandikLokasyonGuncelle;

        public ApprovalReference GetApprovalReference() => new(
            OnayReferansTipleri.Proje,
            ProjeId,
            ProjeId,
            "/onay-merkezi");

        public string GetApprovalDescription()
        {
            var proje = string.IsNullOrWhiteSpace(ProjeNo)
                ? $"#{ProjeId}"
                : ProjeNo.Trim();
            var hedefLokasyon = string.IsNullOrWhiteSpace(DepoLokasyonAdi)
                ? $"Lokasyon #{DepoLokasyonId}"
                : DepoLokasyonAdi.Trim();

            var siraliKalemler = (Kalemler ?? new List<SandikLokasyonDegisiklikKalemi>())
                .OrderBy(kalem => kalem.SandikNo, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var gorunenDetaylar = siraliKalemler
                .Take(8)
                .Select(kalem =>
                {
                    var sandikNo = string.IsNullOrWhiteSpace(kalem.SandikNo)
                        ? $"#{kalem.SandikId}"
                        : kalem.SandikNo.Trim();
                    var eskiLokasyon = string.IsNullOrWhiteSpace(kalem.BeklenenDepoLokasyonAdi)
                        ? $"Lokasyon #{kalem.BeklenenDepoLokasyonId}"
                        : kalem.BeklenenDepoLokasyonAdi.Trim();

                    return $"Sandık {sandikNo}: {eskiLokasyon} → {hedefLokasyon}";
                });

            var kalanSayisi = Math.Max(0, siraliKalemler.Count - 8);
            var kalanBilgisi = kalanSayisi > 0
                ? $"; ayrıca {kalanSayisi} sandık daha"
                : string.Empty;
            var detay = string.Join("; ", gorunenDetaylar);

            return $"{proje} projesinde {siraliKalemler.Count} sandık için manuel lokasyon atama talebi. {detay}{kalanBilgisi}.";
        }
    }

    public sealed class SandikLokasyonDegisiklikKalemi
    {
        public int SandikId { get; set; }
        public string SandikNo { get; set; } = string.Empty;
        public int BeklenenDepoLokasyonId { get; set; }
        public string BeklenenDepoLokasyonAdi { get; set; } = string.Empty;
    }
}
