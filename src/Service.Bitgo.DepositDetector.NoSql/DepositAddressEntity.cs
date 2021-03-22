using System;
using MyNoSqlServer.Abstractions;

namespace Service.Bitgo.DepositDetector.NoSql
{
    public class DepositAddressEntity : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-deposit-addresses";

        public static string GeneratePartitionKey(string walletId) => $"wallet:{walletId}";
        public static string GenerateRowKey(string assetSymbol) => $"{assetSymbol}";

        public string Address { get; set; }

        public string WalletId { get; set; }

        public string AssetSymbol { get; set; }

        public static DepositAddressEntity Create(string walletId, string symbol, string address)
        {
            return new DepositAddressEntity()
            {
                PartitionKey = GeneratePartitionKey(walletId),
                RowKey = GenerateRowKey(symbol),
                WalletId = walletId,
                AssetSymbol = symbol,
                Address = address
            };
        }
    }
}
