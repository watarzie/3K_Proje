using _3K.Core.Entities;
using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    public static class SahaAktarimBlokajHelper
    {
        public const string GridMesaji = "Bu ürün sahaya aktarıldığı için normal proje üzerinden Grid işlemi yapılamaz. İşlem saha projesinde yürütülmelidir.";
        public const string UcKMesaji = "Bu ürün sahaya aktarıldığı için normal proje üzerinden 3K işlemi yapılamaz. İşlem saha projesinde yürütülmelidir.";
        public const string SandikMesaji = "Bu ürün sahaya aktarıldığı için normal proje sandığı üzerinden işlem yapılamaz. İşlem saha projesinde yürütülmelidir.";

        public static async Task<bool> KaynakSatirAktarildiMiAsync(
            ISahaTamamlamaService sahaTamamlamaService,
            CekiSatiri satir,
            CancellationToken cancellationToken = default)
        {
            return !satir.KaynakCekiSatiriId.HasValue &&
                await sahaTamamlamaService.AktifTamamlamaVarMiAsync(satir.Id, cancellationToken);
        }

        public static async Task<HashSet<int>> GetAktarilanKaynakSatirIdleriAsync(
            ISahaTamamlamaService sahaTamamlamaService,
            IEnumerable<CekiSatiri> satirlar,
            CancellationToken cancellationToken = default)
        {
            var kaynakSatirIds = satirlar
                .Where(s => !s.KaynakCekiSatiriId.HasValue)
                .Select(s => s.Id)
                .Distinct()
                .ToList();

            if (kaynakSatirIds.Count == 0)
                return new HashSet<int>();

            var map = await sahaTamamlamaService.GetAktifTamamlamaMapAsync(kaynakSatirIds, cancellationToken);
            return map.Keys.ToHashSet();
        }
    }
}
