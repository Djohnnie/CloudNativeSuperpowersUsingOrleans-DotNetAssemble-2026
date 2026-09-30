namespace SuperheroHQ.Abstractions;

/// <summary>
/// A city that coordinates heroes. The grain key is the city name, for example "new-york".
/// </summary>
public interface ICityGrain : IGrainWithStringKey
{
    /// <summary>Adds heroes to the city roster.</summary>
    Task<IReadOnlyList<string>> AssembleAsync(params string[] heroes);

    Task<IReadOnlyList<string>> GetRosterAsync();

    /// <summary>Sends every hero on the roster after a villain, one attack each.</summary>
    Task<MissionReport> LaunchMissionAsync(string villain);
}
