using Autofac;
using DotNetCoreDecorators;
using MyServiceBus.TcpClient;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Bitgo.DepositDetector.Client
{
    public static class AutofacHelper
    {
        public static void RegisterSignalBitGoTransferPublisher(this ContainerBuilder builder, MyServiceBusTcpClient client)
        {
            builder
                .RegisterInstance(new ClientRegistrationServiceBusPublisher(client))
                .As<IPublisher<SignalBitGoTransfer>>()
                .SingleInstance();
        }

        public static void RegisterBitgoDepositDetectorClient(this ContainerBuilder builder, string balanceHistoryGrpcServiceUrl)
        {
            var factory = new BitgoDepositDetectorClientFactory(balanceHistoryGrpcServiceUrl);

            builder.RegisterInstance(factory.GetBitgoDepositTransferProcessService()).As<IBitgoDepositTransferProcessService>().SingleInstance();
        }
    }
}