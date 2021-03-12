using System.ServiceModel;
using System.Threading.Tasks;
using Service.Bitgo.DepositDetector.Domain.Models;

namespace Service.Bitgo.DepositDetector.Grpc
{
    [ServiceContract]
    public interface IBitgoDepositTransferProcessService
    {
        [OperationContract]
        Task HandledDepositAsync(SignalBitGoTransfer transferId);
    }
}