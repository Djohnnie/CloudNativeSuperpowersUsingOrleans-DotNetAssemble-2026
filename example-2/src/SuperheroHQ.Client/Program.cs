using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using SuperheroHQ.Abstractions;

var host = Host.CreateApplicationBuilder(args);
host.Logging.SetMinimumLevel(LogLevel.Warning);

// Under Aspire the cluster configuration arrives as configuration, so the client
// only has to connect the Redis multiplexer it was given and call UseOrleansClient.
if (host.Configuration.GetSection("Orleans:Clustering").Exists())
{
    host.AddServiceDefaults();
    host.AddKeyedRedisClient("redis");
    host.UseOrleansClient();
}
else
{
    host.UseOrleansClient(client => client.UseLocalhostClustering(gatewayPort: 30001));
}

using var app = host.Build();

AnsiConsole.Write(new FigletText("Superhero HQ").Centered().Color(Color.Orange1));
AnsiConsole.Write(new Markup("[grey]Cloud-native superpowers with Microsoft Orleans[/]").Centered());
AnsiConsole.WriteLine();

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Line)
    .SpinnerStyle(Style.Parse("orange1"))
    .StartAsync("Connecting to the Orleans cluster...", async _ => await app.StartAsync());

var grains = app.Services.GetRequiredService<IClusterClient>();

Title("1. A grain is virtual: you never create it");
var ironman = grains.GetGrain<IHeroGrain>("ironman");
Print(await ironman.GetProfileAsync());
Note("The call above activated the grain. We only ever used its name.");

Title("2. Same identity means the same grain, with the same state");
await ironman.TrainAsync();
await ironman.TrainAsync();
Print(await grains.GetGrain<IHeroGrain>("ironman").GetProfileAsync());
Note("A brand new grain reference points at the same activation and state.");

Title("3. Single-threaded: 100 parallel calls, no locks, no lost updates");
var hulk = grains.GetGrain<IHeroGrain>("hulk");
var before = (await hulk.GetProfileAsync()).Experience;

await AnsiConsole.Progress()
    .Columns(
        new TaskDescriptionColumn(),
        new AsciiBarColumn(),
        new PercentageColumn(),
        new SpinnerColumn(Spinner.Known.Line))
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("[orange1]Training the Hulk in parallel[/]", maxValue: 100);
        await Task.WhenAll(Enumerable.Range(0, 100).Select(async _ =>
        {
            await hulk.TrainAsync();
            task.Increment(1);
        }));
    });

var after = (await hulk.GetProfileAsync()).Experience;
var expected = before + 1000;

var grid = new Grid().AddColumn().AddColumn();
grid.AddRow("[grey]experience before[/]", $"[bold]{before}[/]");
grid.AddRow("[grey]experience after[/]", $"[bold]{after}[/]");
grid.AddRow("[grey]expected[/]", $"[bold]{expected}[/]");
AnsiConsole.Write(new Padder(grid, new Padding(3, 0, 0, 0)));

Indent(after == expected
    ? "[green]<OK>  Exactly right, even though TrainAsync reads, awaits and then writes.[/]"
    : "[red]<!!> Unexpected result![/]");

Title("4. Grains talk to grains, wherever they live");
var city = grains.GetGrain<ICityGrain>("new-york");
await city.AssembleAsync("ironman", "hulk", "thor", "black-widow", "spider-man");

MissionReport report = null!;
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Line)
    .SpinnerStyle(Style.Parse("red"))
    .StartAsync("New York is assembling against [bold red]thanos[/]...",
        async _ => report = await city.LaunchMissionAsync("thanos"));

var attacks = new Table()
    .Border(TableBorder.Ascii)
    .BorderColor(Color.Orange1)
    .Title("[bold]Mission report: new-york vs thanos[/]")
    .AddColumn("[bold]Hero[/]")
    .AddColumn("[bold]Villain[/]")
    .AddColumn(new TableColumn("[bold]Power[/]").RightAligned())
    .AddColumn(new TableColumn("[bold]Health left[/]").RightAligned());

foreach (var attack in report.Attacks)
{
    attacks.AddRow(
        $"[cyan]{attack.Hero.EscapeMarkup()}[/]",
        $"[red]{attack.Villain.EscapeMarkup()}[/]",
        $"[yellow]{attack.Power}[/]",
        attack.Villains.Health > 0 ? $"[green]{attack.Villains.Health}[/]" : "[bold red]0[/]");
}

