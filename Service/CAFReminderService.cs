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

    private IEnumerable<Rotate> GetAllRotates(string rotateTableName)
    {
        var teamDailyRotates = _azureTableService.GetEntitysByPartitionKey<Rotate>(rotateTableName)
            .Where(rotate => rotate.Status != OwnerStatus.Inactive);

        if (teamDailyRotates == null || !teamDailyRotates.Any())
        {
            throw new RotateListNotExistException();
        }

        return teamDailyRotates;
    }

    private List<Rotate> GetAllRotatorsWithOrder(string rotateTableName)
    {
        var teamDailyRotates = GetAllRotates(rotateTableName).OrderBy(rotate => rotate.Order);

        return teamDailyRotates.ToList();
    }

    private Rotate GetCurrentRotator(string rotateTableName)
    {
        var teamDailyRotates = GetAllRotates(rotateTableName);

        var curRotate = teamDailyRotates
            .FirstOrDefault(rotate => rotate.Status == (int)OwnerStatus.Cur);

        return curRotate;
    }

    private Rotate GetNextRotator(string rotateTableName)
    {
        var allRotatorsWithOrder = GetAllRotatorsWithOrder(rotateTableName);

        var curRotate = allRotatorsWithOrder.FirstOrDefault(rotate => rotate.Status == OwnerStatus.Cur);
        if (curRotate != null)
        {
            curRotate.Status = OwnerStatus.Done;
        }

        if (allRotatorsWithOrder.All(rotate => rotate.Status == OwnerStatus.Done || rotate.Status >= OwnerStatus.Skip))
        {
            foreach (var rotate in allRotatorsWithOrder)
            {
                rotate.Status = OwnerStatus.Wait;
            }
        }

        var firstRotate = allRotatorsWithOrder.FirstOrDefault(rotate => rotate.Status == OwnerStatus.Wait);

        if (firstRotate == null)
        {
            throw new RotateNotExistException();
        }

        firstRotate.Status = OwnerStatus.Cur;

        foreach (var rotate in allRotatorsWithOrder)
        {
            _azureTableService.UpdateEntityByRowKey(rotate);
        }

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
                _azureTableService.UpdateEntityByRowKey(curRotate);
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

    public async Task UpdateRotateAndSendReminder(ReminderCacheEntity reminderCache)
    {
        var reminderJobName = reminderCache.ReminderJobName.RowKey;
        var rotateTableName = reminderCache.RotateTableRef.Content;
        try
        {
            var emailTemplateContents = _azureTableService
                .GetEntityByRowKey<MsgTemplateEntity>(TablePartitionKey.MsgTemplate, reminderJobName)
                .Content;

            if (emailTemplateContents.Contains("<next_username>"))
            {
                var username = GetNextRotator(rotateTableName).Content;
                emailTemplateContents = emailTemplateContents.Replace("<next_username>", username);
            }

            if (emailTemplateContents.Contains("<cur_username>"))
            {
                var username = GetCurrentRotator(rotateTableName).Content;
                emailTemplateContents = emailTemplateContents.Replace("<cur_username>", username);
            }

            if (emailTemplateContents.Contains("<all_username>"))
            {
                var allUsername = string.Join(", ",
                    GetAllRotatorsWithOrder(rotateTableName).Select(rotate => rotate.RowKey));
                emailTemplateContents = emailTemplateContents.Replace("<all_username>", allUsername);
            }

            var webhook = _azureTableService
                .GetEntityByRowKey<HangsoutWebhooks>(TablePartitionKey.HangsoutWebhooks, reminderJobName)
                .Content;

            await _hangoutService.SendMessage(webhook, emailTemplateContents);
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            throw;
        }
    }

    public async Task SendReminderWithoutUpdate(ReminderCacheEntity reminderCache)
    {
        var reminderJobName = reminderCache.ReminderJobName.RowKey;
        var rotateTableName = reminderCache.RotateTableRef.Content;
        try
        {
            var emailTemplateContents = _azureTableService
                .GetEntityByRowKey<MsgTemplateEntity>(TablePartitionKey.MsgTemplate, reminderJobName)
                .Content;

            if (emailTemplateContents.Contains("<next_username>") || emailTemplateContents.Contains("<cur_username>"))
            {
                var username = GetCurrentRotator(rotateTableName).Content;
                emailTemplateContents = emailTemplateContents.Replace("<next_username>", username);
                emailTemplateContents = emailTemplateContents.Replace("<cur_username>", username);
            }

            if (emailTemplateContents.Contains("<all_username>"))
            {
                var allUsername = string.Join(", ",
                    GetAllRotatorsWithOrder(rotateTableName).Select(rotate => rotate.RowKey));
                emailTemplateContents = emailTemplateContents.Replace("<all_username>", allUsername);
            }

            var webhook = _azureTableService
                .GetEntityByRowKey<HangsoutWebhooks>(TablePartitionKey.HangsoutWebhooks, reminderJobName)
                .Content;

            await _hangoutService.SendMessage(webhook, emailTemplateContents);
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