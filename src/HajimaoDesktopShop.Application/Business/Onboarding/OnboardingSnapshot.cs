namespace HajimaoDesktopShop.Application.Business.Onboarding;

public sealed class OnboardingSnapshot
{
    private static readonly OnboardingTaskId[] OrderedTaskIds = Enum.GetValues<OnboardingTaskId>();

    public OnboardingSnapshot(
        IEnumerable<OnboardingTaskState> tasks,
        int completedTasks,
        OnboardingTaskId? currentTaskId)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        var taskArray = tasks.ToArray();
        ValidateTasks(taskArray, completedTasks, currentTaskId);

        Tasks = Array.AsReadOnly(taskArray);
        CompletedTasks = completedTasks;
        CurrentTaskId = currentTaskId;
    }

    public IReadOnlyList<OnboardingTaskState> Tasks { get; }

    public int CompletedTasks { get; }

    public OnboardingTaskId? CurrentTaskId { get; }

    public int TotalTasks => Tasks.Count;

    public bool IsComplete => CurrentTaskId is null;

    private static void ValidateTasks(
        OnboardingTaskState[] tasks,
        int completedTasks,
        OnboardingTaskId? currentTaskId)
    {
        if (tasks.Length != OrderedTaskIds.Length)
        {
            throw new ArgumentException("Onboarding snapshot must include every configured task.", nameof(tasks));
        }

        if (completedTasks < 0 || completedTasks > tasks.Length)
        {
            throw new ArgumentException("Completed task count is outside the task range.", nameof(completedTasks));
        }

        for (var i = 0; i < tasks.Length; i++)
        {
            if (tasks[i] is null)
            {
                throw new ArgumentException("Onboarding tasks cannot contain null entries.", nameof(tasks));
            }

            if (i >= OrderedTaskIds.Length || tasks[i].Id != OrderedTaskIds[i])
            {
                throw new ArgumentException("Onboarding tasks must be unique and in the configured order.", nameof(tasks));
            }
        }

        var actualCompletedTasks = tasks.Count(task => task.IsCompleted);
        if (completedTasks != actualCompletedTasks)
        {
            throw new ArgumentException("Completed task count must match task states.", nameof(completedTasks));
        }

        var firstIncompleteTaskId = tasks.FirstOrDefault(task => !task.IsCompleted)?.Id;
        if (currentTaskId != firstIncompleteTaskId)
        {
            throw new ArgumentException("Current task must be the first incomplete task.", nameof(currentTaskId));
        }
    }
}
