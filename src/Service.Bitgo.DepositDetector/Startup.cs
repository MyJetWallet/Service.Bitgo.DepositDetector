using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyJetWallet.Sdk.GrpcMetrics;
using MyJetWallet.Sdk.GrpcSchema;
using MyJetWallet.Sdk.Postgres;
using MyJetWallet.Sdk.Service;
using Prometheus;
using ProtoBuf.Grpc.Server;
using Service.Bitgo.DepositDetector.Grpc;
using Service.Bitgo.DepositDetector.Modules;
using Service.Bitgo.DepositDetector.Postgres;
using Service.Bitgo.DepositDetector.Services;
using SimpleTrading.BaseMetrics;
using SimpleTrading.ServiceStatusReporterConnector;

namespace Service.Bitgo.DepositDetector
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCodeFirstGrpc(options =>
            {
                options.Interceptors.Add<PrometheusMetricsInterceptor>();
                options.BindMetricsInterceptors();
            });

            services.AddHostedService<ApplicationLifetimeManager>();

            DatabaseContext.LoggerFactory = Program.LogFactory;
            services.AddDatabase(DatabaseContext.Schema, Program.Settings.PostgresConnectionString,
                o => new DatabaseContext(o));
            DatabaseContext.LoggerFactory = null;

            services.AddMyTelemetry("SP-", Program.Settings.ZipkinUrl);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseRouting();

            app.UseMetricServer();

            app.BindServicesTree(Assembly.GetExecutingAssembly());

            app.BindIsAlive();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcSchema<BitgoDepositTransferProcessService, IBitgoDepositTransferProcessService>();

                endpoints.MapGrpcSchema<BitgoDepositAddressService, IBitgoDepositAddressService>();

                endpoints.MapGrpcSchema<BitgoDepositService, IBitgoDepositService>();

                endpoints.MapGrpcSchemaRegistry();

                endpoints.MapGet("/",
                    async context =>
                    {
                        await context.Response.WriteAsync(
                            "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                    });
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule<SettingsModule>();
            builder.RegisterModule<ServiceModule>();
        }
    }
}