using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MongoDB.Driver;
using cosmosdbstaticclient.Models;
using System.Collections.Generic;

namespace Company.Function
{
    public static class HttpTriggerMongo
    {
        [FunctionName("SeedMongo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                var client = new MongoClient(System.Environment.GetEnvironmentVariable("MongoDBAtlasConnectionString"));
                var database = client.GetDatabase(System.Environment.GetEnvironmentVariable("MongoDBDatabaseName"));
                var collection = database.GetCollection<MyClass>(System.Environment.GetEnvironmentVariable("MongoDBCollectionName"));     

                var stuff = new MyClass { id = "1", DateUpdated = System.DateTime.Now};
                
                List<MyClass> lotsOfStuff = new List<MyClass>{                    
                    new MyClass { id = "2", DateUpdated = System.DateTime.Now},
                    new MyClass { id = "3", DateUpdated = System.DateTime.Now},
                    new MyClass { id = "4", DateUpdated = System.DateTime.Now},
                    new MyClass { id = "5", DateUpdated = System.DateTime.Now},
                    new MyClass { id = "6", DateUpdated = System.DateTime.Now},
                    new MyClass { id = "7", DateUpdated = System.DateTime.Now},
                };

                await collection.InsertOneAsync(stuff);
                await collection.InsertManyAsync(lotsOfStuff);
            }   
            catch( Exception e)
            {
                return new BadRequestObjectResult("Error refreshing demo - " + e.Message);
            }

            return (ActionResult)new OkObjectResult("Refreshed Demo database");            
        }

        [FunctionName("DeleteMongo")]
        public static async Task<IActionResult> Run2(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
            {
                var client = new MongoClient(System.Environment.GetEnvironmentVariable("MongoDBAtlasConnectionString"));
                var database = client.GetDatabase(System.Environment.GetEnvironmentVariable("MongoDBDatabaseName"));
                var collection = database.GetCollection<MyClass>(System.Environment.GetEnvironmentVariable("MongoDBCollectionName"));     

                /*
                //Potentially pass in a date here
                string date = req.Query["date"];

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                date = date ?? data?.date;
                */
                var filter = Builders<MyClass>.Filter.Where(a=>a.DateUpdated < System.DateTime.Now.AddDays(1));

                await collection.DeleteManyAsync(filter);

                return (ActionResult)new OkObjectResult("Refreshed Demo database");     
            }
    }
}
