using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;

namespace HajimaoDesktopShop.Domain.Shops;

public sealed class RetailBusiness
{
    private readonly BusinessWallet _wallet;
    private readonly Dictionary<ShopId, Shop> _stores = [];

    private RetailBusiness(
        PlayerProfile player,
        BusinessWallet wallet,
        ShopDefinition starterDefinition)
    {
        Player = player;
        _wallet = wallet;
        _stores.Add(starterDefinition.Id, Shop.CreateWithWallet(wallet));
    }

    public static RetailBusiness Start(
        PlayerProfile player,
        Money openingCash,
        ShopDefinition starterDefinition)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(starterDefinition);
        if (starterDefinition.RequiredPlayerLevel > player.Level)
        {
            throw new ArgumentException(
                "The starter store must be unlocked for the current player.",
                nameof(starterDefinition));
        }

        return new RetailBusiness(player, new BusinessWallet(openingCash), starterDefinition);
    }

    public PlayerProfile Player { get; }

    public Money Cash => _wallet.Balance;

    public IReadOnlyList<ShopId> StoreIds => _stores.Keys.ToArray();

    public Shop GetShop(ShopId shopId) => _stores[shopId];

    public OpenShopResult TryOpenStore(ShopDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (_stores.ContainsKey(definition.Id))
        {
            return new OpenShopResult(OpenShopStatus.AlreadyOpen, definition.Id, Money.Zero);
        }

        if (Player.Level < definition.RequiredPlayerLevel)
        {
            return new OpenShopResult(OpenShopStatus.LevelLocked, definition.Id, definition.OpeningCost);
        }

        if (!_wallet.TryDebit(definition.OpeningCost))
        {
            return new OpenShopResult(
                OpenShopStatus.InsufficientFunds,
                definition.Id,
                definition.OpeningCost);
        }

        _stores.Add(definition.Id, Shop.CreateWithWallet(_wallet));
        return new OpenShopResult(OpenShopStatus.Success, definition.Id, definition.OpeningCost);
    }

    public WagePaymentResult TryPayEmployeeMinute(ShopId shopId, Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        var amount = employee.NextMinuteWage;
        if (!_stores.TryGetValue(shopId, out var shop))
        {
            return new WagePaymentResult(WagePaymentStatus.UnknownStore, amount);
        }

        if (!_wallet.TryDebit(amount))
        {
            return new WagePaymentResult(WagePaymentStatus.InsufficientFunds, amount);
        }

        var charged = employee.RecordWorkedMinute();
        if (charged != amount)
        {
            throw new InvalidOperationException("Employee wage preview changed before payment was committed.");
        }

        shop.RecordWagePayment(charged);
        return new WagePaymentResult(WagePaymentStatus.Success, charged);
    }
}
