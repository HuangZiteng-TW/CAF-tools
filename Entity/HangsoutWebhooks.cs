using System;
using Azure;
using Azure.Data.Tables;

namespace CAF.Entity;

public class HangsoutWebhooks : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public int Order { get; set; }

    public string Content { get; set; }
}