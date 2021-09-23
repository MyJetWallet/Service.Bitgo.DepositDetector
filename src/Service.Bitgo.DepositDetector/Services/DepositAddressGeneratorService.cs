using System;
using System.Linq;
using System.Threading.Tasks;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Models;
using MyJetWallet.BitGo.Settings.NoSql;
using MyNoSqlServer.Abstractions;

namespace Service.Bitgo.DepositDetector.Services
{
    public class DepositAddressGeneratorService
    {
        private readonly IBitGoClient _bitgoClient;
        private readonly IMyNoSqlServerDataReader<BitgoCoinEntity> _bitgoCoinReader;

        public DepositAddressGeneratorService(
            IBitGoClient bitgoClient,
            IMyNoSqlServerDataReader<BitgoCoinEntity> bitgoCoinReader)
        {
            _bitgoClient = bitgoClient;
            _bitgoCoinReader = bitgoCoinReader;
        }

        public async Task<(string, string, Error)> GenerateOrGetAddressIdAsync(string bitgoCoin, string bitgoWalletId,
            string label)
        {
            var coin = await GetCoin(bitgoCoin);
            
            var addresses = await _bitgoClient.Get(coin.IsMainNet).GetAddressesAsync(bitgoCoin, bitgoWalletId, label, 50);
            if (addresses.Data.TotalAddressCount > 0)
                return (addresses.Data.Addresses.Last().AddressId, addresses.Data.Addresses.Last().Label, null);

            var newAddress = await _bitgoClient.Get(coin.IsMainNet).CreateAddressAsync(bitgoCoin, bitgoWalletId, label);

            return (newAddress.Data?.AddressId, newAddress.Data?.Label, newAddress.Error);
        }

        public async Task<(string, Error)> GetAddressAsync(string bitgoCoin, string bitgoWalletId,
            string label)
        {
            var coin = await GetCoin(bitgoCoin);
            var addresses = await _bitgoClient.Get(coin.IsMainNet).GetAddressesAsync(bitgoCoin, bitgoWalletId, label, 50);
            if (addresses.Data.TotalAddressCount > 0) return (addresses.Data.Addresses.Last().Address, null);
            return (null, addresses.Error);
        }

        public async Task<(string, Error)> UpdateAddressLabelAsync(string bitgoCoin, string bitgoWalletId,
            string addressId, string label)
        {
            var coin = await GetCoin(bitgoCoin);
            var addresses = await _bitgoClient.Get(coin.IsMainNet).UpdateAddressAsync(bitgoCoin, bitgoWalletId, addressId, label);
            return (addresses.Data?.Address, addresses.Error);
        }

        public async Task<(string, Error)> GenerateAddressAsync(string bitgoCoin, string bitgoWalletId,
            string label)
        {
            var coin = await GetCoin(bitgoCoin);
            var newAddress = await _bitgoClient.Get(coin.IsMainNet).CreateAddressAsync(bitgoCoin, bitgoWalletId, label);
            return (newAddress.Data.Address, newAddress.Error);
        }

        private async Task<BitgoCoinEntity> GetCoin(string bitgoCoin)
        {
            var coin = _bitgoCoinReader.Get(BitgoCoinEntity.GeneratePartitionKey(), BitgoCoinEntity.GenerateRowKey(bitgoCoin));

            if (coin == null)
            {
                await Task.Delay(5000);
                coin = _bitgoCoinReader.Get(BitgoCoinEntity.GeneratePartitionKey(), BitgoCoinEntity.GenerateRowKey(bitgoCoin));
            }

            if (coin == null)
            {
                throw new Exception($"BitGo coin '{bitgoCoin}' does not exist.");
            }

            return coin;
        }
    }
}