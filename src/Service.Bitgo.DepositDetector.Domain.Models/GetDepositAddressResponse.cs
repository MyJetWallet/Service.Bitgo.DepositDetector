using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Domain.Models
{
    [DataContract]
    public class GetDepositAddressResponse
    {
        public enum ErrorCode
        {
            Ok,
            AssetDoNotSupported,
            AddressNotGenerated
        }

        [DataMember(Order = 1)] public string Address { get; set; }
        [DataMember(Order = 2)] public ErrorCode Error { get; set; }
    }
}