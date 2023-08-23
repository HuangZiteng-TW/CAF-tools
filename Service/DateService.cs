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
    private DateTime lastUpdateTime;
    private DayType lastUpdateDayType;
    private readonly int utcHourOffset = 8;

    public DateService(HttpClient httpClient, ILogger<DateService> log)
    {
        _httpClient = httpClient;
        _log = log;
        lastUpdateTime = new DateTime();
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

    public DayType GetTodayDayType()
    {
        var currentDatetime = GetCurrentDatetime();
        if (lastUpdateTime.Date == currentDatetime.Date)
        {
            return lastUpdateDayType;
        }

        try
        {
            _log.LogInformation("Date Service");
            var response = _httpClient
                .GetAsync(
                    $"http://apis.juhe.cn/fapig/calendar/day?date={currentDatetime.ToString("yyyy-MM-dd")}&detail=0&key=279e552ab087c6dc099d88dde41bad4c")
                .Result;
            _log.LogInformation("StatusCode : " + response.StatusCode);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _log.LogError(response.Content.ReadAsAsync<string>().Result);
                throw new Exception("send request to get day type error");
            }

            var tmpDayType = response.Content.ReadAsAsync<DayType>().Result;
            if (tmpDayType.ErrorCode == 0)
            {
                lastUpdateTime = currentDatetime.Date;
                lastUpdateDayType = tmpDayType;
            }
            else
            {
                _log.LogError($"Response return error. error code:{tmpDayType.ErrorCode}, reason:{tmpDayType.Reason}");
            }

            return new DayType();
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            throw;
        }
    }

    public DayType GetTodayDayTypeV2()
    {
        var currentDatetime = GetCurrentDatetime();
        try
        {
            _log.LogInformation("Date Service");
            var response = _httpClient
                .GetAsync(
                    $"http://apis.juhe.cn/fapig/calendar/day?date={currentDatetime.ToString("yyyy-MM-dd")}&detail=0&key=279e552ab087c6dc099d88dde41bad4c")
                .Result;
            _log.LogInformation("StatusCode : " + response.StatusCode);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _log.LogError(response.Content.ReadAsAsync<string>().Result);
                throw new Exception("send request to get day type error");
            }

            var tmpDayType = response.Content.ReadAsAsync<DayType>().Result;
            if (tmpDayType.ErrorCode == 0)
            {
                return tmpDayType;
            }

            _log.LogError($"Response return error. error code:{tmpDayType.ErrorCode}, reason:{tmpDayType.Reason}");
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
        }

        return new DayType();
    }
}