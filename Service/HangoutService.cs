using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CAF.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CAF_tools.Service;

public class HangoutService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HangoutService> _log;

    public HangoutService(HttpClient httpClient, ILogger<HangoutService> log)
    {
        _httpClient = httpClient;
        _log = log;
    }

    public async Task SendMessage(string webhook, string text)
    {
        try
        {
            var request = new HangoutMsg(text);
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhook, content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseString);
                _log.LogError(responseString);
                throw new Exception("send msg to google chat error");
            }
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            throw;
        }
    }
}