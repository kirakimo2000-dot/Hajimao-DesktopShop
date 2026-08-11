using CommunityToolkit.Mvvm.ComponentModel;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class NextActionViewModel : ObservableObject
{
    private string _contextText = string.Empty;
    private string _title = string.Empty;
    private string _detailText = string.Empty;
    private string _actionText = "前往";
    private ManagementSection _suggestedSection = ManagementSection.Overview;

    public string ContextText
    {
        get => _contextText;
        private set => SetProperty(ref _contextText, value);
    }

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string DetailText
    {
        get => _detailText;
        private set => SetProperty(ref _detailText, value);
    }

    public string ActionText
    {
        get => _actionText;
        private set => SetProperty(ref _actionText, value);
    }

    public ManagementSection SuggestedSection
    {
        get => _suggestedSection;
        private set => SetProperty(ref _suggestedSection, value);
    }

    public void Update(
        OnboardingViewModel onboarding,
        LongTermProgressionViewModel progression)
    {
        ArgumentNullException.ThrowIfNull(onboarding);
        ArgumentNullException.ThrowIfNull(progression);

        ActionText = "前往";
        if (onboarding.IsVisible)
        {
            ContextText = onboarding.ProgressText;
            Title = onboarding.Title;
            DetailText = onboarding.Guidance;
            SuggestedSection = onboarding.SuggestedSection;
            return;
        }

        ContextText = "长期目标";
        Title = progression.TitleText;
        DetailText = progression.ProgressText;
        SuggestedSection = progression.SuggestedSection;
    }
}
