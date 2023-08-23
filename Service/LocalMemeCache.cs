using System;
using System.Collections.Generic;
using System.Linq;
using CAF_tools.Common;
using CAF.Dtos;
using CAF.Entity;
using Microsoft.Extensions.Logging;

namespace CAF_tools.Service;

public class LocalMemeCache
{
    private readonly AzureTableService _azureTableService;
    private readonly DateService _dateService;
    private readonly ILogger<LocalMemeCache> _log;

    public List<ReminderCacheEntity> ReminderCaches { get; } = new();

    private DayTypeDetail _todayDayTypeDetailCache;
    private DateTime _todayIsHolidayLastUpdateTime;

    public DayTypeDetail TodayDayTypeDetailCache
    {
        get
        {
            var currentDatetime = _dateService.GetCurrentDatetime();
            if (_todayIsHolidayLastUpdateTime != currentDatetime.Date)
            {
                _todayDayTypeDetailCache = _dateService.GetTodayDayTypeV2().Result;
                _todayIsHolidayLastUpdateTime = currentDatetime.Date;
            }

            return _todayDayTypeDetailCache;
        }
    }

    public LocalMemeCache(AzureTableService azureTableService, DateService dateService, ILogger<LocalMemeCache> log)
    {
        _azureTableService = azureTableService;
        _dateService = dateService;
        _log = log;

        RefreshReminderCache();
    }

    public void RefreshReminderCache()
    {
        var reminderJobNames = _azureTableService
            .GetEntitysByPartitionKey<ReminderJobName>(TablePartitionKey.ReminderJobName);
        foreach (var reminderJobName in reminderJobNames)
        {
            var schedule = _azureTableService
                .GetEntityByRowKey<Schedule>(TablePartitionKey.Schedule, reminderJobName.RowKey);
            var rotateTableRef = _azureTableService
                .GetEntityByRowKey<RotateTableRef>(TablePartitionKey.RotateTableRef, reminderJobName.RowKey);
            var reminderCache = ReminderCaches
                .SingleOrDefault(reminderCache => reminderCache.ReminderJobName.RowKey == reminderJobName.RowKey, null);
            _log.LogInformation("reminderJobNames : " + reminderJobName);
            if (reminderCache == null)
            {
                reminderCache = new ReminderCacheEntity();
                ReminderCaches.Add(reminderCache);
            }

            reminderCache.ReminderJobName = reminderJobName;
            reminderCache.Schedule = schedule;
            reminderCache.RotateTableRef = rotateTableRef;
        }

        _log.LogInformation("ReminderCaches count: " + ReminderCaches.Count);
    }
}

public class ReminderCacheEntity
{
    public ReminderJobName ReminderJobName { get; set; }
    public Schedule Schedule { get; set; }
    public RotateTableRef RotateTableRef { get; set; }

    public ReminderCacheEntity()
    {
    }

    public ReminderCacheEntity(ReminderJobName reminderJobName, Schedule schedule, RotateTableRef rotateTableRef)
    {
        ReminderJobName = reminderJobName;
        Schedule = schedule;
        RotateTableRef = rotateTableRef;
    }
}