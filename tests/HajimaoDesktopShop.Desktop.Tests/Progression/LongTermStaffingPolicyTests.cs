using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Desktop.Tests.Progression;

public sealed class LongTermStaffingPolicyTests
{
    public static TheoryData<LongTermProgressionPolicy, EmployeeRole, bool> EmptyLeanStoreCandidates => new()
    {
        { LongTermProgressionPolicy.HighMargin, EmployeeRole.Cashier, false },
        { LongTermProgressionPolicy.HighMargin, EmployeeRole.Restocker, false },
        { LongTermProgressionPolicy.HighMargin, EmployeeRole.Manager, true },
        { LongTermProgressionPolicy.CashPreservation, EmployeeRole.Cashier, false },
        { LongTermProgressionPolicy.CashPreservation, EmployeeRole.Restocker, false },
        { LongTermProgressionPolicy.CashPreservation, EmployeeRole.Manager, true }
    };

    public static TheoryData<LongTermProgressionPolicy, int, bool, bool, EmployeeRole, bool> PartialLeanStoreCandidates => new()
    {
        {
            LongTermProgressionPolicy.HighMargin,
            1,
            true,
            false,
            EmployeeRole.Cashier,
            false
        },
        {
            LongTermProgressionPolicy.HighMargin,
            1,
            true,
            false,
            EmployeeRole.Restocker,
            true
        },
        {
            LongTermProgressionPolicy.HighMargin,
            1,
            false,
            true,
            EmployeeRole.Cashier,
            true
        },
        {
            LongTermProgressionPolicy.HighMargin,
            1,
            false,
            true,
            EmployeeRole.Restocker,
            false
        },
        {
            LongTermProgressionPolicy.CashPreservation,
            1,
            true,
            false,
            EmployeeRole.Cashier,
            false
        },
        {
            LongTermProgressionPolicy.CashPreservation,
            1,
            true,
            false,
            EmployeeRole.Restocker,
            true
        },
        {
            LongTermProgressionPolicy.CashPreservation,
            1,
            false,
            true,
            EmployeeRole.Cashier,
            true
        },
        {
            LongTermProgressionPolicy.CashPreservation,
            1,
            false,
            true,
            EmployeeRole.Restocker,
            false
        }
    };

    public static TheoryData<int, bool, bool, EmployeeRole, bool> HighTurnoverCandidates => new()
    {
        { 0, false, false, EmployeeRole.Cashier, true },
        { 0, false, false, EmployeeRole.Restocker, true },
        { 1, true, false, EmployeeRole.Restocker, true },
        { 1, true, false, EmployeeRole.Cashier, false },
        { 1, false, true, EmployeeRole.Cashier, true },
        { 1, false, true, EmployeeRole.Restocker, false },
        { 1, true, true, EmployeeRole.Cashier, true },
        { 2, true, true, EmployeeRole.Cashier, false }
    };

    [Theory]
    [MemberData(nameof(EmptyLeanStoreCandidates))]
    public void EmptyLeanStoreRequiresDualCapabilityCandidate(
        LongTermProgressionPolicy policy,
        EmployeeRole candidate,
        bool expected)
    {
        var need = new LongTermStoreStaffingNeed(
            EmployeeCount: 0,
            HasCheckout: false,
            HasRestock: false);

        Assert.Equal(expected, LongTermStaffingPolicy.ShouldRecruit(policy, need, candidate));
    }

    [Theory]
    [MemberData(nameof(PartialLeanStoreCandidates))]
    public void LeanStoreWithPartialCoverageRecruitsOnlyMissingRole(
        LongTermProgressionPolicy policy,
        int employeeCount,
        bool hasCheckout,
        bool hasRestock,
        EmployeeRole candidate,
        bool expected)
    {
        var need = new LongTermStoreStaffingNeed(employeeCount, hasCheckout, hasRestock);

        Assert.Equal(expected, LongTermStaffingPolicy.ShouldRecruit(policy, need, candidate));
    }

    [Theory]
    [MemberData(nameof(HighTurnoverCandidates))]
    public void HighTurnoverPreservesTwoStaffCheckoutAndRestockCoverage(
        int employeeCount,
        bool hasCheckout,
        bool hasRestock,
        EmployeeRole candidate,
        bool expected)
    {
        var need = new LongTermStoreStaffingNeed(employeeCount, hasCheckout, hasRestock);

        Assert.Equal(
            expected,
            LongTermStaffingPolicy.ShouldRecruit(
                LongTermProgressionPolicy.HighTurnover,
                need,
                candidate));
    }
}
