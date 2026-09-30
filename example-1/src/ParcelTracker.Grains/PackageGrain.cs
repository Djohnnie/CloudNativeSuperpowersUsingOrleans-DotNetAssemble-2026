using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Runtime;
using ParcelTracker.Abstractions;

namespace ParcelTracker.Grains;

/// <summary>Everything one parcel remembers. Small on purpose.</summary>
[GenerateSerializer]
public sealed class PackageState
{
    [Id(0)] public string Location { get; set; } = "not scanned yet";
    [Id(1)] public DeliveryStage Stage { get; set; } = DeliveryStage.Registered;
    [Id(2)] public int Scans { get; set; }
    [Id(3)] public DateTimeOffset? LastScanAt { get; set; }
    [Id(4)] public List<ScanEvent> History { get; set; } = [];
}

/// <summary>
/// One activation per tracking number, anywhere in the cluster.
///
/// This class is the entire domain model of example 1. Identity comes from the
/// grain key, behaviour from the methods below and state from
/// <see cref="IPersistentState{T}"/>. Nothing else is needed to have millions of
/// independent, addressable parcels spread over a cluster of silos.
/// </summary>
// Orleans' default placement prefers the silo that made the call when the cluster
// is evenly loaded. That is a sensible production default - a local call is far
// cheaper than a network hop - but on stage it makes one silo look like it does
// all the work. Asking for plain random placement keeps the demo honest about
// "parcels live all over the cluster".
[RandomPlacement]
public sealed class PackageGrain : Grain, IPackageGrain
{
    /// <summary>The newest scans are kept; older ones are dropped.</summary>
    private const int HistoryLength = 10;

    private readonly IPersistentState<PackageState> _state;
    private readonly ILocalSiloDetails _silo;
    private readonly ILogger<PackageGrain> _logger;

    public PackageGrain(
        [PersistentState("package")] IPersistentState<PackageState> state,
        ILocalSiloDetails silo,
        ILogger<PackageGrain> logger)
    {
        _state = state;
        _silo = silo;
        _logger = logger;
    }

    private string TrackingNumber => this.GetPrimaryKeyString();

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Watch these lines in the Aspire logs: the first call to a parcel is
        // what brings it to life, and Orleans decides on which silo that happens.
        _logger.LogInformation("Parcel {TrackingNumber} activated on {Silo}", TrackingNumber, _silo.Name);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parcel {TrackingNumber} deactivated ({Reason})", TrackingNumber, reason.ReasonCode);
        return Task.CompletedTask;
    }

    public async Task<PackageStatus> ScanAsync(string location, DeliveryStage stage)
    {
        // This reads the counter, awaits, and only then writes it back. In any
        // other multi-threaded program that is a lost-update bug waiting to
        // happen. Here it is correct without a single lock, because Orleans never
        // runs two calls on the same grain at the same time.
        var scans = _state.State.Scans;

        // Stand-in for the slow thing a real scanner would do.
        await Task.Delay(5);

        _state.State.Scans = scans + 1;
        _state.State.Location = location;
        _state.State.Stage = stage;
        _state.State.LastScanAt = DateTimeOffset.UtcNow;

        _state.State.History.Add(new ScanEvent(_state.State.LastScanAt.Value, location, stage));
        if (_state.State.History.Count > HistoryLength)
        {
            _state.State.History.RemoveRange(0, _state.State.History.Count - HistoryLength);
        }

        await _state.WriteStateAsync();

        return Status();
    }

    public Task<PackageStatus> GetStatusAsync() => Task.FromResult(Status());

    public Task<IReadOnlyList<ScanEvent>> GetHistoryAsync() =>
        Task.FromResult<IReadOnlyList<ScanEvent>>(_state.State.History.ToArray());

    private PackageStatus Status() => new(
        TrackingNumber,
        _state.State.Location,
        _state.State.Stage,
        _state.State.Scans,
        _state.State.LastScanAt,
        _silo.Name);
}
