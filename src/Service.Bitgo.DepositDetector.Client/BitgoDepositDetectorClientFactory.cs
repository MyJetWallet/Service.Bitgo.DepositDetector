using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using JetBrains.Annotations;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Client;
using Service.Bitgo.DepositDetector.Grpc;

namespace Service.Bitgo.DepositDetector.Client
{
    [UsedImplicitly]
    public class BitgoDepositDetectorClientFactory
    {
        private readonly CallInvoker _channel;

        public BitgoDepositDetectorClientFactory(string assetsDictionaryGrpcServiceUrl)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(assetsDictionaryGrpcServiceUrl);
            _channel = channel.Intercept(new PrometheusMetricsInterceptor());
        }

        public IBitgoDepositTransferProcessService GetBitgoDepositTransferProcessService() => _channel.CreateGrpcService<IBitgoDepositTransferProcessService>();
    }


}
