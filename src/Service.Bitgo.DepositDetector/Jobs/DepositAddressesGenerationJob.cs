using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo.Settings.Services;
using MyJetWallet.Sdk.Service.Tools;
using MyNoSqlServer.Abstractions;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.NoSql;
using Service.Bitgo.DepositDetector.Services;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.DepositDetector.Jobs
{
    public class DepositAddressesGenerationJob : IDisposable
    {
        private readonly ILogger<DepositAddressesGenerationJob> _logger;
        private readonly IBitGoAssetMapSettingsService _bitGoAssetMapSettingsService;
        private readonly IMyNoSqlServerDataWriter<GeneratedDepositAddressEntity> _dataWriter;
        private readonly DepositAddressGeneratorService _depositAddressGeneratorService;

        private readonly MyTaskTimer _timer;

        public DepositAddressesGenerationJob(ILogger<DepositAddressesGenerationJob> logger,
            IBitGoAssetMapSettingsService bitGoAssetMapSettingsService,
            IMyNoSqlServerDataWriter<GeneratedDepositAddressEntity> dataWriter,
            DepositAddressGeneratorService depositAddressGeneratorService)
        {
            _logger = logger;
            _bitGoAssetMapSettingsService = bitGoAssetMapSettingsService;
            _dataWriter = dataWriter;
            _depositAddressGeneratorService = depositAddressGeneratorService;

            _timer = new MyTaskTimer(typeof(DepositAddressesGenerationJob),
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.GenerateAddressesIntervalSec).Invoke()),
                logger, DoTime);
        }

        private async Task DoTime()
        {
            var maxCount = Program.ReloadedSettings(e => e.PreGeneratedAddressesCount).Invoke();
            var wallets = await _bitGoAssetMapSettingsService.GetAllAssetMapsAsync();
            foreach (var wallet in wallets)
            {
                var entitiesCount = await _dataWriter.GetCountAsync(
                    GeneratedDepositAddressEntity.GeneratePartitionKey(wallet.BrokerId, wallet.BitgoCoin,
                        wallet.BitgoWalletId));
                if (entitiesCount < maxCount)
                {
                    for (var i = 1; i <= maxCount - entitiesCount; i++)
                        try
                        {
                            var id = Guid.NewGuid().ToString();
                            var label = $"PreGenerated-{id}";
                            var (addressId, address, error) =
                                await _depositAddressGeneratorService.GenerateOrGetAddressIdAsync(wallet.BitgoCoin,
                                    wallet.BitgoWalletId, label);
                            if (string.IsNullOrEmpty(addressId) || error != null)
                            {
                                _logger.LogError(
                                    "Unable to pre-generate address for broker {broker}, asset {asset}, wallet id {walletId}: {error}",
                                    wallet.BrokerId, wallet.BitgoCoin, wallet.BitgoWalletId, error);
                                continue;
                            }

                            await _dataWriter.InsertAsync(GeneratedDepositAddressEntity.Create(
                                new GeneratedDepositAddress
                                {
                                    BrokerId = wallet.BrokerId,
                                    Asset = wallet.BitgoCoin,
                                    WalletId = wallet.BitgoWalletId,
                                    PreGeneratedWalletId = id,
                                    AddressLabel = label,
                                    Address = address,
                                    BitGoAddressId = addressId
                                }));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Unable to pre-generate address for broker {broker}, asset {asset}, wallet id {walletId}",
                                wallet.BrokerId, wallet.BitgoCoin, wallet.BitgoWalletId);
                        }

                    _logger.LogInformation(
                        "Pre-generated {count} addresses for broker {broker}, asset {asset}, wallet id {walletId}",
                        maxCount - entitiesCount, wallet.BrokerId, wallet.BitgoCoin, wallet.BitgoWalletId);
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}