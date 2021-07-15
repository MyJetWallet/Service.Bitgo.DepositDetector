using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Grpc.Models
{
    [DataContract]
    public class RetryDepositRequest
    {
        [DataMember(Order = 1)] public long DepositId { get; set; }
    }
}