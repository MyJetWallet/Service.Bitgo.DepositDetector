using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyJetWallet.Domain.ServiceBus;
using MyJetWallet.Domain.ServiceBus.Serializers;
using MyServiceBus.Abstractions;
using MyServiceBus.TcpClient;
using Service.Bitgo.DepositDetector.Domain.Models;

namespace Service.Bitgo.DepositDetector.ServiceBus
{
    public class SignalBitGoTransferSubscriber : ISubscriber<SignalBitGoTransfer>
    {
        private readonly List<Func<SignalBitGoTransfer, ValueTask>> _list = new List<Func<SignalBitGoTransfer, ValueTask>>();

        public SignalBitGoTransferSubscriber(
            MyServiceBusTcpClient client,
            string queueName,
            TopicQueueType queryType)
        {

            client.Subscribe(SignalBitGoTransfer.ServiceBusTopicName, queueName, queryType, Handler);
        }

        private async ValueTask Handler(IMyServiceBusMessage data)
        {
            var item = Deserializer(data.Data);

            foreach (var callback in _list)
            {
                await callback.Invoke(item);
            }
        }


        public void Subscribe(Func<SignalBitGoTransfer, ValueTask> callback)
        {
            this._list.Add(callback);
        }

        private SignalBitGoTransfer Deserializer(ReadOnlyMemory<byte> data) => data.ByteArrayToServiceBusContract<SignalBitGoTransfer>();
    }
}