using System.Threading.Tasks;

namespace _3K.Core.Interfaces
{
    public interface ISseNotifier
    {
        Task SubscribeAsync(object context, int kullaniciId);
        Task NotifyUsersAsync(IEnumerable<int> kullaniciIdleri, string eventName, string data = "refresh");
        Task BroadcastApprovalUpdateAsync();
    }
}
