using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Domain.Models
{
    [DataContract]
    public class GetAddressInfoResponse
    {
        [DataMember(Order = 1)] public bool IsInternalAddress { get; set; }
        [DataMember(Order = 2)] public string WalletId { get; set; }
    }
}