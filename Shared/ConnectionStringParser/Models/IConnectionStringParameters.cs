using System.Collections.Generic;

namespace Shared.ConnectionStringParser.Models
{
    public interface IConnectionStringParameters
    {
        string Scheme { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
        IConnectionStringHost[] Hosts { get; set; }
        string Endpoint { get; set; }
        IDictionary<string, string> Options { get; set; }
    }
}