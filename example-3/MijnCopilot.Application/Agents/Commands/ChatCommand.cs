using MediatR;
using MijnCopilot.Contracts.Grains;

namespace MijnCopilot.Application.Agents.Commands;

public class ChatCommand : IRequest<ChatResponse>
{
    public Guid ChatId { get; set; }
    public string Request { get; set; }
    public bool IgnoreRequest { get; set; }
}

public class ChatResponse
{
    public string Response { get; set; }
}

public class ChatCommandHandler : IRequestHandler<ChatCommand, ChatResponse>
{
    private readonly IGrainFactory _grainFactory;

    public ChatCommandHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<ChatResponse> Handle(ChatCommand request, CancellationToken cancellationToken)
    {
        var chatGrain = _grainFactory.GetGrain<IChatGrain>(request.ChatId);
        var response = await chatGrain.ChatAsync(request.Request, request.IgnoreRequest);
        return new ChatResponse { Response = response };
    }
}