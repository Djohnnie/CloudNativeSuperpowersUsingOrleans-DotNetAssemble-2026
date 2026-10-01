using MijnCopilot.Agents;
using MijnCopilot.Contracts.Grains;
using MijnCopilot.Contracts.Model;
using MijnCopilot.Model;
using Orleans.Placement;
using Orleans.Runtime;

namespace MijnCopilot.Orleans.Host.Grains;

[GenerateSerializer]
public class UserGrainState
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public string Email { get; set; } = string.Empty;
    [Id(2)] public DateTime LastSeenOn { get; set; }
    [Id(3)] public List<Guid> ChatIds { get; set; } = [];
}

[PreferLocalPlacement]
public class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<UserGrainState> _state;
    private readonly IServiceScopeFactory _scopeFactory;

    public UserGrain(
        [PersistentState("state", "blob-store")] IPersistentState<UserGrainState> state,
        IServiceScopeFactory scopeFactory)
    {
        _state = state;
        _scopeFactory = scopeFactory;
    }

    private UserGrainState State => _state.State;

    public Task<UserInfo> GetInfoAsync()
        => Task.FromResult(new UserInfo
        {
            UserId = this.GetPrimaryKeyString(),
            Name = State.Name,
            Email = State.Email,
            LastSeenOn = State.LastSeenOn
        });

    public async Task SetInfoAsync(string name, string email)
    {
        State.Name = name;
        State.Email = email;
        State.LastSeenOn = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task<ChatInfo> CreateChatAsync(string request)
    {
        using var scope = _scopeFactory.CreateScope();
        var copilotHelper = scope.ServiceProvider.GetRequiredService<ICopilotHelper>();
        var keywordResult = await copilotHelper.GenerateKeyword(request);

        var chatId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var chatGrain = GrainFactory.GetGrain<IChatGrain>(chatId);
        await chatGrain.InitializeAsync(this.GetPrimaryKeyString(), keywordResult.Keyword);
        await chatGrain.AddMessageAsync(
            MessageType.User,
            request,
            null,
            keywordResult.InputTokenCount + keywordResult.OutputTokenCount);

        State.ChatIds.Add(chatId);
        await _state.WriteStateAsync();

        return new ChatInfo
        {
            Id = chatId,
            UserId = this.GetPrimaryKeyString(),
            Title = keywordResult.Keyword,
            StartedOn = now,
            LastActivityOn = now
        };
    }

    public Task<IReadOnlyList<Guid>> GetChatIdsAsync()
        => Task.FromResult<IReadOnlyList<Guid>>(State.ChatIds.AsReadOnly());

    public async Task AddChatAsync(Guid chatId)
    {
        if (!State.ChatIds.Contains(chatId))
        {
            State.ChatIds.Add(chatId);
            await _state.WriteStateAsync();
        }
    }

    public async Task RemoveChatAsync(Guid chatId)
    {
        if (State.ChatIds.Remove(chatId))
            await _state.WriteStateAsync();
    }

    public Task DeactivateAsync()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }
}
