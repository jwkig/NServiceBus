using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Shared.ConnectionStringParser.Models;

namespace Shared.ConnectionStringParser
{
    public class ConnectionStringParser
    {
        private static readonly string DefaultScheme = "db";
        private readonly string _scheme;


        public ConnectionStringParser(string scheme = null)
        {
            _scheme = scheme ?? DefaultScheme;
        }

        public ConnectionStringParser(IConnectionStringParameters options = null)
        {
            _scheme = options?.Scheme ?? DefaultScheme;
        }

        /// <summary>
        /// Takes a connection string object and returns a URI string of the form:
        /// scheme://[username[:password]@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[endpoint]][?options]
        /// </summary>
        /// <param name="connectionStringObject">The object that describes connection string parameters</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string Format(IConnectionStringParameters connectionStringObject = null)
        {
            if (connectionStringObject == null)
            {
                return $"{_scheme}://localhost";
            }

            if (!string.IsNullOrEmpty(_scheme) && !string.IsNullOrEmpty(connectionStringObject.Scheme) &&
                !string.Equals(_scheme, connectionStringObject.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConnectionStringParserException($"Scheme not supported: {connectionStringObject.Scheme}");
            }

            var uri = $"{_scheme ?? connectionStringObject.Scheme ?? DefaultScheme}://";

            if (!string.IsNullOrEmpty(connectionStringObject.UserName))
            {
                uri += Uri.EscapeDataString(connectionStringObject.UserName);
                // Allow empty passwords
                if (!string.IsNullOrEmpty(connectionStringObject.Password))
                {
                    uri += $":{Uri.EscapeDataString(connectionStringObject.Password)}";
                }

                uri += "@";
            }

            uri += FormatAddress(connectionStringObject);
            // Only put a slash when there is an endpoint
            if (!string.IsNullOrEmpty(connectionStringObject.Endpoint))
            {
                uri += $"/{Uri.EscapeDataString(connectionStringObject.Endpoint)}";
            }

            if (connectionStringObject.Options != null
                && connectionStringObject.Options.Any())
            {
                uri +=
                    $"?{string.Join("&", connectionStringObject.Options.Select(option => $"{Uri.EscapeDataString(option.Key)}={Uri.EscapeDataString(option.Value)}").ToArray())}";
            }

            return uri;
        }

        /// <summary>
        /// Takes a connection string URI of form:
        /// scheme://[username[:password]@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[endpoint]][?options]
        /// </summary>
        /// <param name="uri">The connection string URI</param>
        /// <returns></returns>
        /// <exception cref="ConnectionStringParserException"></exception>
        public IConnectionStringParameters Parse(string uri)
        {
            var connectionStringParser = new Regex(
                "^\\s*" + // Optional whitespace padding at the beginning of the line
                "([^:]+)://" + // Scheme (Group 1)
                "(?:([^:@,/?=&]+)(?::([^:@,/?=&]+))?@)?" + // User (Group 2) and Password (Group 3)
                "([^@/?=&]+)" + // Host address(es) (Group 4)
                "(?:/([^:@,/?=&]+)?)?" + // Endpoint (Group 5)
                "(?:\\?([^:@,/?]+)?)?" + // Options (Group 6)
                "\\s*$", // Optional whitespace padding at the end of the line
                RegexOptions.IgnoreCase);

            var connectionStringObject = new ConnectionStringParameters();

            if (!uri.Contains("://"))
            {
                throw new ConnectionStringParserException($"No scheme found in URI {uri}");
            }

            var tokens = connectionStringParser.Matches(uri).FirstOrDefault()?.Groups;

            if (tokens != null && tokens.Count > 0)
            {
                connectionStringObject.Scheme = tokens[1].Value;
                if (!string.IsNullOrEmpty(_scheme) && !string.Equals(_scheme, connectionStringObject.Scheme,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConnectionStringParserException($"URI must start with '{_scheme}://'");
                }

                connectionStringObject.UserName = !string.IsNullOrEmpty(tokens[2].Value)
                    ? Uri.UnescapeDataString(tokens[2].Value)
                    : tokens[2].Value;
                connectionStringObject.Password = !string.IsNullOrEmpty(tokens[3].Value)
                    ? Uri.UnescapeDataString(tokens[3].Value)
                    : tokens[3].Value;
                connectionStringObject.Hosts = ParseAddress(tokens[4].Value);
                connectionStringObject.Endpoint = !string.IsNullOrEmpty(tokens[5].Value)
                    ? Uri.UnescapeDataString(tokens[5].Value)
                    : tokens[5].Value;
                connectionStringObject.Options = !string.IsNullOrEmpty(tokens[6].Value) ? ParseOptions(tokens[6].Value) : null;
            }

            return connectionStringObject;
        }

        /// <summary>
        /// Formats the address portion of a connection string
        /// </summary>
        /// <param name="connectionStringObject">The object that describes connection string parameters</param>
        /// <returns></returns>
        private string FormatAddress(IConnectionStringParameters connectionStringObject)
        {
            return string.Join(",", connectionStringObject.Hosts
                .Select(address =>
                    $"{Uri.EscapeDataString(address.Host)}{(address.Port.HasValue ? $":{Uri.EscapeDataString(address.Port.ToString())}" : "")}")
                .ToArray());
        }

        /// <summary>
        /// Parses an address
        /// </summary>
        /// <param name="addresses">The address(es) to process</param>
        /// <returns></returns>
        private IConnectionStringHost[] ParseAddress(string addresses)
        {
            return addresses.Split(",")
                .Select(address =>
                {
                    var i = address.IndexOf(":", StringComparison.OrdinalIgnoreCase);
                    var host = new ConnectionStringHost
                    {
                        Host = i >= 0 ? Uri.UnescapeDataString(address[..i]) : Uri.UnescapeDataString(address),
                        Port = i >= 0 ? (int?) Convert.ToInt32(address[(i + 1)..]) : null
                    };

                    return host;
                })
                .ToArray();
        }

        /// <summary>
        /// Parses options
        /// </summary>
        /// <param name="options">The options to process</param>
        /// <returns></returns>
        private IDictionary<string, string> ParseOptions(string options)
        {
            var result = new Dictionary<string, string>();

            options.Split("&")
                .ToList()
                .ForEach(option =>
                {
                    var i = option.IndexOf("=", StringComparison.OrdinalIgnoreCase);

                    if (i >= 0)
                    {
                        result[Uri.UnescapeDataString(option[..i])] = Uri.UnescapeDataString(option[(i + 1)..]);
                    }
                });
            return result;
        }
    }
}
