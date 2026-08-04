using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Infrastructure.Persistence;
using HajimaoDesktopShop.Domain.Employees;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HajimaoDesktopShop.Infrastructure.Tests.Persistence;

public sealed class SqliteGameSaveStoreTests
{
    [Fact]
    public async Task NewDatabase_MigratesToCurrentSchema()
    {
        using var database = new TemporaryDatabase();
        var store = new SqliteGameSaveStore(database.Path);

        Assert.Null(await store.LoadGameAsync());

        await using var connection = new SqliteConnection($"Data Source={database.Path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(GameSaveSchema.CurrentVersion, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsGameAndWindowPlacement()
    {
        using var database = new TemporaryDatabase();
        var store = new SqliteGameSaveStore(database.Path);
        var save = CreateSave();
        var placement = new DesktopWindowPlacement(1440d, 820d, IsLocked: true);

        await store.SaveGameAsync(save);
        await store.SaveDesktopWindowPlacementAsync(placement);

        Assert.Equivalent(save, await store.LoadGameAsync(), strict: true);
        Assert.Equal(placement, await store.LoadDesktopWindowPlacementAsync());
    }

    [Fact]
    public async Task FutureDatabaseVersion_IsRejectedWithoutMutation()
    {
        using var database = new TemporaryDatabase();
        await using (var connection = new SqliteConnection($"Data Source={database.Path};Pooling=False"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 99;";
            await command.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SqliteGameSaveStore(database.Path).LoadGameAsync());

        Assert.Contains("99", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task VersionOneSave_MigratesToFixedTimeSchemaTwo(int legacySpeed)
    {
        using var database = new TemporaryDatabase();
        await CreateVersionOneDatabaseAsync(database.Path, legacySpeed);

        var migrated = await new SqliteGameSaveStore(database.Path).LoadGameAsync();

        Assert.NotNull(migrated);
        Assert.Equal(6, migrated.SchemaVersion);
        Assert.Equal(88, migrated.Simulation.GameMinute);
        Assert.Equal(7, Assert.Single(migrated.Shop.Products).Quantity);

        await using var connection = new SqliteConnection($"Data Source={database.Path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT schema_version, payload_json FROM game_save WHERE slot = 1;";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(GameSaveSchema.CurrentVersion, reader.GetInt32(0));
        Assert.DoesNotContain("speed", reader.GetString(1), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VersionTwoSave_MigratesToSchemaThreeWithoutLosingLegacyState()
    {
        using var database = new TemporaryDatabase();
        await CreateVersionTwoDatabaseAsync(database.Path);

        var migrated = await new SqliteGameSaveStore(database.Path).LoadGameAsync();

        Assert.NotNull(migrated);
        Assert.Equal(6, migrated.SchemaVersion);
        Assert.Equal(51_250, migrated.Shop.CashCents);
        Assert.Equal(7, Assert.Single(migrated.Shop.Products).Quantity);
        Assert.Equal(88, migrated.Simulation.GameMinute);
        Assert.Equal(4, migrated.Simulation.CompletedSales);
        Assert.Null(migrated.Business);
        Assert.Null(migrated.BusinessSimulation);

        await using var connection = new SqliteConnection($"Data Source={database.Path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT schema_version, payload_json FROM game_save WHERE slot = 1;";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(GameSaveSchema.CurrentVersion, reader.GetInt32(0));
        Assert.Contains("\"schemaVersion\":6", reader.GetString(1), StringComparison.Ordinal);
    }

    [Fact]
    public async Task VersionThreeSave_MigratesToCurrentSchemaWithEmptyProcurementState()
    {
        using var database = new TemporaryDatabase();
        await CreateVersionThreeDatabaseAsync(database.Path);

        var migrated = await new SqliteGameSaveStore(database.Path).LoadGameAsync();

        Assert.NotNull(migrated);
        Assert.Equal(6, migrated.SchemaVersion);
        Assert.NotNull(migrated.Business);
        Assert.NotNull(migrated.Business.Procurement);
        Assert.Equal(1, migrated.Business.Procurement.NextOrderId);
        Assert.Empty(migrated.Business.Procurement.PendingOrders);
        Assert.Empty(migrated.Business.Procurement.AutoRestockPolicies);
    }

    [Fact]
    public async Task VersionFourSave_MigratesEmployeeDefaultsAndLegacyAlwaysOnShift()
    {
        using var database = new TemporaryDatabase();
        await CreateVersionFourDatabaseAsync(database.Path);

        var migrated = await new SqliteGameSaveStore(database.Path).LoadGameAsync();

        Assert.NotNull(migrated);
        Assert.Equal(6, migrated.SchemaVersion);
        var simulation = Assert.IsType<BusinessSimulationSaveData>(migrated.BusinessSimulation);
        var employee = Assert.Single(simulation.Employees);
        Assert.Equal(0, employee.TrainingLevel);
        Assert.Equal(1_000, employee.EnergyPermille);
        Assert.Equal(700, employee.SatisfactionPermille);
        Assert.True(employee.IsAlwaysOn);
        Assert.Null(simulation.EmployeeOperations);
    }

    [Fact]
    public async Task VersionFiveSave_MigratesStoreGrowthDefaultsWithoutLosingBusinessState()
    {
        using var database = new TemporaryDatabase();
        await CreateVersionFiveDatabaseAsync(database.Path);

        var migrated = await new SqliteGameSaveStore(database.Path).LoadGameAsync();

        Assert.NotNull(migrated);
        Assert.Equal(6, migrated.SchemaVersion);
        var business = Assert.IsType<BusinessSaveData>(migrated.Business);
        var store = Assert.Single(business.Stores);
        Assert.Equal(51_250, business.CashCents);
        Assert.Equal(7, Assert.Single(store.Products).Quantity);
        Assert.Equal(0, store.OperatingCostCents);
        Assert.Null(store.Development);
        Assert.Empty(business.Promotions ?? []);

        await using var connection = new SqliteConnection($"Data Source={database.Path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(6, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    private static GameSaveData CreateSave() =>
        new(
            GameSaveSchema.CurrentVersion,
            new DateTimeOffset(2026, 8, 3, 13, 0, 0, TimeSpan.Zero),
            new ShopSaveData(
                51_250,
                4_000,
                2_500,
                1_500,
                [new ProductSaveData("water", 230, 7)]),
            new SimulationSaveData(
                88,
                88,
                3,
                4,
                [],
                [],
                null,
                [new RestockTaskSaveData("water", 2)],
                null,
                null),
            new BusinessSaveData(
                100,
                51_250,
                [
                    new BusinessStoreSaveData(
                        "corner-store",
                        4_000,
                        2_500,
                        1_500,
                        100,
                        [new BusinessProductSaveData("water", 230, 7)],
                        40_000,
                        new StoreDevelopmentSaveData(2, 3, 2))
                ],
                Promotions: [new StorePromotionSaveData("corner-store", "local-flyers", 217)]),
            new BusinessSimulationSaveData(
                88,
                42,
                [
                    new EmployeeAssignmentSaveData(
                        "corner-store",
                        "cashier",
                        "小葵",
                        EmployeeRole.Cashier,
                        1_000,
                        6_000,
                        1,
                        100,
                        0)
                ],
                [
                    new StoreRuntimeSaveData(
                        "corner-store",
                        5,
                        4,
                        3,
                        1,
                        ["water"],
                        new ActiveCheckoutSaveData("water", 2),
                        950,
                        1_000,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        12,
                        8)
                ],
                new BusinessDayReport(1, [])));

    private static async Task CreateVersionOneDatabaseAsync(string path, int speed)
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 1,
                savedAtUtc = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
                shop = new
                {
                    cashCents = 51_250,
                    totalRevenueCents = 4_000,
                    totalStockPurchaseCostCents = 2_500,
                    totalGrossProfitCents = 1_500,
                    products = new[] { new { productId = "water", salePriceCents = 230, quantity = 7 } }
                },
                simulation = new
                {
                    gameMinute = 88,
                    speed,
                    tick = 88,
                    nextCustomerId = 3,
                    completedSales = 4,
                    customers = Array.Empty<object>(),
                    checkoutQueue = Array.Empty<long>(),
                    cashierCustomerId = (long?)null,
                    restockQueue = Array.Empty<object>(),
                    activeRestockTask = (object?)null,
                    lastRestockFailure = (string?)null
                }
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE game_save (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                schema_version INTEGER NOT NULL,
                saved_at_utc TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );
            CREATE TABLE desktop_window (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                left_px REAL NOT NULL,
                top_px REAL NOT NULL,
                is_locked INTEGER NOT NULL CHECK(is_locked IN (0, 1))
            );
            INSERT INTO game_save(slot, schema_version, saved_at_utc, payload_json)
            VALUES(1, 1, '2026-08-03T12:00:00.0000000+00:00', $payload);
            PRAGMA user_version = 1;
            """;
        command.Parameters.AddWithValue("$payload", payload);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateVersionTwoDatabaseAsync(string path)
    {
        var payload = JsonSerializer.Serialize(
            new
            {
                schemaVersion = 2,
                savedAtUtc = new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
                shop = new
                {
                    cashCents = 51_250,
                    totalRevenueCents = 4_000,
                    totalStockPurchaseCostCents = 2_500,
                    totalGrossProfitCents = 1_500,
                    products = new[] { new { productId = "water", salePriceCents = 230, quantity = 7 } }
                },
                simulation = new
                {
                    gameMinute = 88,
                    tick = 88,
                    nextCustomerId = 3,
                    completedSales = 4,
                    customers = Array.Empty<object>(),
                    checkoutQueue = Array.Empty<long>(),
                    cashierCustomerId = (long?)null,
                    restockQueue = Array.Empty<object>(),
                    activeRestockTask = (object?)null,
                    lastRestockFailure = (string?)null
                }
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE game_save (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                schema_version INTEGER NOT NULL,
                saved_at_utc TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );
            CREATE TABLE desktop_window (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                left_px REAL NOT NULL,
                top_px REAL NOT NULL,
                is_locked INTEGER NOT NULL CHECK(is_locked IN (0, 1))
            );
            INSERT INTO game_save(slot, schema_version, saved_at_utc, payload_json)
            VALUES(1, 2, '2026-08-03T12:00:00.0000000+00:00', $payload);
            PRAGMA user_version = 2;
            """;
        command.Parameters.AddWithValue("$payload", payload);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateVersionThreeDatabaseAsync(string path)
    {
        var current = CreateSave();
        var legacy = current with
        {
            SchemaVersion = 3,
            Business = current.Business! with { Procurement = null }
        };
        var payload = JsonSerializer.Serialize(
            legacy,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE game_save (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                schema_version INTEGER NOT NULL,
                saved_at_utc TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );
            CREATE TABLE desktop_window (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                left_px REAL NOT NULL,
                top_px REAL NOT NULL,
                is_locked INTEGER NOT NULL CHECK(is_locked IN (0, 1))
            );
            INSERT INTO game_save(slot, schema_version, saved_at_utc, payload_json)
            VALUES(1, 3, '2026-08-03T13:00:00.0000000+00:00', $payload);
            PRAGMA user_version = 3;
            """;
        command.Parameters.AddWithValue("$payload", payload);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateVersionFourDatabaseAsync(string path)
    {
        var payloadNode = JsonSerializer.SerializeToNode(
            CreateSave(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        payloadNode["schemaVersion"] = 4;
        var businessSimulation = payloadNode["businessSimulation"]!.AsObject();
        businessSimulation.Remove("employeeOperations");
        foreach (var employeeNode in businessSimulation["employees"]!.AsArray())
        {
            var employee = employeeNode!.AsObject();
            employee.Remove("trainingLevel");
            employee.Remove("energyPermille");
            employee.Remove("satisfactionPermille");
            employee.Remove("workMinutesTowardSatisfactionLoss");
            employee.Remove("restMinutesTowardSatisfactionGain");
            employee.Remove("shiftStartMinute");
            employee.Remove("shiftEndMinute");
            employee.Remove("isAlwaysOn");
        }

        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE game_save (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                schema_version INTEGER NOT NULL,
                saved_at_utc TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );
            CREATE TABLE desktop_window (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                left_px REAL NOT NULL,
                top_px REAL NOT NULL,
                is_locked INTEGER NOT NULL CHECK(is_locked IN (0, 1))
            );
            INSERT INTO game_save(slot, schema_version, saved_at_utc, payload_json)
            VALUES(1, 4, '2026-08-03T13:00:00.0000000+00:00', $payload);
            PRAGMA user_version = 4;
            """;
        command.Parameters.AddWithValue("$payload", payloadNode.ToJsonString());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateVersionFiveDatabaseAsync(string path)
    {
        var payloadNode = JsonSerializer.SerializeToNode(
            CreateSave(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        payloadNode["schemaVersion"] = 5;
        var business = payloadNode["business"]!.AsObject();
        business.Remove("promotions");
        foreach (var storeNode in business["stores"]!.AsArray())
        {
            var store = storeNode!.AsObject();
            store.Remove("operatingCostCents");
            store.Remove("development");
        }

        var businessSimulation = payloadNode["businessSimulation"]!.AsObject();
        foreach (var runtimeNode in businessSimulation["stores"]!.AsArray())
        {
            runtimeNode!.AsObject().Remove("dayStartOperatingCostCents");
        }

        await using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE game_save (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                schema_version INTEGER NOT NULL,
                saved_at_utc TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );
            CREATE TABLE desktop_window (
                slot INTEGER NOT NULL PRIMARY KEY CHECK(slot = 1),
                left_px REAL NOT NULL,
                top_px REAL NOT NULL,
                is_locked INTEGER NOT NULL CHECK(is_locked IN (0, 1))
            );
            INSERT INTO game_save(slot, schema_version, saved_at_utc, payload_json)
            VALUES(1, 5, '2026-08-03T13:00:00.0000000+00:00', $payload);
            PRAGMA user_version = 5;
            """;
        command.Parameters.AddWithValue("$payload", payloadNode.ToJsonString());
        await command.ExecuteNonQueryAsync();
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"hajimao-tests-{Guid.NewGuid():N}");

        public TemporaryDatabase()
        {
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "save.db");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}
