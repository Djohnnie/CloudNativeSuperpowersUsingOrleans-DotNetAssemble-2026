using Orleans.TestingHost;

namespace ParcelTracker.Tests;

/// <summary>
/// A small in-process cluster of two silos, so the behaviour the demo shows on
/// stage can be verified without starting the silo host or Aspire.
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
        public void Configure(ISiloBuilder silo) => silo.AddMemoryGrainStorageAsDefault();
    }
}

[CollectionDefinition(Name)]
public sealed class ClusterCollection : ICollectionFixture<ClusterFixture>
{
    public const string Name = "cluster";
}
