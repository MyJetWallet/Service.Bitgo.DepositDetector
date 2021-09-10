using System.Linq;
using System.Threading.Tasks;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Models;

namespace Service.Bitgo.DepositDetector.Services
{
    public class DepositAddressGeneratorService
    {
        private readonly IBitGoClient _bitgoClient;

        public DepositAddressGeneratorService(IBitGoClient bitgoClient)
        {
            _bitgoClient = bitgoClient;
        }

        public async Task<(string, string, Error)> GenerateOrGetAddressIdAsync(string bitgoCoin, string bitgoWalletId,
            string label)
        {
            var addresses = await _bitgoClient.GetAddressesAsync(bitgoCoin, bitgoWalletId, label, 50);
            if (addresses.Data.TotalAddressCount > 0)
                return (addresses.Data.Addresses.Last().AddressId, addresses.Data.Addresses.Last().Label, null);

            var newAddress = await _bitgoClient.CreateAddressAsync(bitgoCoin, bitgoWalletId, label);

            return (newAddress.Data?.AddressId, newAddress.Data?.Label, newAddress.Error);
        }

        public async Task<(string, Error)> GetAddressAsync(string bitgoCoin, string bitgoWalletId,
            string label)
        {
            var addresses = await _bitgoClient.GetAddressesAsync(bitgoCoin, bitgoWalletId, label, 50);
            if (addresses.Data.TotalAddressCount > 0) return (addresses.Data.Addresses.Last().Address, null);
            return (null, addresses.Error);
        }

        public async Task<(string, Error)> UpdateAddressLabelAsync(string bitgoCoin, string bitgoWalletId,
            string addressId, string label)
        {
            var addresses = await _bitgoClient.UpdateAddressAsync(bitgoCoin, bitgoWalletId, addressId, label);
            return (addresses.Data?.Address, addresses.Error);
        }

        public async Task<(string, Error)> GenerateAddressAsync(string bitgoCoin, string bitgoWalletId,
            string label)
        {
            var newAddress = await _bitgoClient.CreateAddressAsync(bitgoCoin, bitgoWalletId, label);
            return (newAddress.Data.Address, newAddress.Error);
        }
    }
}