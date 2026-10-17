using MediatR;
using _3K.Application.Common;
using _3K.Core.Constants;
using _3K.Core.Models;

namespace _3K.Application.Features.FinansIslemleri.Commands;

/// <summary>
/// PO öncesi iş onayına gönderme: seçilen işlerden uygun kalemler grup (proje × iş türü × para birimi) başına bir
/// talep olur. Kural açıksa talepler Onay Merkezi'ne düşer (202), kapalıysa kalemler hemen onaylanır. PO açma
/// sekmelerinin yazma iznini ister; yeni izin kodu yoktur.
/// </summary>
public sealed class FinansPoOnayGonderCommand : FinansCommand<FinansPoOnayGonderSonucu>
{
    public override IReadOnlyList<string> Sekmeler => FinansSekmeleri.SiparisAcma;
    public FinansPoOnayGonderModel Model { get; init; } = null!;
}

/// <summary>
/// Yalnız onay motorunun sakladığı ve çalıştırdığı iç komut; HTTP ucu yoktur. Kalemlerin kendisi yerine değişmez onay
/// talebinin kimliğini ve görüntü özetini taşır; onaylanınca talep kalemlere uygulanır.
/// </summary>
public sealed class FinansPoOnayUygulaCommand
    : IRequest<Result<FinansPoOnayUygulamaSonucu>>,
      IConfigurableApproval,
      IApprovalReference
{
    public int TalepId { get; set; }
    public int? ProjeId { get; set; }
    public string ProjeNo { get; set; } = string.Empty;
    /// <summary>Onay listesinde gösterilen özet: kategori, kalem sayısı ve net tutar.</summary>
    public string Ozet { get; set; } = string.Empty;
    public string OnizlemeHash { get; set; } = string.Empty;

    public string GetApprovalDescription() => $"{ProjeNo} · {Ozet}: PO öncesi iş onayı bekliyor.";

    public string GetApprovalOperationCode() => OnayIslemKodlari.FinansPoIsOnayi;

    public ApprovalReference GetApprovalReference() => new(
        OnayReferansTipleri.FinansPoOnayTalebi,
        TalepId,
        ProjeId,
        "/onay-merkezi");
}
