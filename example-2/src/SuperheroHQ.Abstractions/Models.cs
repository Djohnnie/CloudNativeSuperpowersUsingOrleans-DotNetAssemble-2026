namespace SuperheroHQ.Abstractions;

[GenerateSerializer, Immutable]
public sealed record HeroProfile(
    [property: Id(0)] string Name,
    [property: Id(1)] int Experience,
    [property: Id(2)] int Energy,
    [property: Id(3)] int Missions,
    [property: Id(4)] string Silo);

[GenerateSerializer, Immutable]
public sealed record VillainStatus(
    [property: Id(0)] string Name,
    [property: Id(1)] int Health,
    [property: Id(2)] bool Defeated,
    [property: Id(3)] string Silo);

[GenerateSerializer, Immutable]
public sealed record FightResult(
    [property: Id(0)] string Hero,
    [property: Id(1)] string Villain,
    [property: Id(2)] int Power,
    [property: Id(3)] VillainStatus Villains);

[GenerateSerializer, Immutable]
public sealed record MissionReport(
    [property: Id(0)] string City,
    [property: Id(1)] string Villain,
    [property: Id(2)] IReadOnlyList<FightResult> Attacks,
    [property: Id(3)] bool Won);
