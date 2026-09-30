namespace ParcelTracker.Abstractions;

/// <summary>
/// The one and only grain type in this example.
///
/// There is exactly one grain instance per tracking number, and the tracking
/// number *is* the identity. You never new one up, never look one up and never
/// dispose one: you just ask for "PKG-000001" and start calling it.
/// </summary>
public interface IPackageGrain : IGrainWithStringKey
{
    /// <summary>Records a scan: the parcel was seen at a location, in a stage.</summary>
    Task<PackageStatus> ScanAsync(string location, DeliveryStage stage);

    /// <summary>Returns where the parcel is right now.</summary>
    Task<PackageStatus> GetStatusAsync();

    /// <summary>Returns the most recent scans, newest last.</summary>
    Task<IReadOnlyList<ScanEvent>> GetHistoryAsync();
}
