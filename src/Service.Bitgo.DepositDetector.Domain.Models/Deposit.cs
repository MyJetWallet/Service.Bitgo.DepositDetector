using System;
using System.Runtime.Serialization;

namespace Service.Bitgo.DepositDetector.Domain.Models
{
    public class Deposit
    {
        public Deposit(long id, string brokerId, string clientId, string walletId, string transactionId, double amount,
            string assetSymbol, string comment, string integration, string txid, DepositStatus status,
            string matchingEngineId, string lastError, int retriesCount, DateTime eventDate)
        {
            Id = id;
            BrokerId = brokerId;
            ClientId = clientId;
            WalletId = walletId;
            TransactionId = transactionId;
            Amount = amount;
            AssetSymbol = assetSymbol;
            Comment = comment;
            Integration = integration;
            Txid = txid;
            Status = status;
            MatchingEngineId = matchingEngineId;
            LastError = lastError;
            RetriesCount = retriesCount;
            EventDate = eventDate;
        }

        public Deposit(Deposit deposit) : this(deposit.Id, deposit.BrokerId, deposit.ClientId, deposit.WalletId,
            deposit.TransactionId, deposit.Amount, deposit.AssetSymbol, deposit.Comment,
            deposit.Integration, deposit.Txid, deposit.Status, deposit.MatchingEngineId, deposit.LastError,
            deposit.RetriesCount, deposit.EventDate)
        {
        }

        public Deposit()
        {
        }

        [DataMember(Order = 1)] public long Id { get; set; }

        [DataMember(Order = 2)] public string BrokerId { get; set; }

        [DataMember(Order = 3)] public string ClientId { get; set; }

        [DataMember(Order = 4)] public string WalletId { get; set; }

        [DataMember(Order = 5)] public string TransactionId { get; set; }

        [DataMember(Order = 6)] public double Amount { get; set; }

        [DataMember(Order = 7)] public string AssetSymbol { get; set; }

        [DataMember(Order = 8)] public string Comment { get; set; }

        [DataMember(Order = 9)] public string Integration { get; set; }

        [DataMember(Order = 10)] public string Txid { get; set; }

        [DataMember(Order = 11)] public DepositStatus Status { get; set; }

        [DataMember(Order = 12)] public string MatchingEngineId { get; set; }

        [DataMember(Order = 13)] public string LastError { get; set; }

        [DataMember(Order = 14)] public int RetriesCount { get; set; }

        [DataMember(Order = 15)] public DateTime EventDate { get; set; }
    }
}