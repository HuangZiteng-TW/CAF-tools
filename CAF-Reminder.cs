using System;
using System.Threading.Tasks;
using CAF_tools.Service;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CAF.Reminder
{
    public class CAF_Reminder
    {
        private readonly CAFReminderService _cafReminderService;
        private readonly LocalMemeCache _localMemeCache;
        private readonly DateService _dateService;

        public CAF_Reminder(CAFReminderService cafReminderService,
            LocalMemeCache localMemeCache, DateService dateService)
        {
            _cafReminderService = cafReminderService;
            _localMemeCache = localMemeCache;
            _dateService = dateService;
        }

        [FunctionName("CAF-Reminder")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer, ILogger log)
        {
            
            if (_localMemeCache.TodayDayTypeDetailCache.isHoliday())
            {
                return;
            }

            var currentDatetime = _dateService.GetCurrentDatetime();
            log.LogInformation("log information: " + currentDatetime);
            Console.WriteLine("Console: " + currentDatetime);

            foreach (var reminderCache in _localMemeCache.ReminderCaches)
            {
                log.LogInformation(reminderCache.Schedule.ScheduleCron.ToString());
                Console.WriteLine(reminderCache.Schedule.ScheduleCron.ToString());
                if (reminderCache.Schedule.ScheduleCron.Match(currentDatetime))
                {
                    await _cafReminderService.SendReminder(reminderCache);
                }
            }
        }
    }
}