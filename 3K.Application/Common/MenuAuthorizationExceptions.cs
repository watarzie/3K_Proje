using _3K.Application.Features.AuthIslemleri.Commands;
using _3K.Application.Features.BildirimIslemleri.Commands;
using _3K.Application.Features.BildirimIslemleri.Queries;
using _3K.Application.Features.CekiIslemleri.Commands;
using _3K.Application.Features.FinansIslemleri.Commands;
using _3K.Application.Features.LookupIslemleri.Queries;
using _3K.Application.Features.OnayIslemleri.Queries;
using _3K.Application.Features.RolIslemleri.Queries;
using _3K.Application.Features.SandikIslemleri.Commands;

namespace _3K.Application.Common;

/// <summary>
/// Menü izni gerektirmeyen yolların kapalı listesi. ISecuredRequest eklemeyi
/// unutmak yeni bir operasyonu public yapmaz. Kişisel yollarda handler'ın kayıt
/// sahipliği kontrolü, iç komutlarda çağıran iş/onay bağlamı korunur.
/// </summary>
public static class MenuAuthorizationExceptions
{
    public static bool IsPublic(object request) => request is
        LoginCommand or IkiFaktorKurulumBaslatCommand or IkiFaktorKurulumDogrulaCommand or
        IkiFaktorGirisDogrulaCommand or IkiFaktorKurtarmaKoduDogrulaCommand or GetLookupsQuery;

    public static bool IsOwnAuthenticatedData(object request) => request is
        GetKullaniciMenuQuery or GetBildirimlerQuery or GetBildirimDetayiQuery or
        GetOkunmamisBildirimlerQuery or BildirimiOkunduIsaretleCommand or TumBildirimleriOkunduIsaretleCommand or
        GetOnayGecmisiQuery or GetOnayGecmisiDetayiQuery;

    // Bu komutların HTTP endpoint'i yoktur. İlk iki komut ApprovalBehavior'dan
    // geçerek doğrudan veya onay sonrası uygulanır; finans aktarımı iç üretim akışıdır.
    public static bool IsServerInternal(object request) => request is
        CekiRevizyonOnayliUygulaCommand or SandikLokasyonOnayliUygulaCommand or FinansUretimAktarCommand;
}
