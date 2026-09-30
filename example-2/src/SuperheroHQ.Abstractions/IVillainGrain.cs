namespace SuperheroHQ.Abstractions;

/// <summary>
/// A villain that heroes can attack. The grain key is the villain name, for example "thanos".
/// </summary>
public interface IVillainGrain : IGrainWithStringKey
{
    /// <summary>Absorbs an attack and reports how much health is left.</summary>
    Task<VillainStatus> TakeHitAsync(string hero, int power);

    Task<VillainStatus> GetStatusAsync();
}
