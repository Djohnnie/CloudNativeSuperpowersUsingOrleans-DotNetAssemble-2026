using MijnCopilot.Agents;
using MijnCopilot.Agents.Model;
using MijnCopilot.Contracts.Grains;
using MijnCopilot.Contracts.Model;
using MijnCopilot.Model;
using Orleans.Placement;
using Orleans.Runtime;

namespace MijnCopilot.Orleans.Host.Grains;

[GenerateSerializer]
public class ChatGrainState
{
    [Id(0)] public string UserId { get; set; } = string.Empty;
    [Id(1)] public string Title { get; set; } = string.Empty;
    [Id(2)] public bool IsArchived { get; set; }
    [Id(3)] public DateTime StartedOn { get; set; }
    [Id(4)] public DateTime LastActivityOn { get; set; }
    [Id(5)] public List<ChatMessageInfo> History { get; set; } = [];
}

[PreferLocalPlacement]
public class ChatGrain : Grain, IChatGrain
{
    private readonly IPersistentState<ChatGrainState> _state;
    private readonly IServiceScopeFactory _scopeFactory;

    public ChatGrain(
        [PersistentState("state", "blob-store")] IPersistentState<ChatGrainState> state,
        IServiceScopeFactory scopeFactory)
    {
        _state = state;
        _scopeFactory = scopeFactory;
    }

    private ChatGrainState State => _state.State;

    public Task<ChatInfo> GetInfoAsync()
        => Task.FromResult(new ChatInfo
        {
            Id = this.GetPrimaryKey(),
            UserId = State.UserId,
            Title = State.Title,
            IsArchived = State.IsArchived,
            StartedOn = State.StartedOn,
            LastActivityOn = State.LastActivityOn
        });

    public async Task InitializeAsync(string userId, string title)
    {
        if (State.StartedOn != default)
            return;

        State.UserId = userId;
        State.Title = title;
        State.StartedOn = DateTime.UtcNow;
        State.LastActivityOn = State.StartedOn;
        await _state.WriteStateAsync();
    }

    public Task<IReadOnlyList<ChatMessageInfo>> GetHistoryAsync()
        => Task.FromResult<IReadOnlyList<ChatMessageInfo>>(State.History.AsReadOnly());

    public async Task AddMessageAsync(MessageType type, string content, string? agentName = null, int tokensUsed = 0)
    {
        State.History.Add(new ChatMessageInfo
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            AgentName = agentName,
            TokensUsed = tokensUsed,
            PostedOn = DateTime.UtcNow
        });
        State.LastActivityOn = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task<string> ChatAsync(string request, bool ignoreRequest)
    {
        var chatHistory = new CopilotChatHistory();
        foreach (var message in State.History)
        {
            switch (message.Type)
            {
                case MessageType.User:
                    chatHistory.AddUserMessage(message.Content);
                    break;
                case MessageType.Assistant:
                    chatHistory.AddAssistantMessage(message.Content);
                    break;
            }
        }

        if (!ignoreRequest)
            chatHistory.AddUserMessage(request);

        using var scope = _scopeFactory.CreateScope();
        var copilotHelper = scope.ServiceProvider.GetRequiredService<ICopilotHelper>();
        var copilotResponse = await copilotHelper.Chat(chatHistory);

        if (!ignoreRequest)
            State.History.Add(new ChatMessageInfo
            {
                Id = Guid.NewGuid(),
                Type = MessageType.User,
                Content = request,
                AgentName = string.Empty,
                TokensUsed = copilotResponse.InputTokenCount,
                PostedOn = DateTime.UtcNow
            });

        foreach (var debug in copilotResponse.Debug)
            State.History.Add(new ChatMessageInfo
            {
                Id = Guid.NewGuid(),
                Type = debug.IsQuestion ? MessageType.DebugQuestion : MessageType.DebugAnswer,
                Content = debug.Content,
                AgentName = debug.AgentName,
                TokensUsed = 0,
                PostedOn = DateTime.UtcNow
            });

        State.History.Add(new ChatMessageInfo
        {
            Id = Guid.NewGuid(),
            Type = MessageType.Assistant,
            Content = copilotResponse.LastAssistantMessage,
            AgentName = copilotResponse.AgentName,
            TokensUsed = copilotResponse.OutputTokenCount,
            PostedOn = DateTime.UtcNow
        });

        State.LastActivityOn = DateTime.UtcNow;
        await _state.WriteStateAsync();

        return copilotResponse.LastAssistantMessage;
    }

    public async Task ArchiveAsync()
    {
        State.IsArchived = true;
        await _state.WriteStateAsync();
    }
}
