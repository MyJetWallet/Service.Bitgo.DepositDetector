using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.DataReader;
using MyServiceBus.TcpClient;

namespace Service.Bitgo.DepositDetector
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyNoSqlTcpClient _myNoSqlClient;
        private readonly MyServiceBusTcpClient _myServiceBusTcpClient;

        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime, ILogger<ApplicationLifetimeManager> logger,
            MyNoSqlTcpClient myNoSqlClient,
            MyServiceBusTcpClient myServiceBusTcpClient)
            : base(appLifetime)
        {
            _logger = logger;
            _myNoSqlClient = myNoSqlClient;
            _myServiceBusTcpClient = myServiceBusTcpClient;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _myNoSqlClient.Start();
            _logger.LogInformation("MyNoSqlTcpClient is started.");
            _myServiceBusTcpClient.Start();
            _logger.LogInformation("MyServiceBusTcpClient is started.");
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _myNoSqlClient.Stop();
            _logger.LogInformation("MyNoSqlTcpClient is stopped.");
            _myServiceBusTcpClient.Stop();
            _logger.LogInformation("MyServiceBusTcpClient is stopped.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