AnsiConsole.Write(attacks);
Indent(report.Won
    ? "[bold green]*** Villain defeated! ***[/]"
    : "[bold red]!!! The villain is still standing. !!![/]");

Title("5. Scalability: the cluster places grains for you");
var recruits = Enumerable.Range(1, 60).Select(i => $"recruit-{i:D3}").ToArray();
string[] placements = [];

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Line)
    .SpinnerStyle(Style.Parse("orange1"))
    .StartAsync($"Activating {recruits.Length} new heroes across the cluster...", async _ =>
        placements = await Task.WhenAll(recruits
            .Select(async name => await grains.GetGrain<IHeroGrain>(name).WhereAmIAsync())));

// A hand-rolled '#' bar chart: Spectre's BarChart draws with block characters that the
// Aspire dashboard log viewer cannot render.
var colors = new[] { "orange1", "aqua", "green", "fuchsia", "yellow", "red" };
var chart = new Table()
    .Border(TableBorder.Ascii)
    .BorderColor(Color.Orange1)
    .Title($"[bold]Where the {recruits.Length} recruits live[/]")
    .AddColumn("[bold]Silo[/]")
    .AddColumn("[bold]Grains[/]")
    .AddColumn(new TableColumn("[bold]#[/]").RightAligned());

var index = 0;
foreach (var silo in placements.GroupBy(s => s).OrderBy(g => g.Key))
{
    var count = silo.Count();
    var color = colors[index++ % colors.Length];
    chart.AddRow(
        $"[bold]{silo.Key.EscapeMarkup()}[/]",
        $"[{color}]{new string('#', Math.Max(1, count * 40 / recruits.Length))}[/]",
        $"[{color}]{count}[/]");
}

AnsiConsole.Write(chart);
Note("Start another silo and run this again: the spread changes, your code does not.");

AnsiConsole.WriteLine();
AnsiConsole.Write(new Panel(
        "[bold]Done![/]\n[grey]Watch the cluster on the Orleans dashboard of any silo (/dashboard).[/]")
    .Border(BoxBorder.Ascii)
    .BorderColor(Color.Orange1)
    .Padding(2, 1));

await app.StopAsync();

// Spectre's Rule draws a box-drawing line, so the steps are separated with plain '=' instead.
static void Title(string text)
{
    AnsiConsole.WriteLine();
    var width = Math.Clamp(AnsiConsole.Profile.Width, 40, 100);
    var padding = Math.Max(3, width - text.Length - 5);
    AnsiConsole.MarkupLine($"[bold orange1]== {text.EscapeMarkup()} {new string('=', padding)}[/]");
}

static void Note(string text) => Indent($"[grey]{text}[/]");

static void Indent(string markup) =>
    AnsiConsole.Write(new Padder(new Markup(markup), new Padding(3, 0, 0, 0)));

static void Print(HeroProfile hero)
{
    var table = new Table()
        .Border(TableBorder.Ascii)
        .BorderColor(Color.Orange1)
        .AddColumn("[bold]Hero[/]")
        .AddColumn(new TableColumn("[bold]Experience[/]").RightAligned())
        .AddColumn(new TableColumn("[bold]Energy[/]").RightAligned())
        .AddColumn(new TableColumn("[bold]Missions[/]").RightAligned())
        .AddColumn("[bold]Living on[/]");

    table.AddRow(
        $"[bold cyan]{hero.Name.EscapeMarkup()}[/]",
        $"[yellow]{hero.Experience}[/]",
        $"[green]{hero.Energy}[/]",
        $"{hero.Missions}",
        $"[grey]{hero.Silo.EscapeMarkup()}[/]");

    AnsiConsole.Write(table);
}

// Spectre's ProgressBarColumn uses block characters, which the Aspire dashboard log
// viewer cannot render, so the bar is drawn with plain '#' and '-' instead.
file sealed class AsciiBarColumn : ProgressColumn
{
    private const int Width = 40;

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var completed = (int)Math.Round(Width * Math.Clamp(task.Value / task.MaxValue, 0, 1));
        return new Markup(
            $"[[[orange1]{new string('#', completed)}[/][grey]{new string('-', Width - completed)}[/]]]");
    }
}
