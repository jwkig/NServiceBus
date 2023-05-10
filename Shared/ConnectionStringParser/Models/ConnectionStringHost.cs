namespace Shared.ConnectionStringParser.Models
{
    public class ConnectionStringHost : IConnectionStringHost
    {
        public string Host { get; set; }
        public int? Port { get; set; }
    }
}