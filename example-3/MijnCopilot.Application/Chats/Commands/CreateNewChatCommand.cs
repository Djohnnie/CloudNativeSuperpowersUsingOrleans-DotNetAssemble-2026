using MediatR;
using MijnCopilot.Contracts.Grains;

namespace MijnCopilot.Application.Chats.Commands;

public class CreateNewChatCommand : MediatR.IRequest<CreateNewChatResponse>
{
    public string UserId { get; set; }
    public string Request { get; set; }
}

public class CreateNewChatResponse
{
    public Guid ChatId { get; set; }
    public string Title { get; set; }
}

public class CreateNewChatCommandHandler : IRequestHandler<CreateNewChatCommand, CreateNewChatResponse>
{
    private readonly IGrainFactory _grainFactory;

    public CreateNewChatCommandHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<CreateNewChatResponse> Handle(CreateNewChatCommand request, CancellationToken cancellationToken)
    {
        var userGrain = _grainFactory.GetGrain<IUserGrain>(request.UserId);
        var chatInfo = await userGrain.CreateChatAsync(request.Request);
        return new CreateNewChatResponse { ChatId = chatInfo.Id, Title = chatInfo.Title };
    }
}
