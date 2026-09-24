namespace _3K.Application.Common
{
    public interface IApprovalExecutionContext
    {
        bool IsExecutingApprovedCommand { get; }
        int? InitiatorUserId => null;
        IDisposable BeginApprovedExecution();
        IDisposable BeginApprovedExecution(int initiatorUserId) => BeginApprovedExecution();
        IDisposable BeginApprovedExecution(int initiatorUserId, bool useInitiator) => BeginApprovedExecution(initiatorUserId);
    }
}
