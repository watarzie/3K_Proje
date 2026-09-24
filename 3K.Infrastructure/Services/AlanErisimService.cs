using _3K.Core.Constants;
using _3K.Core.Enums;
using _3K.Core.Interfaces;
using _3K.Core.Models;

namespace _3K.Infrastructure.Services;

public sealed class AlanErisimService(IRolService rolService, ICurrentUserService currentUser) : IAlanErisimService
{
    public async Task<AlanErisimYetkileri> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.IslemKullaniciId is not int userId)
            return AlanErisimYetkileri.Yok;
        // Aynı DbContext üzerinde sorgular sıralıdır; oturumlar arası yetki cache'i yoktur.
        async Task<bool> Read(string code) => await rolService.HasUserPermissionAsync(userId, code, YetkiTipi.R, cancellationToken);
        var olcu = await Read(YetkiKodlari.Ambalaj.OlcuGoruntule);
        var m3 = await Read(YetkiKodlari.Ambalaj.M3Goruntule);
        var sarf = m3 && await Read(YetkiKodlari.Ambalaj.SarfGoruntule);
        var para = await Read(YetkiKodlari.Finans.ParasalVeriGoruntule);
        return new(olcu, m3, sarf, para,
            para && await Read(YetkiKodlari.Finans.BirimFiyatGoruntule),
            para && await Read(YetkiKodlari.Finans.TutarGoruntule),
            para && await Read(YetkiKodlari.Finans.GelirGoruntule),
            para && await Read(YetkiKodlari.Finans.GiderGoruntule),
            para && await Read(YetkiKodlari.Finans.KarlilikGoruntule));
    }
}
