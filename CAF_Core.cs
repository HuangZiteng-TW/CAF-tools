using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAF_tools.Common;
using CAF_tools.Service;
using CAF.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CAF.Core
{
    public class CAF_Core
    {
        private readonly CAFReminderService _cafReminderService;
        private readonly LocalMemeCache _localMemeCache;
        private readonly AzureTableService _azureTableService;
        private readonly ILogger<CAF_Core> _log;

        public CAF_Core(CAFReminderService cafReminderService, AzureTableService azureTableService,
            ILogger<CAF_Core> log, LocalMemeCache localMemeCache)
        {
            _cafReminderService = cafReminderService;
            _azureTableService = azureTableService;
            _log = log;
            _localMemeCache = localMemeCache;
        }

        // [FunctionName("CAF_Core")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            try
            {
                if (!req.Query.ContainsKey("key"))
                {
                    return new BadRequestObjectResult("Invalid api key.");
                }

                var bytes = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(req.Query["key"].ToString()));
                var key = String.Empty;
                foreach (byte b in bytes)
                {
                    key += $"{b:X2}";
                }

                if (_azureTableService.GetEntitysByPartitionKey<ApiKey>(TablePartitionKey.ApiKeyName)
                    .All(apikey => String.Compare(apikey.Content, key, StringComparison.OrdinalIgnoreCase) != 0))
                {
                    return new BadRequestObjectResult("Invalid api key.");
                }

                var reminderJobNames =
                    _azureTableService.GetEntitysByPartitionKey<ReminderJobName>(TablePartitionKey.ReminderJobName);
                foreach (var reminderJobName in reminderJobNames)
                {
                    var schedule = _azureTableService
                        .GetEntityByRowKey<Schedule>(TablePartitionKey.Schedule, reminderJobName.RowKey);
                }

                var teamDailyCache = _localMemeCache.ReminderCaches
                    .SingleOrDefault(cache => cache.ReminderJobName.RowKey == "TeamDaily");
                await _cafReminderService.SendReminder(teamDailyCache);
            }
            catch (Exception e)
            {
                log.LogError(e.StackTrace);
                throw;
            }


            // Console.WriteLine("Start.");
            // Pageable<TableEntity> queryResultsFilter =
            //     _tableClient.Query<TableEntity>(filter: $"PartitionKey eq '{PartitionKey}'");
            //
            // foreach (TableEntity qEntity in queryResultsFilter)
            // {
            //     Console.WriteLine($"{qEntity.GetString("Content")}");
            // }
            //
            // Console.WriteLine($"The query returned {queryResultsFilter.Count()} entities.");


            // var request = new Msg("???");
            // var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            // var response =
            //     await client.PostAsync(
            //         "https://chat.googleapis.com/v1/spaces/AAAAB-3o-pE/messages?key=AIzaSyDdI0hCZtE6vySjMm-WEfRq3CPzqKqqsHI&token=iLE7EFJa66wV3e0MacJaLTNNSfxFPDrLKbpv4kG2b5k",
            //         content);
            //
            // var responseString = await response.Content.ReadAsStringAsync();

            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}