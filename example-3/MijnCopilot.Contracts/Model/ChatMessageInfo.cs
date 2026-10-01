using MijnCopilot.Model;

namespace MijnCopilot.Contracts.Model;

[GenerateSerializer]
public class ChatMessageInfo
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public MessageType Type { get; set; }
    [Id(2)] public string Content { get; set; } = string.Empty;
    [Id(3)] public string? AgentName { get; set; }
    [Id(4)] public bool IsReduced { get; set; }
    [Id(5)] public int TokensUsed { get; set; }
    [Id(6)] public DateTime PostedOn { get; set; }
}
