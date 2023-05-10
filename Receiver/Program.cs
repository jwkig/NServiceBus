using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Shared.ConnectionStringParser;
using Shared.ConnectionStringParser.Models;

namespace Receiver
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
                    var endpointConfiguration = new EndpointConfiguration("Receiver");

                    var connectionString = context.Configuration.GetConnectionString("RabbitMQBusConnectionString");
                    var parser = new ConnectionStringParser("amqp");

                    var opts = parser.Parse(connectionString);

                    var hosts = opts.Hosts.ToList();

                    opts.Hosts = hosts.Take(1).ToArray();
                    var transport = endpointConfiguration.UseTransport<RabbitMQTransport>().ConnectionString(parser.Format(opts)).UseConventionalRoutingTopology(QueueType.Quorum);
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

                    endpointConfiguration.AuditProcessedMessagesTo("audit");

                    // Operational scripting: https://docs.particular.net/transports/azure-service-bus/operational-scripting
                    endpointConfiguration.EnableInstallers();

                    return endpointConfiguration;
                })
                .Build();

            await host.RunAsync();
        }
    }
}
