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

        private bool CheckAPIKeyValid(string apikey)
        {
            var bytes = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(apikey));
            var key = String.Empty;
            foreach (byte b in bytes)
            {
                key += $"{b:X2}";
            }

            return !_azureTableService.GetEntitysByPartitionKey<ApiKey>(TablePartitionKey.ApiKeyName)
                .All(apikey => String.Compare(apikey.Content, key, StringComparison.OrdinalIgnoreCase) != 0);
        }

        [FunctionName("reminder")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            try
            {
                if (!req.Query.ContainsKey("key"))
                {
                    return new BadRequestObjectResult("API key not exist.");
                }

                if (CheckAPIKeyValid(req.Query["key"].ToString()) == false)
                {
                    return new BadRequestObjectResult("Invalid api key.");
                }

                var reminderCache = _localMemeCache.GetCacheByReminderName(req.Query["remindername"].ToString());
                if (!req.Query.ContainsKey("remindername") || reminderCache == null)
                {
                    return new BadRequestObjectResult("Rotate name incorrect or not exist.");
                }

                if (req.Method.ToLower() == "get")
                {
                    await _cafReminderService.SendReminderWithoutUpdate(reminderCache);
                }
                else if (req.Method.ToLower() == "post")
                {
                    await _cafReminderService.UpdateRotateAndSendReminder(reminderCache);
                }
            }
            catch (Exception e)
            {
                log.LogError(e.StackTrace);
                throw;
            }

            return new OkObjectResult("Success.");
        }
    }
}