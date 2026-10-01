namespace MijnCopilot.Contracts.Model;

[GenerateSerializer]
public class UserInfo
{
    [Id(0)] public string UserId { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string Email { get; set; } = string.Empty;
    [Id(3)] public DateTime LastSeenOn { get; set; }
}
