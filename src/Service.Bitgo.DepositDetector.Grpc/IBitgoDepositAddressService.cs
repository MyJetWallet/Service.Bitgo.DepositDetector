using System.ServiceModel;
using System.Threading.Tasks;
using Service.Bitgo.DepositDetector.Domain.Models;

namespace Service.Bitgo.DepositDetector.Grpc
{
    [ServiceContract]
    public interface IBitgoDepositAddressService
    {
        [OperationContract]
        Task<GetDepositAddressResponse> GetDepositAddressAsync(GetDepositAddressRequest request);
    }
}