namespace KustoDGrepConnect
{
    using Microsoft.Azure.Management.Kusto;
    using Microsoft.Rest.Azure;
    using Microsoft.Rest.Azure.Authentication;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Threading;

    class Program
    {
        static void Main(string[] args)
        {
            // Fill the following values:
            string tenantId = null; // "72f988bf-86f1-41af-91ab-2d7cd011db47"; //Directory (tenant) ID
                                                                      //XapLiteClientTest01 https://ms.portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/32ee9687-e7ef-43ba-8671-3bf5118cc0c3/isMSAApp/
            string clientId = null; // "32ee9687-e7ef-43ba-8671-3bf5118cc0c3"; //Application ID
            string clientSecret = null; // Client secret
            string subscriptionId = null; // "b01d3783-f526-474f-9c46-19c1080fdb7b";
            string resourceGroupName = null; // "portablexap";
            string clusterName = null; // "xapazuremdskusto";
            string dataConnectionName = null; // "XapazuremdskustoConnection";
            string location = null; // "West US";
            string genevaEnvironment = null; // "DiagnosticsProd";
            string[] mdsAccounts = null; // new[] { "XapAzure" };

            if(args.Length < 20)
            {
                ShowHelp();
                return;
            }

            for (int i = 0; i< args.Length; i++)
            {
                switch(args[i])
                {
                    case "-tenantId":
                        tenantId = args[i + 1];
                        i++;
                        break;
                    case "-clientId":
                        clientId = args[i + 1];
                        i++;
                        break;
                    case "-clientSecret":
                        clientSecret = args[i + 1];
                        i++;
                        break;
                    case "-subscriptionId":
                        subscriptionId = args[i + 1];
                        i++;
                        break;
                    case "-resourceGroupName":
                        resourceGroupName = args[i + 1];
                        i++;
                        break;
                    case "-clusterName":
                        clusterName = args[i + 1];
                        i++;
                        break;
                    case "-dataConnectionName":
                        dataConnectionName = args[i + 1];
                        i++;
                        break;
                    case "-location":
                        location = args[i + 1];
                        i++;
                        break;
                    case "-genevaEnvironment":
                        genevaEnvironment = args[i + 1];
                        i++;
                        break;
                    case "-mdsAccounts":
                        mdsAccounts = args[i + 1].Split(';');
                        i++;
                        break;
                    case "-help":
                        ShowHelp();
                        return;
                    default:
                        break;
                }
            }

            bool isScrubbed = false; // set to false if you want the data to be not scrubbed
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(
                tenantId,
                clientId,
                clientSecret).Result;
            var resourceManagementClient = new KustoManagementClient(serviceCreds)
            {
                SubscriptionId = subscriptionId
            };

            const string c_clusterDataConnectionResourceFormat = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Kusto/Clusters/{2}/Dataconnections/{3}?api-version={4}";
            var relativeUri = string.Format(
                c_clusterDataConnectionResourceFormat,
                subscriptionId,
                resourceGroupName,
                clusterName,
                dataConnectionName,
                "2019-11-09");
            var requestUri = new Uri(resourceManagementClient.BaseUri, relativeUri);

            var properties = new JObject();
            properties.Add("genevaEnvironment", genevaEnvironment);
            properties.Add("mdsAccounts", new JArray(mdsAccounts));
            properties.Add("isScrubbed", isScrubbed);
            var genevaLegacy = new JObject();
            genevaLegacy.Add("properties", properties);
            genevaLegacy.Add("kind", "GenevaLegacy");
            genevaLegacy.Add("location", location);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri))
            {
                httpRequest.Content = new StringContent(genevaLegacy.ToString(Newtonsoft.Json.Formatting.None), System.Text.Encoding.UTF8);
                httpRequest.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
                resourceManagementClient.Credentials.ProcessHttpRequestAsync(httpRequest, CancellationToken.None).Wait();
                using (var response = resourceManagementClient.HttpClient.SendAsync(httpRequest).Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return; // something went wrong
                    }

                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var azureOperationResponse = new AzureOperationResponse<JObject>
                    {
                        Request = httpRequest,
                        Response = response,
                        Body = JObject.Parse(responseContent)
                    };

                    var result = resourceManagementClient.GetLongRunningOperationResultAsync(azureOperationResponse,
                        customHeaders: null,
                        default(CancellationToken)).Result;
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine(
                "Usage:\r\n" +
                "    KustoDGrepConnect.exe -tenantId 72f988bf-86f1-41af-91ab-2d7cd011db47 -clientId 32ee9687-e7ef-43ba-8671-3bf5118cc0c3 -clientSecret *** -subscriptionId b01d3783-f526-474f-9c46-19c1080fdb7b  -resourceGroupName portablexap  -clusterName xapazuremdskusto  -dataConnectionName XapazuremdskustoConnection  -location 'West US'  -genevaEnvironment DiagnosticsProd  -mdsAccounts XapAzure;Account2;Account3\r\n\r\n" +
                "Options:\r\n" +
                "    -tenantId: 72f988bf-86f1-41af-91ab-2d7cd011db47 //Directory (tenant) ID\r\n" +
                "    -clientId: 32ee9687-e7ef-43ba-8671-3bf5118cc0c3 //Application ID\r\n" +
                "    -clientSecret: Client secret //e.g.:https://ms.portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/Overview/appId/32ee9687-e7ef-43ba-8671-3bf5118cc0c3/isMSAApp/\r\n" +
                "    -subscriptionId: b01d3783-f526-474f-9c46-19c1080fdb7b // Subscription ID\r\n" +
                "    -resourceGroupName: portablexap\r\n" +
                "    -clusterName: xapazuremdskusto\r\n" +
                "    -dataConnectionName: XapazuremdskustoConnection\r\n" +
                "    -location: 'West US'\r\n" +
                "    -genevaEnvironment: DiagnosticsProd\r\n" +
                "    -mdsAccounts: XapAzure;Account2;Account3...\r\n");
        }
    }
}

