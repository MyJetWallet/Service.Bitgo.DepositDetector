﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Domain;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.NoSql;

namespace Service.Bitgo.DepositDetector.Services
{
    public class BitgoDepositAddressService : IBitgoDepositAddressService
    {
        private readonly IAssetMapper _assetMapper;
        private readonly IBitGoClient _bitgoClient;
        private readonly IMyNoSqlServerDataWriter<DepositAddressEntity> _dataWriter;
        private readonly ILogger<BitgoDepositAddressService> _logger;
        private readonly IWalletMapper _walletMapper;

        public BitgoDepositAddressService(
            ILogger<BitgoDepositAddressService> logger,
            IAssetMapper assetMapper,
            IWalletMapper walletMapper,
            IBitGoClient bitgoClient,
            IMyNoSqlServerDataWriter<DepositAddressEntity> dataWriter)
        {
            _logger = logger;
            _assetMapper = assetMapper;
            _walletMapper = walletMapper;
            _bitgoClient = bitgoClient;
            _dataWriter = dataWriter;
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

                var address = await GenerateOrGetAddressAsync(bitgoCoin, bitgoWalletId, request);

                if (string.IsNullOrEmpty(address))
                {
                    _logger.LogError(
                        "Cannot process GetDepositAddress. Unable to generate address. Request: {jsonText}",
                        JsonConvert.SerializeObject(request));
                    return new GetDepositAddressResponse
                    {
                        Error = GetDepositAddressResponse.ErrorCode.AddressNotGenerated
                    };
                }

                await _dataWriter.InsertOrReplaceAsync(DepositAddressEntity.Create(request.WalletId,
                    request.AssetSymbol, address));
                await _dataWriter.CleanAndKeepMaxPartitions(Program.Settings.MaxClientInCache);

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
            var entities = await _dataWriter.GetAsync();
            var entity = entities.FirstOrDefault(e => e.Address == request.Address && e.AssetSymbol == request.AssetSymbol);
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

        public async Task<string> GenerateOrGetAddressAsync(string bitgoCoin, string bitgoWalletId,
            GetDepositAddressRequest request)
        {
            var label = _walletMapper.WalletToBitgoLabel(new JetWalletIdentity
            {
                BrokerId = request.BrokerId,
                ClientId = request.ClientId,
                WalletId = request.WalletId
            });

            var addresses = await _bitgoClient.GetAddressesAsync(bitgoCoin, bitgoWalletId, label, 50);
            if (addresses.Data.TotalAddressCount > 0) return addresses.Data.Addresses.Last().Address;

            var newAddress = await _bitgoClient.CreateAddressAsync(bitgoCoin, bitgoWalletId, label);

            return newAddress.Data.Address;
        }
    }
}