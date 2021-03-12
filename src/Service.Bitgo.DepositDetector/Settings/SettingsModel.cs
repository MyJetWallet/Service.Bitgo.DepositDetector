using MyJetWallet.BitGo;
using SimpleTrading.SettingsReader;

namespace Service.Bitgo.DepositDetector.Settings
{
    [YamlAttributesOnly]
    public class SettingsModel
    {
        [YamlProperty("BitgoDepositDetector.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("BitgoDepositDetector.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }

        [YamlProperty("BitgoDepositDetector.ChangeBalanceGatewayGrpcServiceUrl")]
        public string ChangeBalanceGatewayGrpcServiceUrl { get; set; }

        [YamlProperty("BitgoDepositDetector.BitgoAccessTokenReadOnly")]
        public string BitgoAccessTokenReadOnly { get; set; }

        [YamlProperty("BitgoDepositDetector.BitgoExpressUrl")]
        public string BitgoExpressUrl { get; set; }

        [YamlProperty("BitgoDepositDetector.DefaultBrokerId")]
        public string DefaultBrokerId { get; set; }

        [YamlProperty("BitgoDepositDetector.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }
    }
}