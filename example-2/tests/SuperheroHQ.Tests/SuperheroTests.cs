using SuperheroHQ.Abstractions;

namespace SuperheroHQ.Tests;

[Collection(ClusterCollection.Name)]
public sealed class SuperheroTests(ClusterFixture fixture)
{
    private IGrainFactory Grains => fixture.Cluster.GrainFactory;

    [Fact]
    public async Task A_grain_is_addressable_without_being_created()
    {
        var hero = Grains.GetGrain<IHeroGrain>("ironman");

        var profile = await hero.GetProfileAsync();

        Assert.Equal("ironman", profile.Name);
        Assert.Equal(100, profile.Energy);
    }

    [Fact]
    public async Task The_same_identity_returns_the_same_state()
    {
        await Grains.GetGrain<IHeroGrain>("thor").TrainAsync();
        await Grains.GetGrain<IHeroGrain>("thor").TrainAsync();

        var profile = await Grains.GetGrain<IHeroGrain>("thor").GetProfileAsync();

        Assert.Equal(20, profile.Experience);
    }

    [Fact]
    public async Task Parallel_calls_never_lose_an_update()
    {
        var hero = Grains.GetGrain<IHeroGrain>("hulk");

        await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => hero.TrainAsync()));

        // TrainAsync reads, awaits and then writes without any lock.
        // Because a grain is single-threaded, all 100 updates survive.
        var profile = await hero.GetProfileAsync();
        Assert.Equal(1000, profile.Experience);
    }

    [Fact]
    public async Task Heroes_can_call_other_grains()
    {
        var result = await Grains.GetGrain<IHeroGrain>("spider-man").FightAsync("green-goblin");

        Assert.Equal("green-goblin", result.Villain);
        Assert.Equal(500 - result.Power, result.Villains.Health);
    }

    [Fact]
    public async Task A_city_coordinates_a_whole_roster()
    {
        var city = Grains.GetGrain<ICityGrain>("new-york");
        await city.AssembleAsync("captain", "falcon", "vision");

        var report = await city.LaunchMissionAsync("ultron");

        Assert.Equal(3, report.Attacks.Count);
        Assert.Equal(3, (await city.GetRosterAsync()).Count);
    }

    [Fact]
    public async Task Grains_are_spread_over_the_silos_of_the_cluster()
    {
        var silos = await Task.WhenAll(Enumerable.Range(0, 60)
            .Select(i => Grains.GetGrain<IHeroGrain>($"recruit-{i:D3}").WhereAmIAsync()));

        Assert.True(silos.Distinct().Count() > 1, "expected activations on more than one silo");
    }
}
