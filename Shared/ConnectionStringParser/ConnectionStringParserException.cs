using System;
using System.Threading;

namespace Shared.ConnectionStringParser
{
    public class ConnectionStringParserException : Exception
    {
        public ConnectionStringParserException(string message) : base(message)
        {
        }
    }
}