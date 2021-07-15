using System.ServiceModel;
using System.Threading.Tasks;
using Service.Bitgo.DepositDetector.Grpc.Models;

namespace Service.Bitgo.DepositDetector.Grpc
{
    [ServiceContract]
    public interface IBitgoDepositService
    {
        [OperationContract]
        Task<GetDepositsResponse> GetDeposits(GetDepositsRequest request);

        [OperationContract]
        Task<RetryDepositResponse> RetryDeposit(RetryDepositRequest request);

        [OperationContract]
        Task<CancelDepositResponse> CancelDeposit(CancelDepositRequest request);
    }
}