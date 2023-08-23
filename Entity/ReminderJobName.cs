using System;
using Azure;
using Azure.Data.Tables;

namespace CAF.Entity;

public class ReminderJobName : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}