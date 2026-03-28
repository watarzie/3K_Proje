using MediatR;

namespace 3K.Application.Features.SandikIslemleri.Commands
{
    // İşlem sonucunda geriye bool (başarılı/başarısız) döneceğini belirtiyoruz.
    public class FiiliSandikDegistirCommand : IRequest<bool>
{
    public int CekiSatiriId { get; set; }
    public string YeniFiiliSandikNo { get; set; }
    public int ProjeId { get; set; }
    public int KullaniciId { get; set; }
}
}