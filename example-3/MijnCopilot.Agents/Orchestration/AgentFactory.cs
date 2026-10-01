using MijnCopilot.Agents.Model;

namespace MijnCopilot.Agents.Orchestration;

internal interface IAgentFactory
{
    Task<CopilotAgentResponse> RunAsync(AgentType type, CopilotChatHistory chatHistory);
}

internal class AgentFactory : IAgentFactory
{
    private readonly IGrainFactory _grainFactory;

    public AgentFactory(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public Task<CopilotAgentResponse> RunAsync(AgentType type, CopilotChatHistory chatHistory)
    {
        var grain = _grainFactory.GetGrain<IAgentGrain>(Guid.NewGuid());
        return grain.RunAsync(type, chatHistory);
    }
}
