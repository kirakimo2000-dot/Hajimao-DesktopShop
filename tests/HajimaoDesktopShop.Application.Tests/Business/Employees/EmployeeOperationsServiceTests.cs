using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Tests.Business.Employees;

public sealed class EmployeeOperationsServiceTests
{
    [Fact]
    public void HireCandidate_DebitsFortyHoursAndAddsEmployeeOnce()
    {
        var gateway = new FakeGateway(1_000_000, "store-1");
        var service = CreateService(gateway, CreateCandidate("candidate-1", 1_800));

        var hired = service.Hire("candidate-1", "store-1");
        var duplicate = service.Hire("candidate-1", "store-1");
        var employee = Assert.Single(service.GetSnapshot().Employees);

        Assert.Equal(EmployeeCommandStatus.Success, hired.Status);
        Assert.Equal(new Money(72_000), hired.Cost);
        Assert.Equal(EmployeeCommandStatus.UnknownCandidate, duplicate.Status);
        Assert.Equal(928_000, gateway.CashCents);
        Assert.Equal("employee-000001", employee.EmployeeId);
        Assert.Equal("legacy", employee.ProfileId);
        Assert.Equal("store-1", employee.StoreId);
        Assert.Equal(480, employee.ShiftStartMinute);
        Assert.Equal(960, employee.ShiftEndMinute);
        Assert.Empty(service.GetSnapshot().Candidates);
    }

    [Fact]
    public void HireCandidate_WithInsufficientCash_IsAtomic()
    {
        var gateway = new FakeGateway(71_999, "store-1");
        var service = CreateService(gateway, CreateCandidate("candidate-1", 1_800));

        var result = service.Hire("candidate-1", "store-1");

        Assert.Equal(EmployeeCommandStatus.InsufficientFunds, result.Status);
        Assert.Equal(71_999, gateway.CashCents);
        Assert.Empty(service.GetSnapshot().Employees);
        Assert.Single(service.GetSnapshot().Candidates);
    }

    [Fact]
    public void HireCandidate_ForUnknownStore_IsAtomic()
    {
        var gateway = new FakeGateway(1_000_000, "store-1");
        var service = CreateService(gateway, CreateCandidate("candidate-1", 1_800));

        var result = service.Hire("candidate-1", "missing-store");

        Assert.Equal(EmployeeCommandStatus.UnknownStore, result.Status);
        Assert.Equal(1_000_000, gateway.CashCents);
        Assert.Empty(service.GetSnapshot().Employees);
        Assert.Single(service.GetSnapshot().Candidates);
    }

    [Fact]
    public void TrainEmployee_DebitsConfiguredCostAndIncreasesTrainingLevel()
    {
        var gateway = new FakeGateway(1_000_000, "store-1");
        var service = CreateService(gateway, CreateCandidate("candidate-1", 1_800));
        service.Hire("candidate-1", "store-1");

        var first = service.Train("employee-000001");
        var second = service.Train("employee-000001");
        var employee = Assert.Single(service.GetSnapshot().Employees);

        Assert.Equal(EmployeeCommandStatus.Success, first.Status);
        Assert.Equal(new Money(14_400), first.Cost);
        Assert.Equal(EmployeeCommandStatus.Success, second.Status);
        Assert.Equal(new Money(28_800), second.Cost);
        Assert.Equal(2, employee.TrainingLevel);
        Assert.Equal(884_800, gateway.CashCents);
    }

    [Fact]
    public void TrainingAtMaximumLevel_DoesNotDebitCash()
    {
        var gateway = new FakeGateway(1_000_000, "store-1");
        var service = CreateService(gateway, CreateCandidate("candidate-1", 1_800));
        service.Hire("candidate-1", "store-1");
        for (var level = 0; level < 5; level++)
        {
            Assert.Equal(EmployeeCommandStatus.Success, service.Train("employee-000001").Status);
        }

        var cashBefore = gateway.CashCents;
        var result = service.Train("employee-000001");

        Assert.Equal(EmployeeCommandStatus.MaximumTraining, result.Status);
        Assert.Equal(cashBefore, gateway.CashCents);
    }

