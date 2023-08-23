using System;
using System.Collections.Generic;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace CAF.Entity;

public class Schedule : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    private string _content; 

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            ScheduleCron = JsonSerializer.Deserialize<ScheduleCron>(value);
        }
    }

    public ScheduleCron ScheduleCron { get; private set; }
}

public class ScheduleCron
{
    public IList<string> Year { get; set; }
    public IList<string> Month { get; set; }
    public IList<string> Weekday { get; set; }
    public IList<string> Day { get; set; }
    public IList<string> Hour { get; set; }
    public IList<string> Minute { get; set; }

    public override string ToString()
    {
        return "--- Year: " + String.Join(",", Year) +
               "--- Month: " + String.Join(",", Month) +
               "--- Weekday: " + String.Join(",", Weekday) +
               "--- Day: " + String.Join(",", Day) +
               "--- Hour: " + String.Join(",", Hour) +
               "--- Minute: " + String.Join(",", Minute);
    }

    public bool Match(DateTime datetime)
    {
        if ((Year.Contains("*") || Year.Contains(datetime.Year.ToString())) && // 1-9999
            (Month.Contains("*") || Month.Contains(datetime.Month.ToString())) && // 1-12
            ((Weekday.Contains("*") && Day.Contains("*")) ||
             (!Weekday.Contains("*") && Day.Contains("*") && Weekday.Contains(((int)datetime.DayOfWeek).ToString())) || // 0-6
             (Weekday.Contains("*") && !Day.Contains("*") && Day.Contains(datetime.Day.ToString()))) && // 1-31
            (Hour.Contains("*") || Hour.Contains(datetime.Hour.ToString())) && // 0-59
            (Minute.Contains("*") || Minute.Contains(datetime.Minute.ToString()))) // 0-59
        {
            return true;
        }

        return false;
    }
}