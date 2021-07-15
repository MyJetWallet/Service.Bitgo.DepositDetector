using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.Service.Tools;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Postgres;
using Service.ChangeBalanceGateway.Grpc;
using Service.ChangeBalanceGateway.Grpc.Models;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.DepositDetector.Jobs
{
    public class DepositsProcessingJob : IDisposable
    {
        private readonly ISpotChangeBalanceService _changeBalanceService;
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<DepositsProcessingJob> _logger;

        private readonly MyTaskTimer _timer;

        public DepositsProcessingJob(ILogger<DepositsProcessingJob> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ISpotChangeBalanceService changeBalanceService)
        {
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _changeBalanceService = changeBalanceService;
            _timer = new MyTaskTimer(typeof(DepositsProcessingJob),
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.DepositsProcessingIntervalSec).Invoke()),
                logger, DoTime);
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async Task DoTime()
        {
            using var activity = MyTelemetry.StartActivity("Handle deposits");
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var sw = new Stopwatch();
                sw.Start();

                var deposits = await context.Deposits.Where(e => e.Status == DepositStatus.New).ToListAsync();

                foreach (var deposit in deposits)
                {
                    var request = new BlockchainDepositGrpcRequest
                    {
                        WalletId = deposit.WalletId,
                        ClientId = deposit.ClientId,
                        Amount = deposit.Amount,
                        AssetSymbol = deposit.AssetSymbol,
                        BrokerId = deposit.BrokerId,
                        Integration = deposit.Integration,
                        TransactionId = deposit.TransactionId,
                        Comment = deposit.Comment,
                        Txid = deposit.Txid
                    };

                    var resp = await _changeBalanceService.BlockchainDepositAsync(request);
                    if (!resp.Result)
                    {
                        _logger.LogError("Cannot deposit to ME. Error: {errorText}", resp.ErrorMessage);
                        deposit.RetriesCount++;
                        deposit.LastError = resp.ErrorMessage;
                        if (deposit.RetriesCount >=
                            Program.ReloadedSettings(e => e.DepositsRetriesLimit).Invoke())
                            deposit.Status = DepositStatus.Error;

                        continue;
                    }

                    deposit.MatchingEngineId = resp.TransactionId;
                    deposit.Status = DepositStatus.Processed;
                }

                context.UpdateAsync(deposits);

                deposits.Count.AddToActivityAsTag("deposits-count");

                sw.Stop();
                _logger.LogInformation("Handled {countTrade} deposits. Time: {timeRangeText}", deposits.Count,
                    sw.Elapsed.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle deposits");
                ex.FailActivity();

                throw;
            }

            _timer.ChangeInterval(
                TimeSpan.FromSeconds(Program.ReloadedSettings(e => e.DepositsProcessingIntervalSec).Invoke()));
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