namespace HajimaoDesktopShop.Domain.Economy;

public readonly record struct Money(long Cents) : IComparable<Money>
{
    public static Money Zero => new(0);

    public decimal Yuan => Cents / 100m;

    public bool IsPositive => Cents > 0;

    public static Money FromYuan(decimal yuan) =>
        new(checked((long)decimal.Round(yuan * 100m, 0, MidpointRounding.AwayFromZero)));

    public static Money operator +(Money left, Money right) =>
        new(checked(left.Cents + right.Cents));

    public static Money operator -(Money left, Money right) =>
        new(checked(left.Cents - right.Cents));

    public static Money operator *(Money value, int quantity) =>
        new(checked(value.Cents * quantity));

    public int CompareTo(Money other) => Cents.CompareTo(other.Cents);

    public override string ToString() => $"¥{Yuan:0.00}";
}
