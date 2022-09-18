namespace Flownodes.Cluster.Node.Configurations;

public record RedisConfiguration(string ConnectionString, int DatabaseNumber = 0);