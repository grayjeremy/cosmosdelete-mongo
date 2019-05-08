using cosmosdbstaticclient.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;


namespace cosmosdbstaticclient
{
    /// <summary>
    /// This sample binds a custom array of <see cref="MyClass"/> objects from the HTTP Trigger body and uses a custom static <see cref="DocumentClient"/> to save the documents.
    /// </summary>
    /// <remarks>Sample payload is:
    /// {
    ///     "id": "SomeId",
    /// }
    /// </remarks>
    public static class HttpTriggerWithStaticClient
    {
        /*[{id: "1"},
{id: "2"},
{id: "3"},
{id: "4"},
{id: "5"},
{id: "6"},
{id: "7"}] */
        [FunctionName("HttpTriggerInsertDocuments")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            MyClass[] inputDocuments = JsonConvert.DeserializeObject<MyClass[]>(requestBody);

            if (inputDocuments == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            foreach (MyClass inputDocument in inputDocuments)
            {
                await Client.CreateDocumentAsync(CollectionUri, inputDocument);
                log.LogInformation(inputDocument.id);
            }

            return new HttpResponseMessage(HttpStatusCode.Created);
        }


        [FunctionName("HttpTriggerRemove3")]
        public static async Task<HttpResponseMessage> Run2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(Environment.GetEnvironmentVariable("CosmosDBDatabase"),  Environment.GetEnvironmentVariable("CosmosDBCollection"));

            var option = new FeedOptions { EnableCrossPartitionQuery = true };

            IDocumentQuery<MyClass> query = Client.CreateDocumentQuery<MyClass>(collectionUri,option)
           // .Where(a=> a.DateUpdated != "20190507")
            .AsDocumentQuery();

            

            while (query.HasMoreResults)  
            {
                foreach (MyClass result in await query.ExecuteNextAsync())
                {
                    log.LogInformation(result.id.ToString());
                    await Client.DeleteDocumentAsync(
                        UriFactory.CreateDocumentUri(Environment.GetEnvironmentVariable("CosmosDBDatabase"),  Environment.GetEnvironmentVariable("CosmosDBCollection"), result.id.ToString())
                        ,new RequestOptions { PartitionKey = new Microsoft.Azure.Documents.PartitionKey(result.id.ToString())}
                        );
                }
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }   

        
        private static Uri CollectionUri = UriFactory.CreateDocumentCollectionUri(
            Environment.GetEnvironmentVariable("CosmosDBDatabase"),
            Environment.GetEnvironmentVariable("CosmosDBCollection"));
        private static DocumentClient Client = GetCustomClient();
        private static DocumentClient GetCustomClient()
        {
            DocumentClient customClient = new DocumentClient(
                new Uri(Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint")), 
                Environment.GetEnvironmentVariable("CosmosDBAccountKey"),
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp,
                    // Customize retry options for Throttled requests
                    RetryOptions = new RetryOptions()
                    {
                        MaxRetryAttemptsOnThrottledRequests = 10,
                        MaxRetryWaitTimeInSeconds = 30
                    }
                });

            // Customize PreferredLocations
            customClient.ConnectionPolicy.PreferredLocations.Add(LocationNames.SouthCentralUS);
            customClient.ConnectionPolicy.PreferredLocations.Add(LocationNames.EastUS);

            return customClient;
        }
            
    }
}