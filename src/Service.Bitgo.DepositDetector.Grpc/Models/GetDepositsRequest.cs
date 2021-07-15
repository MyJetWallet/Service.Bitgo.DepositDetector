using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Grpc.Models
{
    public class GetDepositsRequest
    {
        [DataMember(Order = 1)] public long LastId { get; set; }
        [DataMember(Order = 2)] public int BatchSize { get; set; }
    }
}