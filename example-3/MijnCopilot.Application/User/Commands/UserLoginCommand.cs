using MediatR;
using MijnCopilot.Contracts.Grains;

namespace MijnCopilot.Application.User.Commands;

public class UserLoginCommand : MediatR.IRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserLoginCommandHandler : IRequestHandler<UserLoginCommand>
{
    private readonly IGrainFactory _grainFactory;

    public UserLoginCommandHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task Handle(UserLoginCommand request, CancellationToken cancellationToken)
    {
        var userGrain = _grainFactory.GetGrain<IUserGrain>(request.UserId);
        await userGrain.SetInfoAsync(request.Name, request.Email);
    }
}