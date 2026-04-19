using System.Threading.Tasks;

namespace _3K.Core.Interfaces
{
    public interface ISseNotifier
    {
        Task SubscribeAsync(object context);
        Task BroadcastApprovalUpdateAsync();
    }
}
