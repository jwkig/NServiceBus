using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Shared;
using Shared.ConnectionStringParser;

namespace Sender
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));

                    logging.AddConsole();
                })
                .UseConsoleLifetime()
                .UseNServiceBus(context =>
                {
                    var endpointConfiguration = new EndpointConfiguration("Sender");

                    var connectionString = context.Configuration.GetConnectionString("RabbitMQBusConnectionString");

                    var parser = new ConnectionStringParser("amqp");

                    var opts = parser.Parse(connectionString);

                    var hosts = opts.Hosts.ToList();

                    opts.Hosts = hosts.Take(1).ToArray();
                    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>()
                        .ConnectionString(parser.Format(opts)).UseConventionalRoutingTopology(QueueType.Quorum);

                    foreach (var host in hosts.Skip(1))
                    {
                        if (host.Port.HasValue)
                        {
                            transport.AddClusterNode(host.Host, host.Port.Value, false);
                        }
                        else
                        {
                            transport.AddClusterNode(host.Host, false);
                        }
                    }

                    var routing = transport.Routing();

                    endpointConfiguration.AuditProcessedMessagesTo("audit");
                    routing.RouteToEndpoint(typeof(Ping), "Receiver");

                    // Operational scripting: https://docs.particular.net/transports/azure-service-bus/operational-scripting
                    endpointConfiguration.EnableInstallers();

                    return endpointConfiguration;
                })
                .ConfigureServices(services => services.AddHostedService<SenderWorker>())
                .Build();

            await host.RunAsync();
        }
    }
}
