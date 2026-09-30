using Orleans.TestingHost;

namespace SuperheroHQ.Tests;

/// <summary>
/// Spins up a small in-process cluster of two silos, so the demo can be verified
/// without starting the silo host or the client.
/// </summary>
public sealed class ClusterFixture : IAsyncLifetime
{
    public TestCluster Cluster { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        var builder = new TestClusterBuilder(initialSilosCount: 2);
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    public async ValueTask DisposeAsync() => await Cluster.StopAllSilosAsync();

    private sealed class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder silo)
        {
            silo.AddMemoryGrainStorageAsDefault();
            silo.AddMemoryGrainStorage("heroes");
            silo.AddMemoryGrainStorage("villains");
            silo.AddMemoryGrainStorage("cities");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = "cluster";
}
