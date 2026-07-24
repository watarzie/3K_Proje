using MediatR;
using _3K.Application.Common;
using _3K.Core.Enums;

namespace _3K.Application.Features.PdfIslemleri.Queries
{
    public class GetEksikUrunlerPdfQuery
        : IRequest<Result<byte[]>>, ISecuredRequest, IRequiresMenuPermission
    {
        public string RequiredMenuKod => EksikUrunlerRaporYetkisi.GetMenuKod(ProjeTipi);

        public int ProjeId { get; set; }

        public ProjeTipi ProjeTipi { get; set; }
    }

    internal static class EksikUrunlerRaporYetkisi
    {
        private const string GecersizProjeTipiMenuKodu = "__gecersiz-eksik-raporu-proje-tipi__";

        public static string GetMenuKod(ProjeTipi projeTipi)
        {
            return projeTipi switch
            {
                ProjeTipi.Normal => "eksik-raporu",
                ProjeTipi.Saha => "saha-sevk-sonrasi-eksik-raporu",
                ProjeTipi.Yedek => "yedek-eksik-raporu",
                _ => GecersizProjeTipiMenuKodu
            };
        }

        public static bool GecerliMi(ProjeTipi projeTipi)
        {
            return projeTipi is ProjeTipi.Normal or ProjeTipi.Saha or ProjeTipi.Yedek;
        }
    }
}
