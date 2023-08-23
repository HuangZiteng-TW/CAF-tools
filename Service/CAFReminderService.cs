using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAF_tools.Common;
using CAF.Entity;
using Microsoft.Extensions.Logging;

namespace CAF_tools.Service;

public class CAFReminderService
{
    private readonly HangoutService _hangoutService;
    private readonly AzureTableService _azureTableService;
    private readonly ILogger<CAFReminderService> _log;

    public CAFReminderService(HangoutService hangoutService, AzureTableService azureTableService,
        ILogger<CAFReminderService> log)
    {
        _hangoutService = hangoutService;
        _azureTableService = azureTableService;
        _log = log;
    }

    private IEnumerable<Rotate> TeamDailyRotates(string rotateTableName)
    {
        var teamDailyRotates = _azureTableService.GetEntitysByPartitionKey<Rotate>(rotateTableName);
        if (teamDailyRotates == null || !teamDailyRotates.Any())
        {
            throw new RotateListNotExistException();
        }

        teamDailyRotates = teamDailyRotates.OrderBy(rotate => rotate.Order);
        return teamDailyRotates;
    }

    private async Task<Rotate> GetCurrentRotator(string rotateTableName)
    {
        var teamDailyRotates = TeamDailyRotates(rotateTableName);

        var curRotate = teamDailyRotates
            .FirstOrDefault(rotate => rotate.Status == (int)OwnerStatus.Cur);

        return curRotate;
    }

    private async Task<Rotate> GetNextRotator(string rotateTableName)
    {
        var teamDailyRotates = TeamDailyRotates(rotateTableName);

        var curRotate = teamDailyRotates.FirstOrDefault(rotate => rotate.Status == (int)OwnerStatus.Cur);
        if (curRotate != null)
        {
            curRotate.Status = (int)OwnerStatus.Done;
            await _azureTableService.UpdateEntityByRowKey(curRotate);
        }

        if (teamDailyRotates.All(rotate => rotate.Status == (int)OwnerStatus.Done))
        {
            foreach (var rotate in teamDailyRotates)
            {
                rotate.Status = (int)OwnerStatus.Wait;
                await _azureTableService.UpdateEntityByRowKey(rotate);
            }
        }

        var firstRotate = teamDailyRotates.FirstOrDefault(rotate => rotate.Status == (int)OwnerStatus.Skip);
        if (firstRotate == null)
        {
            firstRotate = teamDailyRotates.FirstOrDefault(rotate => rotate.Status == (int)OwnerStatus.Wait);
        }

        if (firstRotate == null)
        {
            throw new RotateNotExistException();
        }

        firstRotate.Status = (int)OwnerStatus.Cur;
        await _azureTableService.UpdateEntityByRowKey(firstRotate);

        return firstRotate;
    }

    private async Task SkipNextRotator(string rotateTableName, bool skipAndNext)
    {
        var teamDailyRotates = _azureTableService.GetEntitysByPartitionKey<Rotate>(rotateTableName);
        if (teamDailyRotates == null || !teamDailyRotates.Any())
        {
            throw new RotateListNotExistException();
        }

        var curRotate = teamDailyRotates.FirstOrDefault(rotate => rotate.Status == (int)OwnerStatus.Cur);
        Rotate firstRotate = null;
        if (curRotate != null)
        {
            if (skipAndNext)
            {
                curRotate.Status = (int)OwnerStatus.Skip;
                await _azureTableService.UpdateEntityByRowKey(curRotate);
                firstRotate = teamDailyRotates.FirstOrDefault(rotate => rotate.Status == (int)OwnerStatus.Wait);

                if (firstRotate == null)
                {
                    throw new RotateNotExistException();
                }
            }
            else
            {
            }
        }
    }

    public async Task SendReminder(ReminderCacheEntity reminderCache)
    {
        var reminderJobName = reminderCache.ReminderJobName.RowKey;
        var rotateTableName = reminderCache.RotateTableRef.Content;
        try
        {
            var teamDailyMsgTemplateContent = _azureTableService
                .GetEntityByRowKey<MsgTemplateEntity>(TablePartitionKey.MsgTemplate, reminderJobName)
                .Content;

            if (teamDailyMsgTemplateContent.Contains("<next_username>"))
            {
                var username = GetNextRotator(rotateTableName).Result;
                teamDailyMsgTemplateContent = teamDailyMsgTemplateContent.Replace("<next_username>", username.Content);
            }
            else if (teamDailyMsgTemplateContent.Contains("<cur_username>"))
            {
                var username = GetCurrentRotator(rotateTableName).Result;
                teamDailyMsgTemplateContent = teamDailyMsgTemplateContent.Replace("<cur_username>", username.Content);
            }

            var webhook = _azureTableService
                .GetEntityByRowKey<MsgTemplateEntity>(TablePartitionKey.HangsoutWebhooks, reminderJobName)
                .Content;

            await _hangoutService.SendMessage(webhook, teamDailyMsgTemplateContent);
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            throw;
        }
    }

    public class RotateListNotExistException : Exception
    {
        public RotateListNotExistException() : base("rotate list not exist!")
        {
        }
    }

    public class RotateNotExistException : Exception
    {
        public RotateNotExistException() : base("rotate cannot find!")
        {
        }
    }
}