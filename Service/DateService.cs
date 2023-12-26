using System;
using System.Net;
using System.Net.Http;
using CAF.Dtos;
using Microsoft.Extensions.Logging;

namespace CAF_tools.Service;

public class DateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DateService> _log;
    private readonly int utcHourOffset = 8;

    public DateService(HttpClient httpClient, ILogger<DateService> log)
    {
        _httpClient = httpClient;
        _log = log;
    }

    public DateTime GetCurrentDatetime()
    {
        var now = DateTime.Now;
        if (now.Second != 0)
        {
            now = now.AddSeconds(30);
        }

        return TimeZoneInfo.ConvertTimeToUtc(now).AddHours(utcHourOffset);
    }
}