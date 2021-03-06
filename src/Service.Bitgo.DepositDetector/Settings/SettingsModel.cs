using MyYamlParser;

namespace Service.Bitgo.DepositDetector.Settings
{
    public class SettingsModel
    {
        [YamlProperty("BitgoDepositDetector.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("BitgoDepositDetector.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }

        [YamlProperty("BitgoDepositDetector.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("BitgoDepositDetector.ChangeBalanceGatewayGrpcServiceUrl")]
        public string ChangeBalanceGatewayGrpcServiceUrl { get; set; }
        
        [YamlProperty("BitgoDepositDetector.BitgoAccessTokenMainNet")]
        public string BitgoAccessTokenMainNet { get; set; }

        [YamlProperty("BitgoDepositDetector.BitgoExpressUrlMainNet")]
        public string BitgoExpressUrlMainNet { get; set; }
        
        [YamlProperty("BitgoDepositDetector.BitgoAccessTokenTestNet")]
        public string BitgoAccessTokenTestNet { get; set; }

        [YamlProperty("BitgoDepositDetector.BitgoExpressUrlTestNet")]
        public string BitgoExpressUrlTestNet { get; set; }

        [YamlProperty("BitgoDepositDetector.DefaultBrokerId")]
        public string DefaultBrokerId { get; set; }

        [YamlProperty("BitgoDepositDetector.SpotServiceBusHostPort")]
        public string SpotServiceBusHostPort { get; set; }

        [YamlProperty("BitgoDepositDetector.MaxClientInCache")]
        public int MaxClientInCache { get; set; }

        [YamlProperty("BitgoDepositDetector.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("BitgoDepositDetector.PostgresConnectionString")]
        public string PostgresConnectionString { get; set; }

        [YamlProperty("BitgoDepositDetector.DepositsProcessingIntervalSec")]
        public int DepositsProcessingIntervalSec { get; set; }

        [YamlProperty("BitgoDepositDetector.DepositsRetriesLimit")]
        public int DepositsRetriesLimit { get; set; }

        [YamlProperty("BitgoDepositDetector.GenerateAddressesIntervalSec")]
        public int GenerateAddressesIntervalSec { get; set; }

        [YamlProperty("BitgoDepositDetector.PreGeneratedAddressesCount")]
        public int PreGeneratedAddressesCount { get; set; }
    }
}