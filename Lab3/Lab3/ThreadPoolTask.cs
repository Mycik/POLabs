namespace Lab3;

public class ThreadPoolTask
{
    private Guid Id { get; } = Guid.NewGuid();
    public int ExecutionTimeMs { get; init; }
    public Action? Action { get; init; }
    public string GuidIndex => Id.ToString()[(Id.ToString().Length - 2)..];
}
