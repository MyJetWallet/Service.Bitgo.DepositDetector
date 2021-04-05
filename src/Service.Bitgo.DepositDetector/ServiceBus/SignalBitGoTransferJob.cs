using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.Webhooks.Domain.Models;

namespace Service.Bitgo.DepositDetector.ServiceBus
{
    public class SignalBitGoTransferJob
    {
        private readonly IBitgoDepositTransferProcessService _service;

        public SignalBitGoTransferJob(ISubscriber<SignalBitGoTransfer> subscriber, ILogger<SignalBitGoTransferJob> logger, IBitgoDepositTransferProcessService service)
        {
            _service = service;
            subscriber.Subscribe(HandleSignal);
        }

        private async ValueTask HandleSignal(SignalBitGoTransfer signal)
        {
            using var activity = MyTelemetry.StartActivity("Handle Event SignalBitGoTransfer");
            try
            {
                await _service.HandledDepositAsync(signal);
            }
            catch(Exception ex)
            {
                ex.FailActivity();
                throw;

            }
        }
    }
}