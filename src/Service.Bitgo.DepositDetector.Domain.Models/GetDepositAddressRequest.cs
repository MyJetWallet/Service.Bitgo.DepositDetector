using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Domain.Models
{
    [DataContract]
    public class GetDepositAddressRequest
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string ClientId { get; set; }
        [DataMember(Order = 3)] public string WalletId { get; set; }
        [DataMember(Order = 4)] public string AssetSymbol { get; set; }
    }
}