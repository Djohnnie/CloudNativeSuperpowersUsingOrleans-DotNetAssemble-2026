using MediatR;
using MijnCopilot.Contracts.Grains;

namespace MijnCopilot.Application.Chats.Queries;

public class GetActiveChatsQuery : IRequest<GetActiveChatsResponse>
{
    public string UserId { get; set; }
}

public class GetActiveChatsResponse
{
    public List<ChatDto> Chats { get; set; } = [];
}

public class ChatDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
}

public class GetActiveChatsQueryHandler : IRequestHandler<GetActiveChatsQuery, GetActiveChatsResponse>
{
    private readonly IGrainFactory _grainFactory;

    public GetActiveChatsQueryHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<GetActiveChatsResponse> Handle(GetActiveChatsQuery request, CancellationToken cancellationToken)
    {
        var userGrain = _grainFactory.GetGrain<IUserGrain>(request.UserId);
        var chatIds = await userGrain.GetChatIdsAsync();

        var infoTasks = chatIds
            .Select(id => _grainFactory.GetGrain<IChatGrain>(id).GetInfoAsync());

        var infos = await Task.WhenAll(infoTasks);

        var chats = infos
            .Where(i => !i.IsArchived)
            .OrderByDescending(i => i.StartedOn)
            .Select(i => new ChatDto { Id = i.Id, Title = i.Title })
            .ToList();

        return new GetActiveChatsResponse { Chats = chats };
    }
}
