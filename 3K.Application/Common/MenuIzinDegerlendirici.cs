using _3K.Core.Interfaces;

namespace _3K.Application.Common
{
    /// <summary>
    /// Bir yetki politikasının (<see cref="RequestMenuPermissions"/>) kullanıcı için sağlanıp sağlanmadığı: gruplar AND,
    /// grup içi All/Any (<see cref="Behaviors.AuthorizationBehavior{TRequest,TResponse}"/> ile aynı anlam). Sonucu
    /// birden çok gruba bölünmüş okumalarda (ör. genel arama) her grubun kendi izniyle süzülmesi için kullanılır; aynı
    /// istekte sorulan kodlar tekrar sorgulanmaz.
    /// </summary>
    public sealed class MenuIzinDegerlendirici(IRolService rolService, int kullaniciId)
    {
        private readonly Dictionary<MenuPermissionRequirement, bool> sonuclar = [];

        public async Task<bool> SaglarMiAsync(RequestMenuPermissions politika, CancellationToken cancellationToken)
        {
            if (politika.Groups.Count == 0)
                return false;
            foreach (var grup in politika.Groups)
            {
                if (grup.Requirements.Count == 0)
                    return false;
                var herhangi = false;
                foreach (var gereksinim in grup.Requirements.Distinct())
                {
                    var izinli = await VarMiAsync(gereksinim, cancellationToken);
                    if (!izinli && grup.Match == MenuPermissionMatch.All)
                        return false;
                    herhangi |= izinli;
                    if (izinli && grup.Match == MenuPermissionMatch.Any)
                        break;
                }
                if (!herhangi)
                    return false;
            }
            return true;
        }

        private async Task<bool> VarMiAsync(MenuPermissionRequirement gereksinim, CancellationToken cancellationToken)
        {
            if (!sonuclar.TryGetValue(gereksinim, out var izinli))
            {
                izinli = await rolService.HasUserPermissionAsync(kullaniciId, gereksinim.MenuKod, gereksinim.YetkiTipi, cancellationToken);
                sonuclar[gereksinim] = izinli;
            }
            return izinli;
        }
    }
}
