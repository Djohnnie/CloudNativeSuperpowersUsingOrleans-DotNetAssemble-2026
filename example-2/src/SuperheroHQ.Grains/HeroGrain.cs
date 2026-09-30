using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using SuperheroHQ.Abstractions;

namespace SuperheroHQ.Grains;

[GenerateSerializer]
public sealed class HeroState
{
    [Id(0)] public int Experience { get; set; }
    [Id(1)] public int Energy { get; set; } = 100;
    [Id(2)] public int Missions { get; set; }
}

public sealed class HeroGrain : Grain, IHeroGrain
{
    private readonly IPersistentState<HeroState> _state;
    private readonly ILocalSiloDetails _silo;
    private readonly ILogger<HeroGrain> _logger;

    public HeroGrain(
        [PersistentState("hero", "heroes")] IPersistentState<HeroState> state,
        ILocalSiloDetails silo,
        ILogger<HeroGrain> logger)
    {
        _state = state;
        _silo = silo;
        _logger = logger;
    }

    private string Name => this.GetPrimaryKeyString();

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hero {Hero} activated on silo {Silo}", Name, _silo.Name);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hero {Hero} deactivated ({Reason})", Name, reason.ReasonCode);
        return Task.CompletedTask;
    }

    public Task<HeroProfile> GetProfileAsync() => Task.FromResult(Profile());

    public async Task<HeroProfile> TrainAsync()
    {
        // This looks like a data race: read, await, then write.
        // It is completely safe, because Orleans never runs two calls
        // on this grain at the same time. No lock is needed.
        var experience = _state.State.Experience;
        var energy = _state.State.Energy;

        await Task.Delay(5);

        _state.State.Experience = experience + 10;
        _state.State.Energy = Math.Max(0, energy - 1);

        await _state.WriteStateAsync();
        return Profile();
    }

    public async Task<FightResult> FightAsync(string villain)
    {
        var power = 10 + _state.State.Experience / 10;

        // Grain-to-grain call: no addresses, no HTTP, just another grain by name.
        var status = await GrainFactory.GetGrain<IVillainGrain>(villain).TakeHitAsync(Name, power);

        _state.State.Missions++;
        _state.State.Energy = Math.Max(0, _state.State.Energy - 5);
        _state.State.Experience += status.Defeated ? 50 : 5;
        await _state.WriteStateAsync();

        return new FightResult(Name, villain, power, status);
    }

    public Task<string> WhereAmIAsync() => Task.FromResult(_silo.Name);

    private HeroProfile Profile() => new(
        Name,
        _state.State.Experience,
        _state.State.Energy,
        _state.State.Missions,
        _silo.Name);
}
