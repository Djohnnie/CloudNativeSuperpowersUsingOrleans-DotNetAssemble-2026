using MijnCopilot.Contracts.Model;
using MijnCopilot.Model;

namespace MijnCopilot.Contracts.Grains;

public interface IChatGrain : IGrainWithGuidKey
{
    Task<ChatInfo> GetInfoAsync();
    Task InitializeAsync(string userId, string title);
    Task<IReadOnlyList<ChatMessageInfo>> GetHistoryAsync();
    Task AddMessageAsync(MessageType type, string content, string? agentName = null, int tokensUsed = 0);
    Task<string> ChatAsync(string request, bool ignoreRequest);
    Task ArchiveAsync();
}