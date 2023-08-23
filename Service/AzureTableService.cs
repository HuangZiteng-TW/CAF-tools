using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace CAF_tools.Service;

public class AzureTableService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<AzureTableService> _log;

    public AzureTableService(TableClient tableClient, ILogger<AzureTableService> log)
    {
        _tableClient = tableClient;
        _log = log;
    }

    public IEnumerable<T> GetEntitysByPartitionKey<T>(string partitionKey) where T : class, ITableEntity
    {
        try
        {
            return _tableClient
                .Query<T>(filter: $"PartitionKey eq '{partitionKey}'");
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            throw;
        }
    }

    public T GetEntityByRowKey<T>(string partitionKey, string rowKey) where T : class, ITableEntity
    {
        try
        {
            return _tableClient
                .Query<T>(filter: $"PartitionKey eq '{partitionKey}'")
                .SingleOrDefault(entity => entity.RowKey == rowKey);
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            throw;
        }
    }

    public async Task UpdateEntityByRowKey(ITableEntity qEntity)
    {
        try
        {
            await _tableClient
                .UpdateEntityAsync(qEntity, qEntity.ETag);
        }
        catch (Exception e)
        {
            _log.LogError(e.Message);
            throw;
        }
    }
}