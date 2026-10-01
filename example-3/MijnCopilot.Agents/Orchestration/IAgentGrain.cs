using MijnCopilot.Agents.Model;

namespace MijnCopilot.Agents.Orchestration;

public interface IAgentGrain : IGrainWithGuidKey
{
    Task<CopilotAgentResponse> RunAsync(AgentType agentType, CopilotChatHistory chatHistory);
}
