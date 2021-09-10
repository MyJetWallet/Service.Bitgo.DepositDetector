using MyNoSqlServer.Abstractions;
using Service.Bitgo.DepositDetector.Domain.Models;

namespace Service.Bitgo.DepositDetector.NoSql
{
    public class GeneratedDepositAddressEntity : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-generated-deposit-addresses";

        public static string GeneratePartitionKey(string brokerId, string asset, string walletId)
        {
            return $"wallet:{brokerId}:{asset}:{walletId}";
        }

        public static string GenerateRowKey(string id)
        {
            return $"{id}";
        }

        public GeneratedDepositAddress Address { get; set; }

        public static GeneratedDepositAddressEntity Create(GeneratedDepositAddress address)
        {
            return new GeneratedDepositAddressEntity
            {
                PartitionKey = GeneratePartitionKey(address.BrokerId, address.Asset, address.WalletId),
                RowKey = GenerateRowKey(address.PreGeneratedWalletId),
                Address = address
            };
        }
    }
}