    [Fact]
    public void AssignStore_MovesEmployeeAndRetainsShiftHours()
    {
        var gateway = new FakeGateway(1_000_000, "store-1", "store-2");
        var service = CreateService(gateway, CreateCandidate("candidate-1", 1_800));
        service.Hire("candidate-1", "store-1");
        service.SetShift("employee-000001", 600, 900);

        var result = service.AssignStore("employee-000001", "store-2");
        var employee = Assert.Single(service.GetSnapshot().Employees);

        Assert.Equal(EmployeeCommandStatus.Success, result.Status);
        Assert.Equal("store-2", employee.StoreId);
        Assert.Equal(600, employee.ShiftStartMinute);
        Assert.Equal(900, employee.ShiftEndMinute);
    }

    [Fact]
    public void CandidateGeneration_IsDeterministicAndAdvancesRestorableState()
    {
        var first = new EmployeeOperationsService(new FakeGateway(1_000_000, "store-1"), 42UL, 1);
        var second = new EmployeeOperationsService(new FakeGateway(1_000_000, "store-1"), 42UL, 1);

        Assert.Equivalent(first.GetSnapshot(), second.GetSnapshot(), strict: true);

        first.RefreshCandidates();
        second.RefreshCandidates();

        Assert.Equivalent(first.GetSnapshot(), second.GetSnapshot(), strict: true);
        Assert.Equal(7, first.GetSnapshot().NextCandidateId);
    }

    [Fact]
    public void CandidateGeneration_WithProfiles_UsesProfileIdentityAndAllowedRoles()
    {
        var profiles = new[]
        {
            new EmployeeProfileDefinition(
                "profile-cashier",
                "林小雨",
                "cn",
                "employee-a01",
                [EmployeeRole.Cashier],
                1100,
                900,
                "社区门店经验。")
        };
        var service = new EmployeeOperationsService(
            new FakeGateway(1_000_000, "store-1"),
            42UL,
            1,
            candidates: null,
            profiles);

        var candidates = service.GetSnapshot().Candidates;

        Assert.Equal(3, candidates.Count);
        Assert.All(candidates, candidate =>
        {
            Assert.Equal("profile-cashier", candidate.ProfileId);
            Assert.Equal("林小雨", candidate.Name);
            Assert.Equal(EmployeeRole.Cashier, candidate.Role);
            Assert.InRange(candidate.EfficiencyPermille, 880, 1375);
        });

        var hired = service.Hire(candidates[0].CandidateId, "store-1");
        Assert.Equal(EmployeeCommandStatus.Success, hired.Status);
        Assert.Equal("profile-cashier", Assert.Single(service.GetSnapshot().Employees).ProfileId);
    }

    private static EmployeeOperationsService CreateService(
        IEmployeeOperationsGateway gateway,
        params EmployeeCandidate[] candidates) =>
        new(gateway, 42UL, 2, candidates);

    private static EmployeeCandidate CreateCandidate(string id, long hourlyWageCents) =>
        new(id, "小葵", EmployeeRole.Cashier, 1_000, new Money(hourlyWageCents));

    private sealed class FakeGateway(long cashCents, params string[] storeIds)
        : IEmployeeOperationsGateway
    {
        private readonly HashSet<string> _storeIds = storeIds.ToHashSet(StringComparer.Ordinal);

        public long CashCents { get; private set; } = cashCents;

        public bool IsStoreOpen(string storeId) => _storeIds.Contains(storeId);

        public bool TryDebitEmployeeExpense(Money amount)
        {
            if (CashCents < amount.Cents)
            {
                return false;
            }

            CashCents -= amount.Cents;
            return true;
        }
    }
}
