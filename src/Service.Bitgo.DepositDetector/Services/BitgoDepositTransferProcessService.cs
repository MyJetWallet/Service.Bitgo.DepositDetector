using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.Postgres;
using Service.Bitgo.DepositDetector.Postgres.Models;
using Service.Bitgo.Webhooks.Domain.Models;

// ReSharper disable InconsistentLogPropertyNaming
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace Service.Bitgo.DepositDetector.Services
{
    public class BitgoDepositTransferProcessService : IBitgoDepositTransferProcessService
    {
        private readonly IAssetMapper _assetMapper;
        private readonly IBitGoClient _bitgoClient;
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<BitgoDepositTransferProcessService> _logger;
        private readonly IWalletMapper _walletMapper;

        public BitgoDepositTransferProcessService(ILogger<BitgoDepositTransferProcessService> logger,
            IAssetMapper assetMapper,
            IWalletMapper walletMapper,
            IBitGoClient bitgoClient,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _logger = logger;
            _assetMapper = assetMapper;
            _walletMapper = walletMapper;
            _bitgoClient = bitgoClient;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task HandledDepositAsync(SignalBitGoTransfer transferId)
        {
            transferId.AddToActivityAsJsonTag("bitgo signal");

            _logger.LogInformation("Request to handle transfer from BitGo: {transferJson}",
                JsonConvert.SerializeObject(transferId));

            var (brokerId, assetSymbol) = _assetMapper.BitgoCoinToAsset(transferId.Coin, transferId.WalletId);

            if (string.IsNullOrEmpty(brokerId) || string.IsNullOrEmpty(assetSymbol))
            {
                _logger.LogWarning("Cannot handle BitGo deposit, asset do not found {transferJson}",
                    JsonConvert.SerializeObject(transferId));
                return;
            }

            var transferResp =
                await _bitgoClient.GetTransferAsync(transferId.Coin, transferId.WalletId, transferId.TransferId);
            var transfer = transferResp.Data;

            if (transfer == null)
            {
                _logger.LogWarning("Cannot handle BitGo deposit, transfer do not found {transferJson}",
                    JsonConvert.SerializeObject(transferId));
                Activity.Current?.SetStatus(Status.Error);
                return;
            }

            transfer.AddToActivityAsJsonTag("bitgo-transfer");

            _logger.LogInformation("Transfer from BitGo: {transferJson}", JsonConvert.SerializeObject(transfer));

            var requirement = _assetMapper.GetRequiredConfirmations(transfer.Coin);
            if (transfer.Confirmations < requirement)
            {
                _logger.LogError(
                    $"Transaction do not has enough conformations. Transaction has: {transfer.Confirmations}, requirement: {requirement}");
                Activity.Current?.SetStatus(Status.Error);
                throw new Exception(
                    $"Transaction do not has enough conformations. Transaction has: {transfer.Confirmations}, requirement: {requirement}");
            }

            if (!_assetMapper.IsWalletEnabled(transfer.Coin, transfer.WalletId))
            {
                _logger.LogError(
                    "Transfer {transferIdString} from BitGo is skipped, Wallet do not include in enabled wallet list",
                    transfer.TransferId);
                Activity.Current?.SetStatus(Status.Error);
                return;
            }

            foreach (var entryGroup in transfer.Entries
                .Where(e => e.Value > 0 && !string.IsNullOrEmpty(e.Label) && e.WalletId == transferId.WalletId &&
                            (e.Token == null || e.Token == transferId.Coin))
                .GroupBy(e => e.Label))
            {
                var label = entryGroup.Key;
                var wallet = _walletMapper.BitgoLabelToWallet(label);
                if (wallet == null)
                {
                    _logger.LogWarning("Cannot found wallet for transfer entry with label={label} address={address}",
                        label, entryGroup.First().Address);
                    continue;
                }

                wallet.WalletId.AddToActivityAsTag("walletId");
                wallet.ClientId.AddToActivityAsTag("clientId");

                var amount = entryGroup.Sum(e => e.Value);

                try
                {
                    await using var ctx = DatabaseContext.Create(_dbContextOptionsBuilder);
                    await ctx.InsertAsync(new DepositEntity
                    {
                        BrokerId = wallet.BrokerId,
                        WalletId = wallet.WalletId,
                        ClientId = wallet.ClientId,
                        TransactionId = $"{transfer.TransferId}:{wallet.WalletId}",
                        Amount = _assetMapper.ConvertAmountFromBitgo(transferId.Coin, amount),
                        AssetSymbol = assetSymbol,
                        Comment =
                            $"Bitgo transfer [{transferId.Coin}:{transferId.WalletId}] entry label='{label}', count entry={entryGroup.Count()}",
                        Integration = "BitGo",
                        Txid = transfer.TxId,
                        Status = DepositStatus.New,
                        EventDate = DateTime.UtcNow
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            _logger.LogInformation("Deposit request from BitGo {transferIdString} is handled", transfer.TransferId);
        }
    }
}