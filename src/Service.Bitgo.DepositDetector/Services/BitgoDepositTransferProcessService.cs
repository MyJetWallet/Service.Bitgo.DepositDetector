using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.Settings;
using Service.Bitgo.Webhooks.Domain.Models;
using Service.ChangeBalanceGateway.Grpc;
using Service.ChangeBalanceGateway.Grpc.Models;


namespace Service.Bitgo.DepositDetector.Services
{
    public class BitgoDepositTransferProcessService : IBitgoDepositTransferProcessService
    {
        private readonly ILogger<BitgoDepositTransferProcessService> _logger;
        private readonly IAssetMapper _assetMapper;
        private readonly ISpotChangeBalanceService _changeBalanceService;
        private readonly IWalletMapper _walletMapper;
        private readonly IBitGoClient _bitgoClient;
        private readonly string[] _walletIds;
        private readonly string _walletIdsSettings;
        private readonly Dictionary<string, int> _conformationRequirements = new Dictionary<string, int>();

        public BitgoDepositTransferProcessService(ILogger<BitgoDepositTransferProcessService> logger,
            IAssetMapper assetMapper,
            ISpotChangeBalanceService changeBalanceService,
            IWalletMapper walletMapper,
            IBitGoClient bitgoClient,
            SettingsModel settingsModel)
        {
            _logger = logger;
            _assetMapper = assetMapper;
            _changeBalanceService = changeBalanceService;
            _walletMapper = walletMapper;
            _bitgoClient = bitgoClient;
            _walletIdsSettings = settingsModel.BitGoWalletIds;
            
            var walletIdString = _walletIdsSettings;
            if (!string.IsNullOrEmpty(walletIdString))
            {
                _walletIds = walletIdString.Split(';');
            }
            else
            {
                _walletIds = new string[] { };
            }

            if (!string.IsNullOrEmpty(settingsModel.BitGoCoinConformationRequirements))
            {
                var pairs = settingsModel.BitGoCoinConformationRequirements.Split(';');
                foreach (var pair in pairs)
                {
                    var coinValue = pair.Split('=');
                    if (coinValue.Length==2)
                    {
                        _conformationRequirements[coinValue[0]] = int.Parse(coinValue[1]);
                    }
                    else
                    {
                        throw new Exception($"Cannot read BitGoCoinConformationRequirements, pair '{pair}'");
                    }
                }
            }
        }

        public async Task HandledDepositAsync(SignalBitGoTransfer transferId)
        {
            transferId.AddToActivityAsJsonTag("bitgo signal");

            _logger.LogInformation("Request to handle transfer fromm BitGo: {transferJson}", JsonConvert.SerializeObject(transferId));

            var (brokerId, assetSymbol) = _assetMapper.BitgoCoinToAsset(transferId.Coin, transferId.WalletId);

            if (string.IsNullOrEmpty(brokerId) || string.IsNullOrEmpty(assetSymbol))
            {
                _logger.LogWarning("Cannot handle BitGo deposit, asset do not found {transferJson}", JsonConvert.SerializeObject(transferId));
                return;
            }

            var transferResp = await _bitgoClient.GetTransferAsync(transferId.Coin, transferId.WalletId, transferId.TransferId);
            var transfer = transferResp.Data;

            if (transfer == null)
            {
                _logger.LogWarning("Cannot handle BitGo deposit, transfer do not found {transferJson}", JsonConvert.SerializeObject(transferId));
                Activity.Current?.SetStatus(Status.Error);
                return;
            }

            transfer.AddToActivityAsJsonTag("bitgo-transfer");

            _logger.LogInformation("Transfer fromm BitGo: {transferJson}", JsonConvert.SerializeObject(transfer));

            if (_conformationRequirements.TryGetValue(transfer.Coin, out var requirement))
            {
                if (transfer.Confirmations < requirement)
                {
                    _logger.LogError($"Transaction do not has enough conformations. Transaction has: {transfer.Confirmations}, requirement: {requirement}");
                    Activity.Current?.SetStatus(Status.Error);
                    throw new Exception($"Transaction do not has enough conformations. Transaction has: {transfer.Confirmations}, requirement: {requirement}");
                }
            }
            //transfer.Confirmations

            if (!_walletIds.Contains(transfer.WalletId))
            {
                _logger.LogInformation("Transfer {transferIdString} fromm BitGo is skipped, Wallet do not include in enabled wallet list {walletListText}", transfer.TransferId, _walletIdsSettings);
                Activity.Current?.SetStatus(Status.Error);
                return;
            }

            foreach (var entryGroup in transfer.Entries
                .Where(e => e.Value > 0 && !string.IsNullOrEmpty(e.Label) && e.WalletId == transferId.WalletId && e.Token == null)
                .GroupBy(e => e.Label))
            {
                var label = entryGroup.Key;
                var wallet = _walletMapper.BitgoLabelToWallet(label);
                if (wallet == null)
                {
                    _logger.LogWarning("Cannot found wallet for transfer entry with label={label} address={address}", label, entryGroup.First().Address);
                    continue;
                }

                wallet.WalletId.AddToActivityAsTag("walletId");
                wallet.ClientId.AddToActivityAsTag("clientId");

                var amount = entryGroup.Sum(e => e.Value);

                var request = new BlockchainDepositGrpcRequest()
                {
                    WalletId = wallet.WalletId,
                    ClientId = wallet.ClientId,
                    Amount = _assetMapper.ConvertAmountFromBitgo(transferId.Coin, amount),
                    AssetSymbol = assetSymbol,
                    BrokerId = wallet.BrokerId,
                    Integration = "BitGo",
                    TransactionId = $"{transfer.TransferId}:{wallet.WalletId}",
                    Comment = $"Bitgo transfer [{transferId.Coin}:{transferId.WalletId}] entry label='{label}', count entry={entryGroup.Count()}",
                    Txid = transfer.TxId
                };

                var resp = await _changeBalanceService.BlockchainDepositAsync(request);
                if (!resp.Result)
                {
                    _logger.LogError("Cannot deposit to ME. Error: {errorText}", resp.ErrorMessage);
                    Activity.Current?.SetStatus(Status.Error);
                    throw new Exception($"Cannot deposit to ME. Error: {resp.ErrorMessage}");
                }
            }

            _logger.LogInformation("Transfer fromm BitGo {transferIdString} is handled", transfer.TransferId);
        }
    }
}
