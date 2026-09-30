using Orleans.Runtime;
using SuperheroHQ.Abstractions;

namespace SuperheroHQ.Grains;

[GenerateSerializer]
public sealed class CityState
{
    [Id(0)] public List<string> Roster { get; set; } = [];
}

public sealed class CityGrain : Grain, ICityGrain
{
    private readonly IPersistentState<CityState> _state;

    public CityGrain([PersistentState("city", "cities")] IPersistentState<CityState> state)
        => _state = state;

    public async Task<IReadOnlyList<string>> AssembleAsync(params string[] heroes)
    {
        foreach (var hero in heroes)
        {
            if (!_state.State.Roster.Contains(hero))
            {
                _state.State.Roster.Add(hero);
            }
        }

        await _state.WriteStateAsync();
        return _state.State.Roster;
    }

    public Task<IReadOnlyList<string>> GetRosterAsync()
        => Task.FromResult<IReadOnlyList<string>>(_state.State.Roster);

    public async Task<MissionReport> LaunchMissionAsync(string villain)
    {
        // Every hero attacks in parallel. The villain grain still handles
        // the hits one by one, so its health can never be corrupted.
        var attacks = await Task.WhenAll(_state.State.Roster
            .Select(hero => GrainFactory.GetGrain<IHeroGrain>(hero).FightAsync(villain)));

        var status = await GrainFactory.GetGrain<IVillainGrain>(villain).GetStatusAsync();
        return new MissionReport(this.GetPrimaryKeyString(), villain, attacks, status.Defeated);
    }
}
