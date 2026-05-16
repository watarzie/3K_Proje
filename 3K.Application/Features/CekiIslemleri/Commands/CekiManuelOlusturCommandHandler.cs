using MediatR;
using _3K.Application.Common;
using _3K.Application.Features.CekiIslemleri.DTOs;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Application.Features.CekiIslemleri.Commands
{
    public class CekiManuelOlusturCommandHandler : IRequestHandler<CekiManuelOlusturCommand, Result<CekiYuklemeResultDto>>
    {
        private readonly ICekiService _cekiService;

        public CekiManuelOlusturCommandHandler(ICekiService cekiService)
        {
            _cekiService = cekiService;
        }

        public async Task<Result<CekiYuklemeResultDto>> Handle(CekiManuelOlusturCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var ceki = await _cekiService.CekiManuelOlusturAsync(new ManuelCekiOlusturModel
                {
                    ProjeNo = request.ProjeNo,
                    FBNo = request.FBNo,
                    Musteri = request.Musteri,
                    Lokasyon = request.Lokasyon,
                    Guc = request.Guc,
                    Gerilim = request.Gerilim,
                    ProjeMuduru = request.ProjeMuduru,
                    SorumluKisi = request.SorumluKisi,
                    OlcuResmiNo = request.OlcuResmiNo,
                    NakilOlcuResmiNo = request.NakilOlcuResmiNo,
                    SonMontajResmiNo = request.SonMontajResmiNo,
                    PlanlananSevkTarihi = request.PlanlananSevkTarihi,
                    ProjeTipiId = request.ProjeTipiId,
                    Sandiklar = request.Sandiklar.Select(s => new ManuelSandikModel
                    {
                        SandikNo = s.SandikNo,
                        Ad = s.Ad,
                        En = s.En,
                        Boy = s.Boy,
                        Yukseklik = s.Yukseklik,
                        NetKg = s.NetKg,
                        GrossKg = s.GrossKg
                    }).ToList(),
                    Satirlar = request.Satirlar.Select(s => new ManuelCekiSatiriModel
                    {
                        SiraNo = s.SiraNo,
                        BarkodNo = s.BarkodNo,
                        Aciklama = s.Aciklama,
                        SandikNo = s.SandikNo,
                        IstenenAdet = s.IstenenAdet,
                        BirimId = s.BirimId,
                        Birim = s.Birim,
                        Remarks = s.Remarks
                    }).ToList()
                });

                var satirlar = await _cekiService.GetCekiSatirlariAsync(ceki.Id);
                var satirList = satirlar.ToList();
                var benzersizSandikSayisi = satirList.Select(s => s.CekideGecenSandikNo).Distinct().Count();

                return Result<CekiYuklemeResultDto>.Success(new CekiYuklemeResultDto
                {
                    CekiId = ceki.Id,
                    SatirSayisi = satirList.Count,
                    SandikSayisi = benzersizSandikSayisi,
                    Mesaj = $"{satirList.Count} ürün satırı manuel oluşturuldu, {benzersizSandikSayisi} benzersiz sandık oluşturuldu."
                });
            }
            catch (Exception ex)
            {
                return Result<CekiYuklemeResultDto>.Failure(ex.Message);
            }
        }
    }
}
