namespace _3K.Application.Common
{
    public interface IRequireApproval
    {
        string GetApprovalDescription();
        int GetApprovalLookupUcKDurumId();
    }

    public interface IAlwaysRequireApproval
    {
        string GetApprovalDescription();
    }

    /// <summary>
    /// Onay zorunluluğu operasyon bazında yönetilebilen komutları işaretler.
    /// Operasyon için kalıcı bir kural bulunamazsa güvenli varsayılan olarak
    /// onay zorunlu kabul edilir.
    /// </summary>
    public interface IConfigurableApproval : IApprovalOperation
    {
        string GetApprovalDescription();
    }

    public interface IApprovalOperation
    {
        string GetApprovalOperationCode();
    }

    public interface IApprovalReference
    {
        ApprovalReference GetApprovalReference();
    }

    public sealed record ApprovalReference(
        string ReferansTipi,
        int ReferansId,
        int? ProjeId,
        string? HedefUrl);
}
