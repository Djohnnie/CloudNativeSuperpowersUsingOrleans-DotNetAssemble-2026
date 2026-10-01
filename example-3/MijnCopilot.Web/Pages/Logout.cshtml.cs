using Auth0.AspNetCore.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MijnCopilot.Application.User.Commands;
using System.Security.Claims;

namespace MijnCopilot.Web.Pages
{
    [Authorize]
    public class LogoutModel : PageModel
    {
        private readonly IMediator _mediator;

        public LogoutModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task OnGet()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await _mediator.Send(new UserLogoutCommand { UserId = userId });
            }

            var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
                .WithRedirectUri("/")
                .Build();

            await HttpContext.SignOutAsync(
                Auth0Constants.AuthenticationScheme,
                authenticationProperties);

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
