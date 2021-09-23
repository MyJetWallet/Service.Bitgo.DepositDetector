using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.DataReader;
using MyServiceBus.TcpClient;
using Service.Bitgo.DepositDetector.Jobs;

namespace Service.Bitgo.DepositDetector
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly DepositsProcessingJob _depositsProcessingJob;
        private readonly DepositAddressesGenerationJob _depositAddressesGenerationJob;
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyNoSqlClientLifeTime _myNoSqlClient;
        private readonly MyServiceBusTcpClient _myServiceBusTcpClient;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime,
            ILogger<ApplicationLifetimeManager> logger, 
            MyNoSqlClientLifeTime myNoSqlClient,
            MyServiceBusTcpClient myServiceBusTcpClient, 
            DepositsProcessingJob depositsProcessingJob,
            DepositAddressesGenerationJob depositAddressesGenerationJob) : base(
            appLifetime)
        {
            _logger = logger;
            _myNoSqlClient = myNoSqlClient;
            _myServiceBusTcpClient = myServiceBusTcpClient;
            _depositsProcessingJob = depositsProcessingJob;
            _depositAddressesGenerationJob = depositAddressesGenerationJob;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called");
            _myNoSqlClient.Start();
            _logger.LogInformation("MyNoSqlTcpClient is started");
            _myServiceBusTcpClient.Start();
            _logger.LogInformation("MyServiceBusTcpClient is started");
            _depositsProcessingJob.Start();
            _logger.LogInformation("DepositsProcessingJob is started");
            _depositAddressesGenerationJob.Start();
            _logger.LogInformation("DepositAddressesGenerationJob is started");
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called");
            _myNoSqlClient.Stop();
            _logger.LogInformation("MyNoSqlTcpClient is stopped");
            _myServiceBusTcpClient.Stop();
            _logger.LogInformation("MyServiceBusTcpClient is stopped");
            _depositsProcessingJob.Stop();
            _logger.LogInformation("DepositsProcessingJob is stopped");
            _depositAddressesGenerationJob.Stop();
            _logger.LogInformation("DepositAddressesGenerationJob is stopped");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called");
        }
    }
}