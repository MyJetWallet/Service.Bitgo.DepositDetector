using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Grpc.Models
{
    public class RetryDepositRequest
    {
        [DataMember(Order = 1)] public string DepositId { get; set; }
    }
}