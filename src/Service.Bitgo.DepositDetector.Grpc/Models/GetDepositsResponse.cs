using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Bitgo.DepositDetector.Domain.Models;

namespace Service.Bitgo.DepositDetector.Grpc.Models
{
    public class GetDepositsResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public long IdForNextQuery { get; set; }
        [DataMember(Order = 4)] public List<Deposit> DepositCollection { get; set; }
    }
}