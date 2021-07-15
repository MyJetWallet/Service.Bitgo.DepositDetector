using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Grpc.Models
{
    [DataContract]
    public class CancelDepositRequest
    {
        [DataMember(Order = 1)] public string DepositId { get; set; }
    }
}