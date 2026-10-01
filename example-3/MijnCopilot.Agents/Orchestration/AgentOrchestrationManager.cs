using MijnCopilot.Agents.Base;
using MijnCopilot.Agents.Model;
using System.Text;

namespace MijnCopilot.Agents.Orchestration;

public interface IAgentOrchestrationManager
{
    Task<CopilotChatHistory> Chat(CopilotChatHistory chat);
}

internal class AgentOrchestrationManager : IAgentOrchestrationManager
{
    private readonly IAgentFactory _agentFactory;
    private readonly IAgentRunner _agentRunner;

    private static readonly AgentType[] _orchestratedAgentTypes =
    [
        AgentType.General,
        AgentType.MijnThuisPower,
        AgentType.MijnThuisSolar,
        AgentType.MijnThuisCar,
        AgentType.MijnThuisHeating,
        AgentType.MijnThuisSmartLock,
        AgentType.MijnSauna,
        AgentType.PhotoCarousel,
    ];

    public AgentOrchestrationManager(IAgentFactory agentFactory, IAgentRunner agentRunner)
    {
        _agentFactory = agentFactory;
        _agentRunner = agentRunner;
    }

    public async Task<CopilotChatHistory> Chat(CopilotChatHistory chat)
    {
        var workingChat = chat.Copy();

        // 1. Summarize the conversation so far to focus on the question asked by the user.
        var summaryResponse = await _agentFactory.RunAsync(AgentType.Summary, workingChat);
        workingChat.InputTokenCount += summaryResponse.InputTokenCount;
        workingChat.OutputTokenCount += summaryResponse.OutputTokenCount;
        workingChat.AddDebug(isQuestion: true, summaryResponse.Response, summaryResponse.AgentName);

        // 2. Build the orchestrator prompt listing available agents and their descriptions.
        var agents = string.Join(",", _orchestratedAgentTypes.Select(t => $"Name: {t}; Description: {_agentRunner.GetDescription(t)}"));
        var systemPromptBuilder = new StringBuilder();
        systemPromptBuilder.AppendLine("Based on the conversation so far, which agents are best suited to respond to the questions asked?");
        systemPromptBuilder.AppendLine($"The available agents are: {agents}.");
        systemPromptBuilder.AppendLine("If the question exists out of multiple sub-questions, start by rewriting the original question in multiple distinct questions.");
        systemPromptBuilder.AppendLine("Next: For each question, select the most appropriate agent from the list of available agents.");
        systemPromptBuilder.AppendLine("Respond with a line for each question, including the rewritten question followed by a semicolon, followed by the name of the agent that should answer that question.");

        var orchestratorChatHistory = new CopilotChatHistory();
        orchestratorChatHistory.AddSystemMessage(systemPromptBuilder.ToString());
        orchestratorChatHistory.AddUserMessage(summaryResponse.Response);

        // 3. Ask the orchestrator to assign questions to the correct agents.
        var orchestratorResponse = await _agentFactory.RunAsync(AgentType.Orchestrator, orchestratorChatHistory);
        workingChat.InputTokenCount += orchestratorResponse.InputTokenCount;
        workingChat.OutputTokenCount += orchestratorResponse.OutputTokenCount;

        // 4. Run the assigned agents in parallel.
        var agentTasks = new List<(string Question, Task<CopilotAgentResponse> Task)>();
        foreach (var line in orchestratorResponse.Response.Split("\n"))
        {
            var parts = line.Split(";");
            if (parts.Length < 2) continue;

            var question = parts[0].Trim();
            var agentType = Enum.Parse<AgentType>(parts[1].Trim());
            agentTasks.Add((question, _agentFactory.RunAsync(agentType, new CopilotChatHistory(question, CopilotChatRole.User))));
        }

        var responses = await Task.WhenAll(agentTasks.Select(x => x.Task));

        // 5. Combine agent responses into a single reply.
        var replyAgentChat = new CopilotChatHistory();
        replyAgentChat.AddUserMessage(summaryResponse.Response);
        foreach (var (agentTask, response) in agentTasks.Zip(responses))
        {
            replyAgentChat.AddAssistantMessage(response.Response);
            workingChat.InputTokenCount += response.InputTokenCount;
            workingChat.OutputTokenCount += response.OutputTokenCount;
            workingChat.AddDebug(isQuestion: true, agentTask.Question, orchestratorResponse.AgentName);
            workingChat.AddDebug(isQuestion: false, response.Response, response.AgentName);
        }

        var replyResponse = await _agentFactory.RunAsync(AgentType.Reply, replyAgentChat);
        workingChat.AddDebug(isQuestion: false, replyResponse.Response, replyResponse.AgentName);

        workingChat.AddAssistantMessage(replyResponse.Response);
        workingChat.LastAssistantMessage = replyResponse.Response;
        workingChat.AgentName = string.Join(", ", responses.Select(x => x.AgentName).Distinct());
        workingChat.InputTokenCount += replyResponse.InputTokenCount;
        workingChat.OutputTokenCount += replyResponse.OutputTokenCount;
        return workingChat;
    }
}
