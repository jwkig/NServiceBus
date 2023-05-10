using System.Collections.Generic;

namespace Shared.ConnectionStringParser.Models
{
    public class ConnectionStringParameters : IConnectionStringParameters
    {
        public string Scheme { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public IConnectionStringHost[] Hosts { get; set; }
        public string Endpoint { get; set; }
        public IDictionary<string, string> Options { get; set; }
    }
}