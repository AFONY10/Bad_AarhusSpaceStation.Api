using AarhusSpaceProgram.Api.Models;
using MongoDB.Driver;

namespace AarhusSpaceProgram.Api.Services;

public class MongoMissionLogRepository : IMissionLogRepository
{
    private readonly IMongoCollection<MissionLog> _collection;

    public MongoMissionLogRepository(IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];
        var databaseName = configuration["MongoDb:DatabaseName"];
        var collectionName = configuration["MongoDb:CollectionName"];

        if (string.IsNullOrWhiteSpace(connectionString) ||
            string.IsNullOrWhiteSpace(databaseName) ||
            string.IsNullOrWhiteSpace(collectionName))
        {
            throw new InvalidOperationException(
                "MongoDb:ConnectionString, MongoDb:DatabaseName, and MongoDb:CollectionName must be configured.");
        }

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<MissionLog>(collectionName);
    }

    public async Task CreateAsync(MissionLog log, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(log, cancellationToken: cancellationToken);
    }

    public async Task<List<MissionLog>> GetByMissionIdAsync(
        int missionId,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(log => log.MissionId == missionId)
            .SortByDescending(log => log.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
