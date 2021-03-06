using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Grpc.Models
{
    [DataContract]
    public class RetryDepositResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public long DepositId { get; set; }
    }
}