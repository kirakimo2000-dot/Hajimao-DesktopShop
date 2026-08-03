namespace HajimaoDesktopShop.Domain.Economy;

public sealed class BusinessWallet
{
    public BusinessWallet(Money openingBalance)
    {
        if (openingBalance.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openingBalance));
        }

        Balance = openingBalance;
    }

    public Money Balance { get; private set; }

    internal bool TryDebit(Money amount)
    {
        if (amount.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        if (Balance.Cents < amount.Cents)
        {
            return false;
        }

        Balance -= amount;
        return true;
    }

    internal void Credit(Money amount)
    {
        if (amount.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        Balance += amount;
    }
}
