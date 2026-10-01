namespace MijnCopilot.Agents.Model;

[GenerateSerializer]
public class DebugChat
{
    [Id(0)] public bool IsQuestion { get; set; }
    [Id(1)] public string AgentName { get; set; } = string.Empty;
    [Id(2)] public string Content { get; set; } = string.Empty;
}