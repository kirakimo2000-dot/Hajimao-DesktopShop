using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Collections;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StoreLoadoutViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private string _statusMessage = "商品会自动投掷；更换组合即可改变招待效率与收益。";

    public StoreLoadoutViewModel(BusinessSession session, Func<string> selectedStoreId)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _selectedStoreId = selectedStoreId ?? throw new ArgumentNullException(nameof(selectedStoreId));
        UseRecommendedLoadoutCommand = new RelayCommand(UseRecommendedLoadout);
        Refresh();
    }

    public ObservableCollection<StoreLoadoutSlotViewModel> Slots { get; } = [];

    public IRelayCommand UseRecommendedLoadoutCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Refresh()
    {
        Slots.Clear();
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
        var definitions = combat.GetProductDefinitions().ToDictionary(item => item.ProductId, StringComparer.Ordinal);
        var displayNames = combat.GetProductDisplayNames();
        var mastery = snapshot.Collection.ToDictionary(item => item.ProductId, StringComparer.Ordinal);
        for (var index = 0; index < loadout.UnlockedSlots; index++)
        {
            var productId = index < loadout.ProductIds.Count ? loadout.ProductIds[index] : null;
            if (productId is null)
            {
                Slots.Add(new StoreLoadoutSlotViewModel(index + 1, null, "空装备栏", "等待选择商品", true));
                continue;
            }

            var product = definitions[productId];
            var level = mastery[productId].MasteryLevel;
            var effectivePower = product.BasePower * ProductMasteryScaling.PowerPermille(level) / 1_000;
            var effectiveRevenue = product.RevenueModifierPermille * ProductMasteryScaling.RevenuePermille(level) / 1_000;
            Slots.Add(new StoreLoadoutSlotViewModel(
                index + 1,
                productId,
                DisplayName(productId, displayNames),
                $"威力 {effectivePower} · 收益 ×{effectiveRevenue / 1000d:0.00}",
                false));
        }
    }

    public void Equip(string productId)
    {
        var combat = _session.Combat
            ?? throw new InvalidOperationException("Combat session is unavailable.");
        var storeId = _selectedStoreId();
        var loadout = combat.GetSnapshot().Loadouts.Single(item => item.StoreId == storeId);
        if (loadout.ProductIds.Contains(productId, StringComparer.Ordinal))
        {
            StatusMessage = $"{DisplayName(productId, combat.GetProductDisplayNames())} 已在当前组合中。";
            return;
        }

        var slot = loadout.ProductIds.Count < loadout.UnlockedSlots
            ? loadout.ProductIds.Count
            : FindWeakestSlot(loadout.ProductIds, combat.GetProductDefinitions(), combat.GetSnapshot().Collection);
        combat.Equip(storeId, slot, productId);
        StatusMessage = $"已装备 {DisplayName(productId, combat.GetProductDisplayNames())}，战斗组合立即生效。";
        Refresh();
    }

    private void UseRecommendedLoadout()
    {
        var combat = _session.Combat;
        if (combat is null)
        {
            return;
        }

        var storeId = _selectedStoreId();
        var snapshot = combat.GetSnapshot();
        var mastery = snapshot.Collection.ToDictionary(entry => entry.ProductId, StringComparer.Ordinal);
        var slots = snapshot.Loadouts.Single(item => item.StoreId == storeId).UnlockedSlots;
        var recommended = combat.GetProductDefinitions()
            .Where(product => mastery.ContainsKey(product.ProductId))
            .OrderByDescending(product => Score(product, mastery[product.ProductId].MasteryLevel))
            .ThenBy(product => product.ProductId, StringComparer.Ordinal)
            .Take(slots)
            .Select(product => product.ProductId)
            .ToArray();
        combat.ReplaceLoadout(storeId, recommended);
        StatusMessage = "已应用推荐组合：优先兼顾招待速度、威力与收入。";
        Refresh();
    }

    private static int FindWeakestSlot(
        IReadOnlyList<string> productIds,
        IReadOnlyList<ProductCombatDefinition> definitions,
        IReadOnlyList<ProductCollectionEntry> collection)
    {
        var map = definitions.ToDictionary(item => item.ProductId, StringComparer.Ordinal);
        var mastery = collection.ToDictionary(item => item.ProductId, StringComparer.Ordinal);
        return productIds
            .Select((productId, index) => (
                Index: index,
                Score: Score(map[productId], mastery[productId].MasteryLevel)))
            .MinBy(item => item.Score)
            .Index;
    }

    private static long Score(ProductCombatDefinition product, int masteryLevel) =>
        checked(((long)product.BasePower
                * ProductMasteryScaling.PowerPermille(masteryLevel)
                * 100
                / product.AttackIntervalTicks)
            + ((long)product.RevenueModifierPermille
                * ProductMasteryScaling.RevenuePermille(masteryLevel)
                / 1_000));

    internal static string DisplayName(
        string productId,
        IReadOnlyDictionary<string, string> displayNames) =>
        displayNames.GetValueOrDefault(productId, productId.Replace('_', ' '));
}

public sealed record StoreLoadoutSlotViewModel(
    int SlotNumber,
    string? ProductId,
    string Name,
    string SummaryText,
    bool IsEmpty);
