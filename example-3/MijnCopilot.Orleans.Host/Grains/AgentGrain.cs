using MijnCopilot.Agents.Model;
using MijnCopilot.Agents.Orchestration;

namespace MijnCopilot.Orleans.Host.Grains;

public class AgentGrain : Grain, IAgentGrain
{
    private readonly IAgentRunner _agentRunner;

    public AgentGrain(IAgentRunner agentRunner)
    {
        _agentRunner = agentRunner;
    }

    public async Task<CopilotAgentResponse> RunAsync(AgentType agentType, CopilotChatHistory chatHistory)
    {
        var response = await _agentRunner.RunAsync(agentType, chatHistory);
        DeactivateOnIdle();
        return response;
    }
}