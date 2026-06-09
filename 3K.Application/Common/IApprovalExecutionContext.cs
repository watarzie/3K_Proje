namespace _3K.Application.Common
{
    public interface IApprovalExecutionContext
    {
        bool IsExecutingApprovedCommand { get; }
        IDisposable BeginApprovedExecution();
    }
}
