using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.Grpc.Models;
using Service.Bitgo.DepositDetector.Postgres;
using Service.Bitgo.DepositDetector.Postgres.Models;
using Service.ChangeBalanceGateway.Grpc;
using Service.ChangeBalanceGateway.Grpc.Models;

// ReSharper disable InconsistentLogPropertyNaming

namespace Service.Bitgo.DepositDetector.Services
{
    public class BitgoDepositService : IBitgoDepositService
    {
        private readonly ISpotChangeBalanceService _changeBalanceService;
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;
        private readonly ILogger<BitgoDepositService> _logger;

        public BitgoDepositService(ILogger<BitgoDepositService> logger,
            DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder,
            ISpotChangeBalanceService changeBalanceService)
        {
            _logger = logger;
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
            _changeBalanceService = changeBalanceService;
        }

        public async Task<GetDepositsResponse> GetDeposits(GetDepositsRequest request)
        {
            request.AddToActivityAsJsonTag("request-data");
            _logger.LogInformation("Receive GetDeposits request: {JsonRequest}", JsonConvert.SerializeObject(request));

            if (request.BatchSize % 2 != 0)
                return new GetDepositsResponse
                {
                    Success = false,
                    ErrorMessage = "Butch size must be even"
                };

            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);
                var deposits = await context.Deposits
                    .Where(e => e.Id > request.LastId)
                    .OrderByDescending(e => e.Id)
                    .Take(request.BatchSize)
                    .ToListAsync();

                var response = new GetDepositsResponse
                {
                    Success = true,
                    DepositCollection = deposits.Select(e => new Deposit(e)).ToList(),
                    IdForNextQuery = deposits.Count > 0 ? deposits.Select(e => e.Id).Max() : 0
                };

                response.DepositCollection.Count.AddToActivityAsTag("response-count-items");
                _logger.LogInformation("Return GetDeposits response count items: {count}",
                    response.DepositCollection.Count);
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Cannot get GetDeposits take: {takeValue}, LastId: {LastId}",
                    request.BatchSize, request.LastId);
                return new GetDepositsResponse {Success = false, ErrorMessage = exception.Message};
            }
        }

        public async Task<RetryDepositResponse> RetryDeposit(RetryDepositRequest request)
        {
            using var activity = MyTelemetry.StartActivity("Handle deposit manual retry")
                .AddTag("DepositId", request.DepositId);
            _logger.LogInformation("Handle deposit manual retry: {depositId}", request.DepositId);
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var deposit = await context.Deposits.FindAsync(request.DepositId);

                if (deposit == null)
                {
                    _logger.LogInformation("Unable to find deposit with id {depositId}", request.DepositId);
                    return new RetryDepositResponse
                    {
                        Success = false,
                        ErrorMessage = "Unable to find deposit",
                        DepositId = request.DepositId
                    };
                }

                if (deposit.Status == DepositStatus.Processed)
                {
                    _logger.LogInformation("Deposit {depositId} already processed", request.DepositId);
                    return new RetryDepositResponse
                    {
                        Success = true,
                        DepositId = request.DepositId
                    };
                }

                var changeBalanceRequest = new BlockchainDepositGrpcRequest
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

                var resp = await _changeBalanceService.BlockchainDepositAsync(changeBalanceRequest);
                if (!resp.Result)
                {
                    _logger.LogError("Cannot deposit to ME. Error: {errorText}", resp.ErrorMessage);
                    deposit.RetriesCount++;
                    deposit.LastError = resp.ErrorMessage;
                    if (deposit.RetriesCount >=
                        Program.ReloadedSettings(e => e.DepositsRetriesLimit).Invoke())
                        deposit.Status = DepositStatus.Error;
                }

                deposit.MatchingEngineId = resp.TransactionId;
                deposit.Status = DepositStatus.Processed;

                await context.UpdateAsync(new List<DepositEntity> {deposit});

                _logger.LogInformation("Handled deposit manual retry: {depositId}", request.DepositId);
                return new RetryDepositResponse
                {
                    Success = true,
                    DepositId = request.DepositId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle deposits");
                ex.FailActivity();

                return new RetryDepositResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal error {ex.Message}",
                    DepositId = request.DepositId
                };
            }
        }

        public async Task<CancelDepositResponse> CancelDeposit(CancelDepositRequest request)
        {
            using var activity = MyTelemetry.StartActivity("Handle deposit manual cancel")
                .AddTag("DepositId", request.DepositId);
            _logger.LogInformation("Handle deposit manual cancel: {depositId}", request.DepositId);
            try
            {
                await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

                var deposit = await context.Deposits.FindAsync(request.DepositId);

                if (deposit == null)
                {
                    _logger.LogInformation("Unable to find deposit with id {depositId}", request.DepositId);
                    return new CancelDepositResponse
                    {
                        Success = false,
                        ErrorMessage = "Unable to find deposit",
                        DepositId = request.DepositId
                    };
                }

                if (deposit.Status != DepositStatus.New)
                {
                    _logger.LogInformation("Incorrect status {status} for {depositId}", deposit.Status,
                        request.DepositId);
                    return new CancelDepositResponse
                    {
                        Success = false,
                        ErrorMessage = $"Wrong status deposit {deposit.Status}",
                        DepositId = request.DepositId
                    };
                }

                deposit.Status = DepositStatus.Cancelled;

                await context.UpdateAsync(new List<DepositEntity> {deposit});

                _logger.LogInformation("Handled deposit manual cancel: {depositId}", request.DepositId);
                return new CancelDepositResponse
                {
                    Success = true,
                    DepositId = request.DepositId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot Handle deposits");
                ex.FailActivity();

                return new CancelDepositResponse
                {
                    Success = false,
                    ErrorMessage = $"Internal error {ex.Message}",
                    DepositId = request.DepositId
                };
            }
        }
    }
}