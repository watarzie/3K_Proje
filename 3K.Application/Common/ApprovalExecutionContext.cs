namespace _3K.Application.Common
{
    public sealed class ApprovalExecutionContext : IApprovalExecutionContext
    {
        private int _depth;

        public bool IsExecutingApprovedCommand => _depth > 0;

        public IDisposable BeginApprovedExecution()
        {
            _depth++;
            return new Scope(() => _depth--);
        }

        private sealed class Scope : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public Scope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _onDispose();
            }
        }
    }
}
