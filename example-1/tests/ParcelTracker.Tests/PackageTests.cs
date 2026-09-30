using ParcelTracker.Abstractions;

namespace ParcelTracker.Tests;

[Collection(ClusterCollection.Name)]
public sealed class PackageTests
{
    private readonly IGrainFactory _grains;

    public PackageTests(ClusterFixture fixture) => _grains = fixture.Cluster.GrainFactory;

    [Fact]
    public async Task A_parcel_is_addressed_by_its_tracking_number()
    {
        var parcel = _grains.GetGrain<IPackageGrain>("PKG-ID-TEST");

        await parcel.ScanAsync("Antwerp depot", DeliveryStage.InTransit);

        // A brand new reference to the same tracking number is the same parcel.
        var again = _grains.GetGrain<IPackageGrain>("PKG-ID-TEST");
        var status = await again.GetStatusAsync();

        Assert.Equal("PKG-ID-TEST", status.TrackingNumber);
        Assert.Equal("Antwerp depot", status.Location);
        Assert.Equal(DeliveryStage.InTransit, status.Stage);
        Assert.Equal(1, status.Scans);
    }

    [Fact]
    public async Task Parcels_do_not_share_any_state()
    {
        await _grains.GetGrain<IPackageGrain>("PKG-ISO-1").ScanAsync("Ghent", DeliveryStage.OutForDelivery);
        await _grains.GetGrain<IPackageGrain>("PKG-ISO-2").ScanAsync("Bruges", DeliveryStage.Registered);

        var first = await _grains.GetGrain<IPackageGrain>("PKG-ISO-1").GetStatusAsync();
        var second = await _grains.GetGrain<IPackageGrain>("PKG-ISO-2").GetStatusAsync();

        Assert.Equal("Ghent", first.Location);
        Assert.Equal("Bruges", second.Location);
        Assert.Equal(DeliveryStage.OutForDelivery, first.Stage);
        Assert.Equal(DeliveryStage.Registered, second.Stage);
    }

    [Fact]
    public async Task Parallel_scans_never_lose_an_update()
    {
        var parcel = _grains.GetGrain<IPackageGrain>("PKG-RACE-TEST");

        // ScanAsync reads the counter, awaits and then writes it back, with no
        // lock anywhere. It still cannot lose an update, because a grain only
        // ever runs one call at a time.
        await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(i => parcel.ScanAsync($"scanner-{i}", DeliveryStage.InTransit)));

        var status = await parcel.GetStatusAsync();

        Assert.Equal(100, status.Scans);
    }

    [Fact]
    public async Task History_keeps_only_the_most_recent_scans()
    {
        var parcel = _grains.GetGrain<IPackageGrain>("PKG-HISTORY-TEST");

        for (var i = 1; i <= 12; i++)
        {
            await parcel.ScanAsync($"stop-{i}", DeliveryStage.InTransit);
        }

        var history = await parcel.GetHistoryAsync();

        Assert.Equal(10, history.Count);
        Assert.Equal("stop-3", history[0].Location);
        Assert.Equal("stop-12", history[^1].Location);
    }

    [Fact]
    public async Task Every_parcel_reports_the_silo_it_lives_on()
    {
        var silos = new HashSet<string>();

        for (var i = 0; i < 40; i++)
        {
            var status = await _grains.GetGrain<IPackageGrain>($"PKG-PLACE-{i:D3}").GetStatusAsync();
            Assert.False(string.IsNullOrWhiteSpace(status.Silo));
            silos.Add(status.Silo);
        }

        // Two silos are running, so 40 parcels should not all land on one of them.
        Assert.True(silos.Count > 1, $"expected parcels on more than one silo, saw: {string.Join(", ", silos)}");
    }
}
