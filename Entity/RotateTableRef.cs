using System;
using Azure;
using Azure.Data.Tables;
using CAF_tools.Common;

namespace CAF.Entity;

public class RotateTableRef : ITableEntity
{
    private OwnerStatus _status;

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public int Order { get; set; }

    public string Content { get; set; }
}