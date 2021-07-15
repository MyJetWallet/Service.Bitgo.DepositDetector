namespace Service.Bitgo.DepositDetector.Domain.Models
{
    public enum DepositStatus
    {
        New,
        Error,
        Processed,
        Cancelled
    }
}