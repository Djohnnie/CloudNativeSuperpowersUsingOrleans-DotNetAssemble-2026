using MediatR;
using MijnCopilot.Contracts.Grains;

namespace MijnCopilot.Application.Chats.Commands;

public class DeleteChatCommand : IRequest<Unit>
{
    public Guid ChatId { get; set; }
}

public class DeleteChatCommandHandler : IRequestHandler<DeleteChatCommand, Unit>
{
    private readonly IGrainFactory _grainFactory;

    public DeleteChatCommandHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task<Unit> Handle(DeleteChatCommand request, CancellationToken cancellationToken)
    {
        var chatGrain = _grainFactory.GetGrain<IChatGrain>(request.ChatId);
        var info = await chatGrain.GetInfoAsync();

        await chatGrain.ArchiveAsync();

        if (!string.IsNullOrEmpty(info.UserId))
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(info.UserId);
            await userGrain.RemoveChatAsync(request.ChatId);
        }

        return Unit.Value;
    }
}
