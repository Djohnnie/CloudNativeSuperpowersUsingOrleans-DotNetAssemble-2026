namespace MijnCopilot.Contracts.Model;

[GenerateSerializer]
public class ChatInfo
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public string UserId { get; set; } = string.Empty;
    [Id(2)] public string Title { get; set; } = string.Empty;
    [Id(3)] public bool IsArchived { get; set; }
    [Id(4)] public DateTime StartedOn { get; set; }
    [Id(5)] public DateTime LastActivityOn { get; set; }
}
