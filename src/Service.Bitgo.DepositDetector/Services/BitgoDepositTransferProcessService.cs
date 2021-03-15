using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using Newtonsoft.Json;
using Service.AssetsDictionary.Client;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.Settings;
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
        }

        public async Task HandledDepositAsync(SignalBitGoTransfer transferId)
        {
            _logger.LogInformation("Request to handle transfer fromm BitGo: {transferJson}", JsonConvert.SerializeObject(transferId));

            var asset = _assetMapper.GetAssetForBitGoAsync(transferId.Coin, transferId.WalletId);

            if (asset == null)
            {
                _logger.LogWarning("Cannot handle BitGo deposit, asset do not found {transferJson}", JsonConvert.SerializeObject(transferId));
                return;
            }

            var transferResp = await _bitgoClient.GetTransferAsync(transferId.Coin, transferId.WalletId, transferId.TransferId);
            var transfer = transferResp.Data;

            if (transfer == null)
            {
                _logger.LogWarning("Cannot handle BitGo deposit, transfer do not found {transferJson}", JsonConvert.SerializeObject(transferId));
                return;
            }

            _logger.LogInformation("Transfer fromm BitGo: {transferJson}", JsonConvert.SerializeObject(transfer));

            if (!_walletIds.Contains(transfer.WalletId))
            {
                _logger.LogInformation("Transfer {transferIdString} fromm BitGo is skipped, Wallet do not include in enabled wallet list {walletListText}", transfer.TransferId, _walletIdsSettings);
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

                var amount = entryGroup.Sum(e => e.Value);

                var request = new BlockchainDepositGrpcRequest()
                {
                    WalletId = wallet.WalletId,
                    ClientId = wallet.ClientId,
                    Amount = _assetMapper.ConvertAmountFromBitgo(transferId.Coin, amount),
                    AssetSymbol = asset.Symbol,
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
                    throw new Exception($"Cannot deposit to ME. Error: {resp.ErrorMessage}");
                }
            }

            _logger.LogInformation("Transfer fromm BitGo {transferIdString} is handled", transfer.TransferId);
        }
    }
}
