using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Domain.Models
{
    [DataContract]
    public class GetAddressInfoRequest
    {
        [DataMember(Order = 1)] public string Address { get; set; }
        [DataMember(Order = 2)] public string AssetSymbol { get; set; }
    }
}