using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using System.Text.Json;

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
        Assert.Equal(3, migrated.SchemaVersion);
        Assert.Equal(88, migrated.Simulation.GameMinute);
        Assert.Equal(7, Assert.Single(migrated.Shop.Products).Quantity);

        await using var connection = new SqliteConnection($"Data Source={database.Path};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT schema_version, payload_json FROM game_save WHERE slot = 1;";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(3, reader.GetInt32(0));
        Assert.DoesNotContain("speed", reader.GetString(1), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VersionTwoSave_MigratesToSchemaThreeWithoutLosingLegacyState()
    {
        using var database = new TemporaryDatabase();
        await CreateVersionTwoDatabaseAsync(database.Path);

        var migrated = await new SqliteGameSaveStore(database.Path).LoadGameAsync();

        Assert.NotNull(migrated);
        Assert.Equal(3, migrated.SchemaVersion);
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
        Assert.Equal(3, reader.GetInt32(0));
        Assert.Contains("\"schemaVersion\":3", reader.GetString(1), StringComparison.Ordinal);
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
                null));

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
