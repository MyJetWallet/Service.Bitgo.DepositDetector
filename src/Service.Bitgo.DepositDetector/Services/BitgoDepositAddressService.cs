using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Domain;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.NoSql;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.DepositDetector.Services
{
    public class BitgoDepositAddressService : IBitgoDepositAddressService
    {
        private readonly IAssetMapper _assetMapper;
        private readonly IMyNoSqlServerDataWriter<DepositAddressEntity> _addressDataWriter;
        private readonly IMyNoSqlServerDataWriter<GeneratedDepositAddressEntity> _generatedAddressDataWriter;
        private readonly ILogger<BitgoDepositAddressService> _logger;
        private readonly IWalletMapper _walletMapper;
        private readonly DepositAddressGeneratorService _depositAddressGeneratorService;

        public BitgoDepositAddressService(IAssetMapper assetMapper,
            IMyNoSqlServerDataWriter<DepositAddressEntity> addressDataWriter,
            IMyNoSqlServerDataWriter<GeneratedDepositAddressEntity> generatedAddressDataWriter,
            ILogger<BitgoDepositAddressService> logger, IWalletMapper walletMapper,
            DepositAddressGeneratorService depositAddressGeneratorService)
        {
            _assetMapper = assetMapper;
            _addressDataWriter = addressDataWriter;
            _generatedAddressDataWriter = generatedAddressDataWriter;
            _logger = logger;
            _walletMapper = walletMapper;
            _depositAddressGeneratorService = depositAddressGeneratorService;
        }

        public async Task<GetDepositAddressResponse> GetDepositAddressAsync(GetDepositAddressRequest request)
        {
            try
            {
                var (bitgoCoin, bitgoWalletId) =
                    _assetMapper.AssetToBitgoCoinAndWallet(request.BrokerId, request.AssetSymbol);

                if (string.IsNullOrEmpty(bitgoWalletId) || string.IsNullOrEmpty(bitgoCoin))
                {
                    _logger.LogError(
                        "Cannot process GetDepositAddress. Asset do not mapped to bitgo. Request: {jsonText}",
                        JsonConvert.SerializeObject(request));
                    return new GetDepositAddressResponse
                    {
                        Error = GetDepositAddressResponse.ErrorCode.AssetDoNotSupported
                    };
                }

                var label = _walletMapper.WalletToBitgoLabel(new JetWalletIdentity
                {
                    BrokerId = request.BrokerId,
                    ClientId = request.ClientId,
                    WalletId = request.WalletId
                });

                var (address, error) =
                    await _depositAddressGeneratorService.GetAddressAsync(bitgoCoin, bitgoWalletId, label);

                if (string.IsNullOrEmpty(address))
                {
                    var preGeneratedAddresses = (await _generatedAddressDataWriter.GetAsync(
                            GeneratedDepositAddressEntity.GeneratePartitionKey(request.BrokerId, bitgoCoin,
                                bitgoWalletId)))
                        .ToList();

                    if (preGeneratedAddresses.Count > 0)
                    {
                        var preGeneratedAddress = preGeneratedAddresses.First();
                        (address, error) = await _depositAddressGeneratorService.UpdateAddressLabelAsync(bitgoCoin,
                            bitgoWalletId,
                            preGeneratedAddress.Address.BitGoAddressId, label);
                        if (error == null)
                            await _generatedAddressDataWriter.DeleteAsync(preGeneratedAddress.PartitionKey,
                                preGeneratedAddress.RowKey);
                    }
                    else
                    {
                        (address, error) =
                            await _depositAddressGeneratorService.GenerateAddressAsync(bitgoCoin, bitgoWalletId, label);
                    }
                }

                if (string.IsNullOrEmpty(address) || error != null)
                {
                    _logger.LogError(
                        "Cannot process GetDepositAddress. Unable to generate address. Request: {jsonText}. Error: {error}",
                        JsonConvert.SerializeObject(request), error);
                    return new GetDepositAddressResponse
                    {
                        Error = GetDepositAddressResponse.ErrorCode.AddressNotGenerated
                    };
                }

                await _addressDataWriter.InsertOrReplaceAsync(DepositAddressEntity.Create(request.WalletId,
                    request.AssetSymbol, address));
                await _addressDataWriter.CleanAndKeepMaxPartitions(Program.Settings.MaxClientInCache);

                _logger.LogInformation("Handle GetDepositAddress, request: {jsonText}, address: {address}",
                    JsonConvert.SerializeObject(request), address);

                return new GetDepositAddressResponse
                {
                    Address = address,
                    Error = GetDepositAddressResponse.ErrorCode.Ok
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception on GetDepositAddress with request: {jsonText}",
                    JsonConvert.SerializeObject(request));
                throw;
            }
        }

        public async Task<GetAddressInfoResponse> GetWalletIdByAddressAsync(GetAddressInfoRequest request)
        {
            var entities = await _addressDataWriter.GetAsync();
            var entity =
                entities.FirstOrDefault(e => e.Address == request.Address && e.AssetSymbol == request.AssetSymbol);
            if (entity == null)
            {
                return new GetAddressInfoResponse()
                {
                    IsInternalAddress = false
                };
            }

            return new GetAddressInfoResponse()
            {
                IsInternalAddress = true,
                WalletId = entity.WalletId
            };
        }
    }
}