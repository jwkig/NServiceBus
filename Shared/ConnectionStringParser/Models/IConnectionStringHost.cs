namespace Shared.ConnectionStringParser.Models
{
    public interface IConnectionStringHost
    {
        string Host { get; set; }
        int? Port { get; set; }
    }
}
