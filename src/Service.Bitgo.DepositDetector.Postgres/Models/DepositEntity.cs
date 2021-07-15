using System;
using Service.Bitgo.DepositDetector.Domain.Models;

namespace Service.Bitgo.DepositDetector.Postgres.Models
{
    public class DepositEntity : Deposit
    {
        public DepositEntity(long id, string brokerId, string clientId, string walletId, string transactionId,
            double amount, string assetSymbol, string comment, string integration, string txid, DepositStatus status,
            string matchingEngineId, string lastError, int retriesCount, DateTime eventDate) : base(id, brokerId,
            clientId, walletId, transactionId, amount, assetSymbol, comment, integration, txid, status,
            matchingEngineId, lastError, retriesCount, eventDate)
        {
        }

        public DepositEntity(Deposit deposit) : base(deposit)
        {
        }

        public DepositEntity()
        {
        }
    }
}