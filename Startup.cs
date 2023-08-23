using System;
using System.Net.Http;
using Azure.Data.Tables;
using CAF_tools.Service;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CAF_tools.Startup))]

namespace CAF_tools
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton((s) => new HttpClient());

            builder.Services.AddSingleton((s) =>
            {
                return new TableClient(
                    new Uri(Environment.GetEnvironmentVariable("TableUri", EnvironmentVariableTarget.Process)),
                    Environment.GetEnvironmentVariable("TableName", EnvironmentVariableTarget.Process),
                    new TableSharedKeyCredential(
                        Environment.GetEnvironmentVariable("TableAccountName", EnvironmentVariableTarget.Process),
                        Environment.GetEnvironmentVariable("TableAccountKey", EnvironmentVariableTarget.Process)));
            });

            builder.Services.AddSingleton<DateService>();
            builder.Services.AddSingleton<AzureTableService>();
            builder.Services.AddSingleton<HangoutService>();
            builder.Services.AddSingleton<CAFReminderService>();
            builder.Services.AddSingleton<LocalMemeCache>();
        }
    }
}