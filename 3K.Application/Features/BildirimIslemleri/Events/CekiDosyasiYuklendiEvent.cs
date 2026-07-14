using MediatR;
using _3K.Core.Enums;

namespace _3K.Application.Features.BildirimIslemleri.Events
{
    public sealed record CekiDosyasiYuklendiEvent(
        int CekiId,
        int ProjeId,
        string ProjeNo,
        string DosyaAdi,
        int YukleyenKullaniciId,
        bool RevizyonMu,
        int SatirSayisi,
        int SandikSayisi,
        int EklenenSatirSayisi = 0,
        int GuncellenenSatirSayisi = 0,
        int SilinenSatirSayisi = 0,
        int ProjeTipiId = (int)ProjeTipi.Normal) : INotification;
}
