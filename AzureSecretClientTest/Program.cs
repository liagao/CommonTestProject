using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;

namespace AzureSecretClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            /*var client = new SecretClient(new Uri("https://xapkeyvault-prod.vault.azure.net/"), 
                                            new ClientSecretCredential("72f988bf-86f1-41af-91ab-2d7cd011db47", 
                                                                        "dd8153f6-a67c-4b41-9646-a53e5a76329f",
                                                                        "0x3lii20R.5E5S14GRrq5o-RE_ljW5B9.c"));*/
            var client = new SecretClient(new Uri("https://xapkeyvault-prod.vault.azure.net/"), new DefaultAzureCredential());
            KeyVaultSecret secret = client.GetSecret("XapCosmosAadSecret");
            Console.WriteLine(secret.Value);

            Console.ReadLine();
        }
    }
}
