using System.Threading.Tasks;
using MyNoSqlServer.Abstractions;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.NoSql;

namespace Service.Bitgo.DepositDetector.Client
{
    public class BitgoDepositAddressServiceCachedClient : IBitgoDepositAddressService
    {
        private readonly IBitgoDepositAddressService _service;
        private readonly IMyNoSqlServerDataReader<DepositAddressEntity> _dataReader;

        public BitgoDepositAddressServiceCachedClient(IBitgoDepositAddressService service, IMyNoSqlServerDataReader<DepositAddressEntity> dataReader)
        {
            _service = service;
            _dataReader = dataReader;
        }

        public async Task<GetDepositAddressResponse> GetDepositAddressAsync(GetDepositAddressRequest request)
        {
            var entity = _dataReader.Get(DepositAddressEntity.GeneratePartitionKey(request.WalletId), DepositAddressEntity.GenerateRowKey(request.AssetSymbol));

            if (entity != null)
            {
                return new GetDepositAddressResponse()
                {
                    Address = entity.Address,
                    Error = GetDepositAddressResponse.ErrorCode.Ok
                };
            }

            return await _service.GetDepositAddressAsync(request);
        }
    }
}