using System;
using Azure;
using Azure.Data.Tables;
using CAF_tools.Common;

namespace CAF.Entity;

public class Rotate : ITableEntity
{
    private OwnerStatus _status;

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public int Order { get; set; }

    public int Status
    {
        get => (int)_status;
        set => _status = (OwnerStatus)value;
    }

    public OwnerStatus OwnerStatus
    {
        get { return _status; }
        set { _status = value; }
    }

    public string Content { get; set; }
}