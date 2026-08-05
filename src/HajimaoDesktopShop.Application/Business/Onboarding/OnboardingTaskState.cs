namespace HajimaoDesktopShop.Application.Business.Onboarding;

public sealed record OnboardingTaskState(
    OnboardingTaskId Id,
    bool IsCompleted);
