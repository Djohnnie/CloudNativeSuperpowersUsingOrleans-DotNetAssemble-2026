namespace SuperheroHQ.Abstractions;

/// <summary>
/// A single superhero. The grain key is the hero name, for example "ironman".
/// </summary>
public interface IHeroGrain : IGrainWithStringKey
{
    Task<HeroProfile> GetProfileAsync();

    /// <summary>Trains the hero, which costs energy and earns experience.</summary>
    Task<HeroProfile> TrainAsync();

    /// <summary>Attacks a villain and returns the outcome of the attack.</summary>
    Task<FightResult> FightAsync(string villain);

    /// <summary>Returns the silo this activation currently lives on.</summary>
    Task<string> WhereAmIAsync();
}
