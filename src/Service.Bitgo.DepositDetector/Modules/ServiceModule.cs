using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Ioc;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.Abstractions;
using MyNoSqlServer.DataReader;
using MyNoSqlServer.DataWriter;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.AssetsDictionary.Client;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.Jobs;
using Service.Bitgo.DepositDetector.NoSql;
using Service.Bitgo.DepositDetector.ServiceBus;
using Service.Bitgo.DepositDetector.Services;
using Service.Bitgo.Webhooks.Client;
using Service.ChangeBalanceGateway.Client;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace Service.Bitgo.DepositDetector.Modules
{
    public class ServiceModule : Module
    {
        public static ILogger ServiceBusLogger { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            var myNoSqlClient = builder.CreateNoSqlClient(Program.ReloadedSettings(e => e.MyNoSqlReaderHostPort));

            builder
                .RegisterAssetsDictionaryClients(myNoSqlClient);

            builder
                .RegisterSpotChangeBalanceGatewayClient(Program.Settings.ChangeBalanceGatewayGrpcServiceUrl);

            var bitgoClient =
                new BitGoClient(
                    Program.Settings.BitgoAccessTokenMainNet, Program.Settings.BitgoExpressUrlMainNet,
                    Program.Settings.BitgoAccessTokenTestNet, Program.Settings.BitgoExpressUrlTestNet);

            builder
                .RegisterInstance(bitgoClient)
                .As<IBitGoClient>()
                .SingleInstance();

            builder.RegisterBitgoSettingsReader(myNoSqlClient);

            ServiceBusLogger = Program.LogFactory.CreateLogger(nameof(MyServiceBusTcpClient));

            var serviceBusClient = builder.RegisterMyServiceBusTcpClient(
                Program.ReloadedSettings(e => e.SpotServiceBusHostPort),
                ApplicationEnvironment.HostName, Program.LogFactory);

            builder.RegisterSignalBitGoTransferSubscriber(serviceBusClient, "Bitgo-DepositDetector",
                TopicQueueType.Permanent);
            
            builder.RegisterMyServiceBusPublisher<Deposit>(serviceBusClient, Deposit.TopicName, false);

            builder
                .RegisterType<SignalBitGoTransferJob>()
                .AutoActivate()
                .SingleInstance();

            builder
                .RegisterType<BitgoDepositTransferProcessService>()
                .As<IBitgoDepositTransferProcessService>();

            builder.RegisterBitgoSettingsWriter(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl));

            builder.RegisterMyNoSqlWriter<DepositAddressEntity>(
                Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), DepositAddressEntity.TableName, true);
            
            builder
                .RegisterMyNoSqlWriter<GeneratedDepositAddressEntity>(Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), GeneratedDepositAddressEntity.TableName, true);
            
            builder
                .RegisterType<DepositsProcessingJob>()
                .AutoActivate()
                .SingleInstance()
                .AsSelf();

            builder
                .RegisterType<DepositAddressesGenerationJob>()
                .AutoActivate()
                .SingleInstance()
                .AsSelf();

            builder
                .RegisterType<DepositAddressGeneratorService>()
                .SingleInstance()
                .AsSelf();
        }
    }
}