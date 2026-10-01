namespace MijnCopilot.Agents.Model;

[GenerateSerializer]
public class CopilotAgentResponse
{
    [Id(0)] public string Response { get; set; } = string.Empty;
    [Id(1)] public string AgentName { get; set; } = string.Empty;
    [Id(2)] public int InputTokenCount { get; set; } = 0;
    [Id(3)] public int OutputTokenCount { get; set; } = 0;
}