namespace _3K.Application.Common
{
    /// <summary>Onaylanmış bir komutun yürütüldüğünü pipeline'a bildirir; HTTP isteğinden açılamaz.</summary>
    public interface IApprovalExecutionContext
    {
        bool IsExecutingApprovedCommand { get; }
        IDisposable BeginApprovedExecution();
    }
}
