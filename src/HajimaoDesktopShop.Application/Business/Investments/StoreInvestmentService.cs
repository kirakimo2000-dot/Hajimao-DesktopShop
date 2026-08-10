using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.Investments;

public sealed class StoreInvestmentService
{
    private readonly BusinessGameService _game;
    private readonly BusinessSimulation _simulation;
    private readonly InvestmentTracker _tracker;

    public StoreInvestmentService(
        BusinessGameService game,
        BusinessSimulation simulation,
        InvestmentTrackingSaveData? restoredTracking = null)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        _tracker = new InvestmentTracker(restoredTracking);
    }

    public StoreInvestmentPortfolio? GetPortfolio(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            return null;
        }

        var normalizedStoreId = storeId.Trim();
        if (!_game.IsStoreOpen(normalizedStoreId))
        {
            return null;
        }

        var snapshot = _simulation.GetSnapshot();
        var economy = StoreEconomyAnalysisService.Calculate(snapshot, normalizedStoreId);
        if (economy is null)
        {
            return null;
        }

        var storePortfolio = StoreInvestmentAdvisor.Create(
            snapshot,
            _game.GetStoreGrowthSnapshot(normalizedStoreId),
            economy);
        var openingCandidates = StoreOpeningInvestmentAdvisor.Create(
            snapshot,
            _game.GetStoreCatalogSnapshot(),
            economy);
        return new StoreInvestmentPortfolio(
            normalizedStoreId,
            economy,
            Array.AsReadOnly(storePortfolio.Candidates.Concat(openingCandidates).ToArray()));
    }

    public bool HasAnyInvestment => _tracker.HasAnyInvestment;

    public CapitalAllocationSnapshot GetCapitalAllocation()
    {
        var catalog = _game.GetStoreCatalogSnapshot();
        var portfolios = catalog
            .Where(store => store.IsOpen)
            .Select(store => GetPortfolio(store.Id))
            .OfType<StoreInvestmentPortfolio>()
            .ToArray();
        return CapitalAllocationAdvisor.Create(catalog, portfolios);
    }

    public InvestmentCommandResult Execute(string storeId, string candidateId)
    {
        var portfolio = GetPortfolio(storeId);
        if (portfolio is null)
        {
            return Result(InvestmentCommandStatus.UnknownStore);
        }

        if (string.IsNullOrWhiteSpace(candidateId))
        {
            return Result(InvestmentCommandStatus.UnknownCandidate);
        }

        var normalizedCandidateId = candidateId.Trim();
        var candidate = portfolio.Candidates.SingleOrDefault(item =>
            string.Equals(item.Id, normalizedCandidateId, StringComparison.Ordinal));
        if (candidate is null)
        {
            return Result(InvestmentCommandStatus.UnknownCandidate);
        }

        if (candidate.Availability == InvestmentAvailability.InsufficientFunds)
        {
            return Result(
                InvestmentCommandStatus.InsufficientFunds,
                candidate,
                candidate.Return.CostCents);
        }

        if (!candidate.IsExecutable)
        {
            return Result(
                InvestmentCommandStatus.NotAvailable,
                candidate,
                candidate.Return.CostCents);
        }

        var before = _simulation.GetSnapshot();
        var result = candidate.Kind switch
        {
            InvestmentKind.OpenStore => ExecuteOpenStore(candidate),
            InvestmentKind.Employee => ExecuteEmployee(candidate),
            _ => ExecuteGrowth(candidate)
        };
        if (result.Status == InvestmentCommandStatus.Success)
        {
            _tracker.Record(candidate, before.GameMinute, before.LastCompletedDay);
        }

        return result;
    }

    public InvestmentTrackingSnapshot? GetLatestComparison(string storeId)
    {
        var snapshot = _simulation.GetSnapshot();
        return _tracker.GetSnapshot(storeId, snapshot.LastCompletedDay);
    }

    public InvestmentTrackingSaveData CaptureTrackingSaveData() =>
        _tracker.CaptureSaveData();

    private InvestmentCommandResult ExecuteEmployee(InvestmentCandidate candidate)
    {
        var result = _simulation.Employees.Hire(candidate.TargetId, candidate.StoreId);
        var status = result.Status switch
        {
            EmployeeCommandStatus.Success => InvestmentCommandStatus.Success,
            EmployeeCommandStatus.InsufficientFunds => InvestmentCommandStatus.InsufficientFunds,
            EmployeeCommandStatus.UnknownStore => InvestmentCommandStatus.UnknownStore,
            EmployeeCommandStatus.UnknownCandidate => InvestmentCommandStatus.UnknownCandidate,
            _ => InvestmentCommandStatus.CommandRejected
        };
        return Result(status, status == InvestmentCommandStatus.Success ? candidate : null,
            result.Cost.Cents, result.EmployeeId);
    }

    private InvestmentCommandResult ExecuteOpenStore(InvestmentCandidate candidate)
    {
        var result = _game.OpenStore(candidate.TargetId);
        var status = result.Status switch
        {
            OpenShopStatus.Success => InvestmentCommandStatus.Success,
            OpenShopStatus.InsufficientFunds => InvestmentCommandStatus.InsufficientFunds,
            OpenShopStatus.LevelLocked or OpenShopStatus.AlreadyOpen => InvestmentCommandStatus.NotAvailable,
            OpenShopStatus.UnknownDefinition => InvestmentCommandStatus.UnknownStore,
            _ => InvestmentCommandStatus.CommandRejected
        };
        return Result(
            status,
            status == InvestmentCommandStatus.Success ? candidate : null,
            result.OpeningCost.Cents);
    }

    private InvestmentCommandResult ExecuteGrowth(InvestmentCandidate candidate)
    {
        var kind = candidate.Kind switch
        {
            InvestmentKind.Expansion => StoreUpgradeKind.Expansion,
            InvestmentKind.Shelf => StoreUpgradeKind.Shelf,
            InvestmentKind.Decoration => StoreUpgradeKind.Decoration,
            _ => throw new InvalidOperationException(
                $"Investment kind '{candidate.Kind}' is not a growth investment.")
        };
        var result = _game.UpgradeStore(candidate.StoreId, kind);
        var status = result.Status switch
        {
            StoreGrowthCommandStatus.Success => InvestmentCommandStatus.Success,
            StoreGrowthCommandStatus.InsufficientFunds => InvestmentCommandStatus.InsufficientFunds,
            StoreGrowthCommandStatus.UnknownStore => InvestmentCommandStatus.UnknownStore,
            StoreGrowthCommandStatus.PrerequisiteNotMet or StoreGrowthCommandStatus.MaximumLevel =>
                InvestmentCommandStatus.NotAvailable,
            _ => InvestmentCommandStatus.CommandRejected
        };
        return Result(
            status,
            status == InvestmentCommandStatus.Success ? candidate : null,
            result.CostCents);
    }

    private static InvestmentCommandResult Result(
        InvestmentCommandStatus status,
        InvestmentCandidate? candidate = null,
        long costCents = 0,
        string? employeeId = null) =>
        new(status, candidate, costCents, employeeId);
}
