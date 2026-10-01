using MediatR;
using MijnCopilot.Contracts.Grains;

namespace MijnCopilot.Application.User.Commands;

public class UserLogoutCommand : MediatR.IRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class UserLogoutCommandHandler : IRequestHandler<UserLogoutCommand>
{
    private readonly IGrainFactory _grainFactory;

    public UserLogoutCommandHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task Handle(UserLogoutCommand request, CancellationToken cancellationToken)
    {
        var userGrain = _grainFactory.GetGrain<IUserGrain>(request.UserId);
        await userGrain.DeactivateAsync();
    }
}