using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Collections;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class ProductCollectionViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private readonly Action<string> _equip;

    public ProductCollectionViewModel(
        BusinessSession session,
        Func<string> selectedStoreId,
        Action<string> equip)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _selectedStoreId = selectedStoreId ?? throw new ArgumentNullException(nameof(selectedStoreId));
        _equip = equip ?? throw new ArgumentNullException(nameof(equip));
        EquipProductCommand = new RelayCommand<string>(EquipProduct);
        Refresh();
    }

    public ObservableCollection<ProductCollectionItemViewModel> Products { get; } = [];

    public IRelayCommand<string> EquipProductCommand { get; }

    public void Refresh()
    {
        Products.Clear();
        var combat = _session.Combat;
        if (combat is null || string.IsNullOrWhiteSpace(_selectedStoreId()))
        {
            return;
        }

        var snapshot = combat.GetSnapshot();
        var loadout = snapshot.Loadouts.SingleOrDefault(item => item.StoreId == _selectedStoreId());
        if (loadout is null)
        {
            return;
        }

        var equipped = loadout.ProductIds.ToHashSet(StringComparer.Ordinal);
        var definitions = combat.GetProductDefinitions().ToDictionary(item => item.ProductId, StringComparer.Ordinal);
        var displayNames = combat.GetProductDisplayNames();
        foreach (var entry in snapshot.Collection.OrderBy(item => item.ProductId, StringComparer.Ordinal))
        {
            var product = definitions[entry.ProductId];
            var effectivePower = product.BasePower
                * ProductMasteryScaling.PowerPermille(entry.MasteryLevel)
                / 1_000;
            var effectiveRevenue = product.RevenueModifierPermille
                * ProductMasteryScaling.RevenuePermille(entry.MasteryLevel)
                / 1_000;
            Products.Add(new ProductCollectionItemViewModel(
                entry.ProductId,
                StoreLoadoutViewModel.DisplayName(entry.ProductId, displayNames),
                entry.MasteryLevel,
                MasteryProgress(entry),
                $"威力 {effectivePower} · 间隔 {product.AttackIntervalTicks}",
                $"收益 ×{effectiveRevenue / 1000d:0.00}",
                EffectText(product),
                equipped.Contains(entry.ProductId)));
        }
    }

    private void EquipProduct(string? productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return;
        }

        _equip(productId);
        Refresh();
    }

    private static string MasteryProgress(ProductCollectionEntry entry) =>
        entry.MasteryLevel >= 20
            ? "熟练度 MAX"
            : $"熟练度 {entry.MasteryLevel} · {entry.StoredCopies}/{ProductCollection.CopiesRequired(entry.MasteryLevel)}";

    private static string EffectText(ProductCombatDefinition product) => product.Effect switch
    {
        ProductEffectKind.None => "稳定单体",
        ProductEffectKind.Splash => $"溅射 {product.EffectStrengthPermille / 10d:0}%",
        ProductEffectKind.Slow => $"减速 {product.EffectStrengthPermille / 10d:0}%",
        ProductEffectKind.BonusDrop => $"掉落增益 {product.EffectStrengthPermille / 10d:0}%",
        _ => ""
    };
}

public sealed record ProductCollectionItemViewModel(
    string ProductId,
    string Name,
    int MasteryLevel,
    string MasteryProgressText,
    string PowerText,
    string RevenueText,
    string EffectText,
    bool IsEquipped);
