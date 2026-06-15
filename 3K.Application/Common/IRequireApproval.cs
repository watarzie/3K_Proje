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
}
