using _3K.Core.Models;

namespace _3K.Core.Interfaces;

/// <summary>
/// PO öncesi iş onayı (<see cref="FinansPoOnayKurali"/>): onay talebi oluşturma, onaylanan talebi kalemlere uygulama
/// ve onaycıya gösterilen talep detayı. Karar Onay Merkezi'nin genel onay hattında verilir.
/// </summary>
public interface IFinansPoOnayService
{
    /// <summary>
    /// Uygun kalemleri grup başına bir talebe toplar, görüntüsünü saklar ve kalemleri talebe bağlar. Kalemler bu
    /// adımda onaylanmaz; onay <see cref="OnayiUygulaAsync"/> ile uygulanır.
    /// </summary>
    Task<FinansPoOnayGonderSonucu> TalepleriOlusturAsync(FinansPoOnayGonderModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Onaylanan talebi kalemlere uygular. Kalem talepten sonra değiştiyse, iptal edildiyse veya başka talebe
    /// geçtiyse hiçbir kalem onaylanmaz (409).
    /// </summary>
    Task<FinansPoOnayUygulamaSonucu> OnayiUygulaAsync(int talepId, string onizlemeHash, CancellationToken cancellationToken);

    /// <summary>
    /// Onaycının göreceği talep detayı. Talep, onay kaydındaki talep eden ve projeyle eşleşmiyorsa veya görüntü
    /// bütünlüğü doğrulanamazsa null.
    /// </summary>
    Task<FinansPoOnayDetayModel?> TalepDetayiAsync(
        int talepId,
        int talepEdenKullaniciId,
        int? projeId,
        CancellationToken cancellationToken);
}
