using AarhusSpaceProgram.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AarhusSpaceProgram.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicyNames.ManagerOnly)]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly IMongoCollection<BsonDocument> _logsCollection;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IConfiguration configuration, ILogger<LogsController> logger)
    {
        _logger = logger;

        var mongoUrlValue = configuration["Serilog:MongoDbUrl"];
        if (string.IsNullOrWhiteSpace(mongoUrlValue))
        {
            throw new InvalidOperationException("Missing Serilog:MongoDbUrl configuration.");
        }

        var mongoUrl = MongoUrl.Create(mongoUrlValue);
        var databaseName = configuration["Serilog:MongoDbDatabase"] ?? mongoUrl.DatabaseName;
        var collectionName = configuration["Serilog:MongoDbCollection"];

        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(collectionName))
        {
            throw new InvalidOperationException(
                "Missing Serilog MongoDB database or collection configuration.");
        }

        var client = new MongoClient(mongoUrl);
        var database = client.GetDatabase(databaseName);
        _logsCollection = database.GetCollection<BsonDocument>(collectionName);
    }

    [HttpGet]
    public async Task<ActionResult> GetLogs([FromQuery] int limit = 50)
    {
        try
        {
            var requestFilter = BuildRequestFilter();
            var logs = await _logsCollection
                .Find(requestFilter)
                .Sort(Builders<BsonDocument>.Sort.Descending("UtcTimeStamp"))
                .Limit(limit)
                .ToListAsync();

            return Ok(new
            {
                count = logs.Count,
                logs = logs.Select(log => new
                {
                    timestamp = log.GetValue("UtcTimeStamp", BsonNull.Value).ToUniversalTime(),
                    method = GetPropertyValue(log, "RequestMethod"),
                    path = GetPropertyValue(log, "RequestPath"),
                    statusCode = GetPropertyValue(log, "StatusCode"),
                    level = log.GetValue("Level", BsonNull.Value).ToString()
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs from MongoDB");
            return StatusCode(500, new { error = "Failed to retrieve logs", message = ex.Message });
        }
    }

    [HttpGet("recent")]
    public async Task<ActionResult> GetRecentLogs([FromQuery] int minutes = 30, [FromQuery] int limit = 100)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);

            var filter = Builders<BsonDocument>.Filter.And(
                BuildRequestFilter(),
                Builders<BsonDocument>.Filter.Gte("UtcTimeStamp", cutoffTime)
            );

            var logs = await _logsCollection
                .Find(filter)
                .Sort(Builders<BsonDocument>.Sort.Descending("UtcTimeStamp"))
                .Limit(limit)
                .ToListAsync();

            return Ok(new
            {
                timeRange = $"Last {minutes} minutes",
                count = logs.Count,
                logs = logs.Select(log => new
                {
                    timestamp = log.GetValue("UtcTimeStamp", BsonNull.Value).ToUniversalTime(),
                    message = log.GetValue("MessageTemplate", BsonNull.Value).ToString(),
                    method = GetPropertyValue(log, "RequestMethod"),
                    path = GetPropertyValue(log, "RequestPath"),
                    statusCode = GetPropertyValue(log, "StatusCode")
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent logs from MongoDB");
            return StatusCode(500, new { error = "Failed to retrieve recent logs", message = ex.Message });
        }
    }

    [HttpGet("count")]
    public async Task<ActionResult> GetLogsCount()
    {
        try
        {
            var requestFilter = BuildRequestFilter();
            var totalCount = await _logsCollection.CountDocumentsAsync(requestFilter);
            var methodCounts = await _logsCollection.Aggregate()
                .Match(requestFilter)
                .Group(new BsonDocument
                {
                    { "_id", "$Properties.RequestMethod" },
                    { "count", new BsonDocument("$sum", 1) }
                })
                .ToListAsync();

            return Ok(new
            {
                totalLogs = totalCount,
                byMethod = methodCounts.Select(m => new
                {
                    method = m["_id"].ToString(),
                    count = m["count"].ToInt32()
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting logs in MongoDB");
            return StatusCode(500, new { error = "Failed to count logs", message = ex.Message });
        }
    }

    private static FilterDefinition<BsonDocument> BuildRequestFilter()
    {
        return Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Exists("Properties.RequestMethod"),
            Builders<BsonDocument>.Filter.Exists("Properties.RequestPath"),
            Builders<BsonDocument>.Filter.Exists("Properties.StatusCode"),
            Builders<BsonDocument>.Filter.In("Properties.RequestMethod", new[] { "POST", "PUT", "DELETE" })
        );
    }

    private static string? GetPropertyValue(BsonDocument log, string propertyName)
    {
        if (!log.Contains("Properties"))
        {
            return null;
        }

        var properties = log["Properties"].AsBsonDocument;
        return properties.Contains(propertyName) ? properties[propertyName].ToString() : null;
    }
}
