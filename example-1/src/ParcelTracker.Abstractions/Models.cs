namespace ParcelTracker.Abstractions;

/// <summary>Where a parcel is in its delivery journey.</summary>
[GenerateSerializer]
public enum DeliveryStage
{
    Registered,
    InTransit,
    OutForDelivery,
    Delivered
}

/// <summary>A single scan of a parcel by a depot, a van or a courier.</summary>
[GenerateSerializer, Immutable]
public sealed record ScanEvent(
    [property: Id(0)] DateTimeOffset At,
    [property: Id(1)] string Location,
    [property: Id(2)] DeliveryStage Stage);

/// <summary>
/// The answer to "where is my parcel?". <see cref="Silo"/> is not part of a real
/// tracking system: it is here so the demo can show which silo the grain happens
/// to live on.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record PackageStatus(
    [property: Id(0)] string TrackingNumber,
    [property: Id(1)] string Location,
    [property: Id(2)] DeliveryStage Stage,
    [property: Id(3)] int Scans,
    [property: Id(4)] DateTimeOffset? LastScanAt,
    [property: Id(5)] string Silo);
