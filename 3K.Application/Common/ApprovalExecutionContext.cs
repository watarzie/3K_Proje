namespace _3K.Application.Common
{
    public sealed class ApprovalExecutionContext : IApprovalExecutionContext, _3K.Core.Interfaces.IIslemKullaniciBaglami
    {
        private int _depth;
        public int? InitiatorUserId { get; private set; }
        public int? IslemKullaniciId { get; private set; }

        public bool IsExecutingApprovedCommand => _depth > 0;

        public IDisposable BeginApprovedExecution()
        {
            _depth++;
            return new Scope(() => _depth--);
        }

        public IDisposable BeginApprovedExecution(int initiatorUserId)
            => BeginApprovedExecution(initiatorUserId, false);

        public IDisposable BeginApprovedExecution(int initiatorUserId, bool useInitiator)
        {
            var previous = InitiatorUserId;
            var previousActor = IslemKullaniciId;
            InitiatorUserId = initiatorUserId;
            IslemKullaniciId = useInitiator ? initiatorUserId : null;
            _depth++;
            return new Scope(() => { _depth--; InitiatorUserId = previous; IslemKullaniciId = previousActor; });
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
