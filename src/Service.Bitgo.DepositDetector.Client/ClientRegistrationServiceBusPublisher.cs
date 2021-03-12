using System.Threading.Tasks;
using DotNetCoreDecorators;
using MyJetWallet.Domain.ServiceBus.Serializers;
using MyServiceBus.TcpClient;
using Service.Bitgo.DepositDetector.Domain.Models;

namespace Service.Bitgo.DepositDetector.Client
{
    public class ClientRegistrationServiceBusPublisher : IPublisher<SignalBitGoTransfer>
    {
        private readonly MyServiceBusTcpClient _client;

        public ClientRegistrationServiceBusPublisher(MyServiceBusTcpClient client)
        {
            _client = client;
            _client.CreateTopicIfNotExists(SignalBitGoTransfer.ServiceBusTopicName);
        }

        public async ValueTask PublishAsync(SignalBitGoTransfer valueToPublish)
        {
            var bytesToSend = valueToPublish.ServiceBusContractToByteArray();
            await _client.PublishAsync(SignalBitGoTransfer.ServiceBusTopicName, bytesToSend, true);
        }
    }
}
