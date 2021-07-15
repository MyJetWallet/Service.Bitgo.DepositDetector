using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Grpc.Models
{
    public class CancelDepositResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public string DepositId { get; set; }
    }
}