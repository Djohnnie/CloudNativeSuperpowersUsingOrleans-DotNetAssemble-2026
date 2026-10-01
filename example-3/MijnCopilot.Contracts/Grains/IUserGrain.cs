using MijnCopilot.Contracts.Model;
using MijnCopilot.Model;

namespace MijnCopilot.Contracts.Grains;

public interface IUserGrain : IGrainWithStringKey
{
    Task<UserInfo> GetInfoAsync();
    Task SetInfoAsync(string name, string email);
    Task<ChatInfo> CreateChatAsync(string request);
    Task<IReadOnlyList<Guid>> GetChatIdsAsync();
    Task AddChatAsync(Guid chatId);
    Task RemoveChatAsync(Guid chatId);
    Task DeactivateAsync();
}