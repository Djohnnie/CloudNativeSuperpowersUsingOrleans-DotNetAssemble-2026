using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using SuperheroHQ.Abstractions;

namespace SuperheroHQ.Grains;

[GenerateSerializer]
public sealed class VillainState
{
    [Id(0)] public int Health { get; set; } = 500;
}

public sealed class VillainGrain : Grain, IVillainGrain
{
    private readonly IPersistentState<VillainState> _state;
    private readonly ILocalSiloDetails _silo;
    private readonly ILogger<VillainGrain> _logger;

    public VillainGrain(
        [PersistentState("villain", "villains")] IPersistentState<VillainState> state,
        ILocalSiloDetails silo,
        ILogger<VillainGrain> logger)
    {
        _state = state;
        _silo = silo;
        _logger = logger;
    }

    private string Name => this.GetPrimaryKeyString();

    public async Task<VillainStatus> TakeHitAsync(string hero, int power)
    {
        _state.State.Health = Math.Max(0, _state.State.Health - power);
        await _state.WriteStateAsync();

        _logger.LogInformation("{Hero} hit {Villain} for {Power}, {Health} health left",
            hero, Name, power, _state.State.Health);

        return Status();
    }

    public Task<VillainStatus> GetStatusAsync() => Task.FromResult(Status());

    private VillainStatus Status() =>
        new(Name, _state.State.Health, _state.State.Health == 0, _silo.Name);
}
