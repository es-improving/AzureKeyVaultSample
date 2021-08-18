using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AzureKeyVault
{
    public class DynamicsConnector
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ILogger<DynamicsConnector> _logger;

        private static string _authString = "https://login.microsoftonline.com/";
        private string _tenantId = "";
        private string _clientId = "";
        private string _clientSecret = "";
        private string _tenantUrl = "";

        public bool PrettyPrintJson { get; set; }

        public DynamicsConnector(IConfiguration configuration, ILogger<DynamicsConnector> logger)
        {
            _tenantId = configuration.GetValue<string>("TenantId");
            _clientId = configuration.GetValue<string>("ClientId");
            _clientSecret = configuration.GetValue<string>("ClientSecret");
            _tenantUrl = configuration.GetValue<string>("TenantUrl");

            var webApiUrl = MakeBaseUrl();
            var authHeader = MakeAuthHeader(webApiUrl);

            _httpClient.BaseAddress = new Uri(webApiUrl);
            _httpClient.DefaultRequestHeaders.Authorization = authHeader;

            _logger = logger;

            PrettyPrintJson = true;
        }

        private AuthenticationHeaderValue MakeAuthHeader(string webApiUrl)
        {
            // Creates a context for login.microsoftonline.com (Azure AD common authentication)
            var authContextUrl = $"{_authString}{_tenantId}";
            var authContext = new AuthenticationContext(authContextUrl);

            // Creates a credential from the client id and secret
            var clientCredentials = new ClientCredential(_clientId, _clientSecret);

            // Requests a bearer token
            var authResult = authContext.AcquireTokenAsync(_tenantUrl, clientCredentials).Result;

            return new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        }

        private string MakeBaseUrl()
        {
            string url = _tenantUrl;
            string apiVersion = "9.2";
            string webApiUrl = $"{url}/api/data/v{apiVersion}/";
            return webApiUrl;
        }

        private void PrettyPrint(string json)
        {
            var jobj = JObject.Parse(json);
            _logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, jobj.ToString());
            Console.WriteLine(jobj.ToString());
        }

        private string GetResponseString(string uri)
        {

            _logger.LogInformation($"Sending request (HTTP GET)... {uri}");
            Console.WriteLine(uri);

            var response = _httpClient.GetAsync(uri).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Request successful.");

                //Get the response content and parse it.  
                string responseBody = response.Content.ReadAsStringAsync().Result;
                if (PrettyPrintJson)
                {
                    PrettyPrint(responseBody);
                }
                return responseBody;
            }
            else
            {
                Console.WriteLine("The request failed with a status of '{0}'", response.ReasonPhrase);
                return null;
            }
        }

        public JObject GetJObject(string uri)
        {
            var responseBody = GetResponseString(uri);
            return JObject.Parse(responseBody);
        }


    }

}
