﻿using Autofac;
using Microsoft.Extensions.Logging;
using MyJetWallet.BitGo;
using MyJetWallet.BitGo.Settings.Ioc;
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

namespace Service.Bitgo.DepositDetector.Modules
{
    public class ServiceModule : Module
    {
        public static ILogger ServiceBusLogger { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            var myNoSqlClient = new MyNoSqlTcpClient(
                Program.ReloadedSettings(e => e.MyNoSqlReaderHostPort),
                ApplicationEnvironment.HostName ??
                $"{ApplicationEnvironment.AppName}:{ApplicationEnvironment.AppVersion}");

            builder
                .RegisterInstance(myNoSqlClient)
                .AsSelf()
                .SingleInstance();

            builder
                .RegisterAssetsDictionaryClients(myNoSqlClient);

            builder
                .RegisterSpotChangeBalanceGatewayClient(Program.Settings.ChangeBalanceGatewayGrpcServiceUrl);

            var bitgoClient =
                new BitGoClient(Program.Settings.BitgoAccessTokenReadOnly, Program.Settings.BitgoExpressUrl);

            builder
                .RegisterInstance(bitgoClient)
                .As<IBitGoClient>()
                .SingleInstance();

            builder.RegisterBitgoSettingsReader(myNoSqlClient);

            ServiceBusLogger = Program.LogFactory.CreateLogger(nameof(MyServiceBusTcpClient));

            var serviceBusClient = new MyServiceBusTcpClient(Program.ReloadedSettings(e => e.SpotServiceBusHostPort),
                ApplicationEnvironment.HostName);
            serviceBusClient.Log.AddLogException(ex =>
                ServiceBusLogger.LogInformation(ex, "Exception in MyServiceBusTcpClient"));
            serviceBusClient.Log.AddLogInfo(info => ServiceBusLogger.LogDebug($"MyServiceBusTcpClient[info]: {info}"));
            serviceBusClient.SocketLogs.AddLogInfo((context, msg) =>
                ServiceBusLogger.LogInformation(
                    $"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Info] {msg}"));
            serviceBusClient.SocketLogs.AddLogException((context, exception) =>
                ServiceBusLogger.LogInformation(exception,
                    $"MyServiceBusTcpClient[Socket {context?.Id}|{context?.ContextName}|{context?.Inited}][Exception] {exception.Message}"));
            builder.RegisterInstance(serviceBusClient).AsSelf().SingleInstance();

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


            builder
                .RegisterInstance(new MyNoSqlServerDataWriter<DepositAddressEntity>(
                    Program.ReloadedSettings(e => e.MyNoSqlWriterUrl), DepositAddressEntity.TableName, true))
                .As<IMyNoSqlServerDataWriter<DepositAddressEntity>>()
                .SingleInstance()
                .AutoActivate();

            builder
                .RegisterType<DepositsProcessingJob>()
                .AutoActivate()
                .SingleInstance()
                .AsSelf();
        }
    }
}