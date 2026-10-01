namespace MijnCopilot.Agents.Model;

[GenerateSerializer]
public class CopilotChat
{
    [Id(0)] public CopilotChatRole Role { get; set; }
    [Id(1)] public string Content { get; set; } = string.Empty;
}