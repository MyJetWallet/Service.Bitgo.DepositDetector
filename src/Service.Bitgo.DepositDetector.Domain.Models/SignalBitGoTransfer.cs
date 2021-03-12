using System.Net.Http.Headers;
using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Domain.Models
{
    [DataContract]
    public class SignalBitGoTransfer
    {
        public const string ServiceBusTopicName = "bitgo-transfer-signal";

        [DataMember(Order = 1)] public string Coin { get; set; }
        [DataMember(Order = 2)] public string WalletId { get; set; }
        [DataMember(Order = 3)] public string TransferId { get; set; }
    }
}