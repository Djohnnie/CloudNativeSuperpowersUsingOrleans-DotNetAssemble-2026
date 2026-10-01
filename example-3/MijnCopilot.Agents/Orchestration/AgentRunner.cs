using Microsoft.Extensions.DependencyInjection;
using MijnCopilot.Agents.Base;
using MijnCopilot.Agents.Model;

namespace MijnCopilot.Agents.Orchestration;

public interface IAgentRunner
{
    Task<CopilotAgentResponse> RunAsync(AgentType agentType, CopilotChatHistory chatHistory);
    string GetDescription(AgentType agentType);
}

internal class AgentRunner : IAgentRunner
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private static readonly IReadOnlyDictionary<AgentType, string> _descriptions =
        new Dictionary<AgentType, string>
        {
            { AgentType.Keyword,          "An agent that summarizes questions and commands in one or two keywords" },
            { AgentType.Summary,          "An agent that repeats the final question in chat history with some added context if needed" },
            { AgentType.Orchestrator,     "An agent that forwards questions and commands to the correct specialized agent" },
            { AgentType.Question,         "An agent that checks if all questions are answered" },
            { AgentType.Reply,            "An agent that summarizes all replies to a question into a single reply" },
            { AgentType.General,          "An agent that can answer general questions" },
            { AgentType.MijnThuisPower,   "An agent that has real-time knowledge on my power usage via MijnThuis (power usage; peak power usage this month; energy imported and exported today and this month; energy cost today and this month; current energy consumption and injection price)" },
            { AgentType.MijnThuisSolar,   "An agent that has real-time knowledge on my solar installation via MijnThuis (current solar production; current battery power; current grid power; solar/home battery charge percentage and health; solar energy production today and this month; solar energy forecast today and tomorrow)" },
            { AgentType.MijnThuisCar,     "An agent that has real-time knowledge on my electric car via MijnThuis (current car location; car battery charge percentage and health; locking and unlocking car; starting and stopping car charging)" },
            { AgentType.MijnThuisHeating, "An agent that has real-time knowledge on my heating installation via MijnThuis (current temperature in my home and outside; current setpoint temperature and next setpoint temperature; next setpoint scheduled time)" },
            { AgentType.MijnThuisSmartLock, "An agent that has real-time knowledge on my smart lock via MijnThuis (current lock state; current door state; smart lock battery charge percentage)" },
            { AgentType.MijnSauna,        "An agent that has real-time knowledge on my sauna via MijnSauna (sauna status: off, sauna or infrared; current temperature inside sauna cabin)" },
            { AgentType.PhotoCarousel,    "An agent that has real-time knowledge on my displayed photos via PhotoCarousel (get information about current, previous and next displayed photo)" },
        };

    public AgentRunner(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<CopilotAgentResponse> RunAsync(AgentType agentType, CopilotChatHistory chatHistory)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        AgentFactoryBase factory = agentType switch
        {
            AgentType.Keyword        => scope.ServiceProvider.GetRequiredService<KeywordAgentFactory>(),
            AgentType.Summary        => scope.ServiceProvider.GetRequiredService<SummaryAgentFactory>(),
            AgentType.Orchestrator   => scope.ServiceProvider.GetRequiredService<OrchestratorAgentFactory>(),
            AgentType.Question       => scope.ServiceProvider.GetRequiredService<QuestionAgentFactory>(),
            AgentType.Reply          => scope.ServiceProvider.GetRequiredService<ReplyAgentFactory>(),
            AgentType.General        => scope.ServiceProvider.GetRequiredService<GeneralAgentFactory>(),
            AgentType.MijnThuisPower => scope.ServiceProvider.GetRequiredService<MijnThuisPowerAgentFactory>(),
            AgentType.MijnThuisSolar => scope.ServiceProvider.GetRequiredService<MijnThuisSolarAgentFactory>(),
            AgentType.MijnThuisCar   => scope.ServiceProvider.GetRequiredService<MijnThuisCarAgentFactory>(),
            AgentType.MijnThuisHeating    => scope.ServiceProvider.GetRequiredService<MijnThuisHeatingAgentFactory>(),
            AgentType.MijnThuisSmartLock  => scope.ServiceProvider.GetRequiredService<MijnThuisSmartLockAgentFactory>(),
            AgentType.MijnSauna      => scope.ServiceProvider.GetRequiredService<MijnSaunaAgentFactory>(),
            AgentType.PhotoCarousel  => scope.ServiceProvider.GetRequiredService<PhotoCarouselAgentFactory>(),
            _ => throw new NotImplementedException($"Agent type {agentType} is not implemented.")
        };

        var agent = await factory.Create();
        return await agent.Chat(chatHistory);
    }

    public string GetDescription(AgentType agentType)
        => _descriptions.TryGetValue(agentType, out var description) ? description : agentType.ToString();
}
