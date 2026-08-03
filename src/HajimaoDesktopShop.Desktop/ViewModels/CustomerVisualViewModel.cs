using HajimaoDesktopShop.Application.Simulation.Customers;

namespace HajimaoDesktopShop.Desktop.ViewModels;

public sealed record CustomerVisualViewModel(
    long Id,
    string StateText,
    string? SelectedProductId,
    double X,
    double Y)
{
    public static CustomerVisualViewModel FromSnapshot(CustomerSnapshot snapshot)
    {
        var (stateText, x, y) = snapshot.State switch
        {
            CustomerState.Entering => ("进店", 16d, 118d),
            CustomerState.SeekingProduct => ("选购", 112d + (snapshot.Id % 3) * 44d, 72d),
            CustomerState.Queueing => ("排队", 286d + (snapshot.Id % 2) * 20d, 120d),
            CustomerState.CheckingOut => ("结账", 340d, 92d),
            CustomerState.Leaving => ("离店", 388d, 118d),
            _ => throw new ArgumentOutOfRangeException(nameof(snapshot))
        };

        return new CustomerVisualViewModel(snapshot.Id, stateText, snapshot.SelectedProductId, x, y);
    }
}
