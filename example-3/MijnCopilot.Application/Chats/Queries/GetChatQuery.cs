using MediatR;
using MijnCopilot.Contracts.Grains;
using MijnCopilot.Contracts.Model;
using MijnCopilot.Model;

namespace MijnCopilot.Application.Chats.Queries;

public class GetChatQuery : IRequest<GetChatResponse>
{
    public Guid ChatId { get; set; }
}

public class GetChatResponse
{
    public Guid ChatId { get; set; }
    public string Title { get; set; }
    public DateTime StartedOn { get; set; }
    public List<MessageDto> Messages { get; set; } = [];
}

public class MessageDto
{
    public string Content { get; set; }
    public string AgentName { get; set; }
    public ChatRole Role { get; set; }
    public int TokensUsed { get; set; }
    public DateTime PostedOn { get; set; }
}

public enum ChatRole
{
    User,
    Assistant,
    Reduced,
    DebugQuestion,
    DebugAnswer
}

public class GetChatQueryHandler : IRequestHandler<GetChatQuery, GetChatResponse>
{
    private readonly IGrainFactory _grainFactory;

    public GetChatQueryHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<GetChatResponse> Handle(GetChatQuery request, CancellationToken cancellationToken)
    {
        var chatGrain = _grainFactory.GetGrain<IChatGrain>(request.ChatId);

        var info = await chatGrain.GetInfoAsync();
        var history = await chatGrain.GetHistoryAsync();

        return new GetChatResponse
        {
            ChatId = info.Id,
            Title = info.Title,
            StartedOn = info.StartedOn,
            Messages = history
                .Select(m => new MessageDto
                {
                    Content = m.Content,
                    AgentName = m.AgentName ?? string.Empty,
                    Role = MapChatRole(m.Type),
                    TokensUsed = m.TokensUsed,
                    PostedOn = m.PostedOn
                })
                .ToList()
        };
    }

    private static ChatRole MapChatRole(MessageType messageType) => messageType switch
    {
        MessageType.Assistant => ChatRole.Assistant,
        MessageType.User => ChatRole.User,
        MessageType.Reduced => ChatRole.Reduced,
        MessageType.DebugQuestion => ChatRole.DebugQuestion,
        MessageType.DebugAnswer => ChatRole.DebugAnswer,
        _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
    };
}
