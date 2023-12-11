using System;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using CAF_tools.Common;

namespace CAF.Entity;

public class RotateNew : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public int Order { get; set; }

    public int Status { get; set; }

    private string _content;

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            RotateDetail = JsonSerializer.Deserialize<RotateDetail>(value);
        }
    }

    public RotateDetail RotateDetail { get; private set; }
}

public class RotateDetail
{
    public string Name { get; set; }
    public string HangsoutId { get; set; }
}