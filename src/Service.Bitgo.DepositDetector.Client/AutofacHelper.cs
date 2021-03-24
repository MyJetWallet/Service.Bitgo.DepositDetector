using Autofac;
using DotNetCoreDecorators;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using MyServiceBus.TcpClient;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.NoSql;

// ReSharper disable UnusedMember.Global

namespace Service.Bitgo.DepositDetector.Client
{
    public static class AutofacHelper
    {
        public static void RegisterBitgoDepositDetectorClient(this ContainerBuilder builder, string bitgoDepositDetectorGrpcServiceUrl)
        {
            var factory = new BitgoDepositDetectorClientFactory(bitgoDepositDetectorGrpcServiceUrl);

            builder.RegisterInstance(factory.GetBitgoDepositTransferProcessService()).As<IBitgoDepositTransferProcessService>().SingleInstance();
        }

        public static void RegisterBitgoDepositAddressClient(this ContainerBuilder builder, string bitgoDepositDetectorGrpcServiceUrl, IMyNoSqlSubscriber myNoSqlClient)
        {
            var reader = new MyNoSqlReadRepository<DepositAddressEntity>(myNoSqlClient, DepositAddressEntity.TableName);

            builder
                .RegisterInstance(reader)
                .As<IMyNoSqlServerDataReader<DepositAddressEntity>>()
                .SingleInstance();

            var factory = new BitgoDepositDetectorClientFactory(bitgoDepositDetectorGrpcServiceUrl);

            builder
                .RegisterInstance(new BitgoDepositAddressServiceCachedClient(factory.GetBitgoDepositAddressService(), reader))
                .As<IBitgoDepositAddressService>()
                .SingleInstance();
        }
    }
}