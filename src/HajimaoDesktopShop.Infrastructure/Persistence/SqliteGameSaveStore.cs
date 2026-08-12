using System.Text.Json;
using HajimaoDesktopShop.Application.Persistence;
using Microsoft.Data.Sqlite;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

public sealed class SqliteGameSaveStore : IGameSaveStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _connectionString;

    static SqliteGameSaveStore()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public SqliteGameSaveStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        var fullPath = Path.GetFullPath(databasePath.Trim());
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString();
    }

    public async Task<GameSaveData?> LoadGameAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenMigratedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT schema_version, payload_json FROM game_save WHERE slot = 1;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            var schemaVersion = reader.GetInt32(0);
            EnsureSupportedVersion(schemaVersion);
            var save = JsonSerializer.Deserialize<GameSaveData>(reader.GetString(1), JsonOptions)
                ?? throw new InvalidDataException("SQLite save payload is empty or invalid.");
            EnsureSupportedVersion(save.SchemaVersion);
            if (save.SchemaVersion != schemaVersion)
            {
                throw new InvalidDataException("SQLite save schema metadata does not match its payload.");
            }

            return save;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveGameAsync(GameSaveData save, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(save);
        EnsureSupportedVersion(save.SchemaVersion);
        var payload = JsonSerializer.Serialize(save, JsonOptions);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenMigratedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO game_save(slot, schema_version, saved_at_utc, payload_json)
                VALUES(1, $schemaVersion, $savedAtUtc, $payload)
                ON CONFLICT(slot) DO UPDATE SET
                    schema_version = excluded.schema_version,
                    saved_at_utc = excluded.saved_at_utc,
                    payload_json = excluded.payload_json;
                """;
            command.Parameters.AddWithValue("$schemaVersion", save.SchemaVersion);
            command.Parameters.AddWithValue("$savedAtUtc", save.SavedAtUtc.ToString("O"));
            command.Parameters.AddWithValue("$payload", payload);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DesktopWindowPlacement?> LoadDesktopWindowPlacementAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenMigratedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT left_px, top_px, is_locked FROM desktop_window WHERE slot = 1;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                ? new DesktopWindowPlacement(reader.GetDouble(0), reader.GetDouble(1), reader.GetBoolean(2))
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveDesktopWindowPlacementAsync(
        DesktopWindowPlacement placement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(placement);
        if (!double.IsFinite(placement.Left) || !double.IsFinite(placement.Top))
        {
            throw new ArgumentOutOfRangeException(nameof(placement));
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenMigratedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO desktop_window(slot, left_px, top_px, is_locked)
                VALUES(1, $left, $top, $isLocked)
                ON CONFLICT(slot) DO UPDATE SET
                    left_px = excluded.left_px,
                    top_px = excluded.top_px,
                    is_locked = excluded.is_locked;
                """;
            command.Parameters.AddWithValue("$left", placement.Left);
            command.Parameters.AddWithValue("$top", placement.Top);
            command.Parameters.AddWithValue("$isLocked", placement.IsLocked);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SqliteConnection> OpenMigratedConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await using var busyTimeout = connection.CreateCommand();
            busyTimeout.CommandText = "PRAGMA busy_timeout = 5000;";
            await busyTimeout.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await using var versionCommand = connection.CreateCommand();
            versionCommand.CommandText = "PRAGMA user_version;";
            var version = Convert.ToInt32(
                await versionCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            if (version > GameSaveSchema.CurrentVersion)
            {
                throw new InvalidOperationException(
                    $"Save database version {version} is newer than supported version {GameSaveSchema.CurrentVersion}.");
            }

            if (version == 0)
            {
                await MigrateFromZeroToOneAsync(connection, cancellationToken).ConfigureAwait(false);
                version = 1;
            }

            if (version == 1)
            {
                await MigrateFromOneToTwoAsync(connection, cancellationToken).ConfigureAwait(false);
                version = 2;
            }

            if (version == 2)
            {
                await MigrateFromTwoToThreeAsync(connection, cancellationToken).ConfigureAwait(false);
                version = 3;
            }

            if (version == 3)
            {
                await MigrateFromThreeToFourAsync(connection, cancellationToken).ConfigureAwait(false);
                version = 4;
            }

            if (version == 4)
            {
                await MigrateFromFourToFiveAsync(connection, cancellationToken).ConfigureAwait(false);
                version = 5;
            }

            if (version == 5)
            {
                await MigrateFromFiveToSixAsync(connection, cancellationToken).ConfigureAwait(false);
                version = 6;
            }

            if (version == 6)
            {
                await MigrateFromSixToSevenAsync(connection, cancellationToken).ConfigureAwait(false);
            }

            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task MigrateFromZeroToOneAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.Transaction = (SqliteTransaction)transaction;
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
            PRAGMA user_version = 1;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MigrateFromOneToTwoAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        string? legacyPayload;
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = (SqliteTransaction)transaction;
            read.CommandText = "SELECT payload_json FROM game_save WHERE slot = 1;";
            legacyPayload = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }

        if (legacyPayload is not null)
        {
            var legacy = JsonSerializer.Deserialize<LegacyGameSaveV1>(legacyPayload, JsonOptions)
                ?? throw new InvalidDataException("SQLite v1 save payload is empty or invalid.");
            if (legacy.SchemaVersion != 1)
            {
                throw new InvalidDataException(
                    $"SQLite v1 migration expected payload schema 1, found {legacy.SchemaVersion}.");
            }

            var upgraded = legacy.UpgradeToV2();
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE game_save
                SET schema_version = $schemaVersion,
                    saved_at_utc = $savedAtUtc,
                    payload_json = $payload
                WHERE slot = 1;
                """;
            update.Parameters.AddWithValue("$schemaVersion", upgraded.SchemaVersion);
            update.Parameters.AddWithValue("$savedAtUtc", upgraded.SavedAtUtc.ToString("O"));
            update.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(upgraded, JsonOptions));
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var setVersion = connection.CreateCommand())
        {
            setVersion.Transaction = (SqliteTransaction)transaction;
            setVersion.CommandText = "PRAGMA user_version = 2;";
            await setVersion.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MigrateFromTwoToThreeAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        string? legacyPayload;
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = (SqliteTransaction)transaction;
            read.CommandText = "SELECT payload_json FROM game_save WHERE slot = 1;";
            legacyPayload = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }

        if (legacyPayload is not null)
        {
            var legacy = JsonSerializer.Deserialize<LegacyGameSaveV2>(legacyPayload, JsonOptions)
                ?? throw new InvalidDataException("SQLite v2 save payload is empty or invalid.");
            if (legacy.SchemaVersion != 2)
            {
                throw new InvalidDataException(
                    $"SQLite v2 migration expected payload schema 2, found {legacy.SchemaVersion}.");
            }

            var upgraded = legacy.UpgradeToV3();
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE game_save
                SET schema_version = $schemaVersion,
                    saved_at_utc = $savedAtUtc,
                    payload_json = $payload
                WHERE slot = 1;
                """;
            update.Parameters.AddWithValue("$schemaVersion", upgraded.SchemaVersion);
            update.Parameters.AddWithValue("$savedAtUtc", upgraded.SavedAtUtc.ToString("O"));
            update.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(upgraded, JsonOptions));
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var setVersion = connection.CreateCommand())
        {
            setVersion.Transaction = (SqliteTransaction)transaction;
            setVersion.CommandText = "PRAGMA user_version = 3;";
            await setVersion.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MigrateFromThreeToFourAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        string? legacyPayload;
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = (SqliteTransaction)transaction;
            read.CommandText = "SELECT payload_json FROM game_save WHERE slot = 1;";
            legacyPayload = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }

        if (legacyPayload is not null)
        {
            var legacy = JsonSerializer.Deserialize<LegacyGameSaveV3>(legacyPayload, JsonOptions)
                ?? throw new InvalidDataException("SQLite v3 save payload is empty or invalid.");
            if (legacy.SchemaVersion != 3)
            {
                throw new InvalidDataException(
                    $"SQLite v3 migration expected payload schema 3, found {legacy.SchemaVersion}.");
            }

            var upgraded = legacy.UpgradeToV4();
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE game_save
                SET schema_version = $schemaVersion,
                    saved_at_utc = $savedAtUtc,
                    payload_json = $payload
                WHERE slot = 1;
                """;
            update.Parameters.AddWithValue("$schemaVersion", upgraded.SchemaVersion);
            update.Parameters.AddWithValue("$savedAtUtc", upgraded.SavedAtUtc.ToString("O"));
            update.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(upgraded, JsonOptions));
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var setVersion = connection.CreateCommand())
        {
            setVersion.Transaction = (SqliteTransaction)transaction;
            setVersion.CommandText = "PRAGMA user_version = 4;";
            await setVersion.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MigrateFromFourToFiveAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        string? legacyPayload;
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = (SqliteTransaction)transaction;
            read.CommandText = "SELECT payload_json FROM game_save WHERE slot = 1;";
            legacyPayload = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }

        if (legacyPayload is not null)
        {
            var legacy = JsonSerializer.Deserialize<LegacyGameSaveV4>(legacyPayload, JsonOptions)
                ?? throw new InvalidDataException("SQLite v4 save payload is empty or invalid.");
            if (legacy.SchemaVersion != 4)
            {
                throw new InvalidDataException(
                    $"SQLite v4 migration expected payload schema 4, found {legacy.SchemaVersion}.");
            }

            var upgraded = legacy.UpgradeToV5();
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE game_save
                SET schema_version = $schemaVersion,
                    saved_at_utc = $savedAtUtc,
                    payload_json = $payload
                WHERE slot = 1;
                """;
            update.Parameters.AddWithValue("$schemaVersion", upgraded.SchemaVersion);
            update.Parameters.AddWithValue("$savedAtUtc", upgraded.SavedAtUtc.ToString("O"));
            update.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(upgraded, JsonOptions));
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var setVersion = connection.CreateCommand())
        {
            setVersion.Transaction = (SqliteTransaction)transaction;
            setVersion.CommandText = "PRAGMA user_version = 5;";
            await setVersion.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task MigrateFromFiveToSixAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        string? legacyPayload;
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = (SqliteTransaction)transaction;
            read.CommandText = "SELECT payload_json FROM game_save WHERE slot = 1;";
            legacyPayload = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }

        if (legacyPayload is not null)
        {
            var legacy = JsonSerializer.Deserialize<LegacyGameSaveV5>(legacyPayload, JsonOptions)
                ?? throw new InvalidDataException("SQLite v5 save payload is empty or invalid.");
            if (legacy.SchemaVersion != 5)
            {
                throw new InvalidDataException(
                    $"SQLite v5 migration expected payload schema 5, found {legacy.SchemaVersion}.");
            }

            var upgraded = legacy.UpgradeToV6();
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE game_save
                SET schema_version = $schemaVersion,
                    saved_at_utc = $savedAtUtc,
                    payload_json = $payload
                WHERE slot = 1;
                """;
            update.Parameters.AddWithValue("$schemaVersion", upgraded.SchemaVersion);
            update.Parameters.AddWithValue("$savedAtUtc", upgraded.SavedAtUtc.ToString("O"));
            update.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(upgraded, JsonOptions));
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var setVersion = connection.CreateCommand())
        {
            setVersion.Transaction = (SqliteTransaction)transaction;
            setVersion.CommandText = "PRAGMA user_version = 6;";
            await setVersion.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureSupportedVersion(int schemaVersion)
    {
        if (schemaVersion != GameSaveSchema.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"Save schema version {schemaVersion} is not supported; expected {GameSaveSchema.CurrentVersion}.");
        }
    }

    private static async Task MigrateFromSixToSevenAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        string? legacyPayload;
        await using (var read = connection.CreateCommand())
        {
            read.Transaction = (SqliteTransaction)transaction;
            read.CommandText = "SELECT payload_json FROM game_save WHERE slot = 1;";
            legacyPayload = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }

        if (legacyPayload is not null)
        {
            var legacy = JsonSerializer.Deserialize<LegacyGameSaveV6>(legacyPayload, JsonOptions)
                ?? throw new InvalidDataException("SQLite v6 save payload is empty or invalid.");
            if (legacy.SchemaVersion != 6)
            {
                throw new InvalidDataException(
                    $"SQLite v6 migration expected payload schema 6, found {legacy.SchemaVersion}.");
            }

            var upgraded = legacy.UpgradeToV7();
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE game_save
                SET schema_version = $schemaVersion,
                    saved_at_utc = $savedAtUtc,
                    payload_json = $payload
                WHERE slot = 1;
                """;
            update.Parameters.AddWithValue("$schemaVersion", upgraded.SchemaVersion);
            update.Parameters.AddWithValue("$savedAtUtc", upgraded.SavedAtUtc.ToString("O"));
            update.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(upgraded, JsonOptions));
            await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await using (var setVersion = connection.CreateCommand())
        {
            setVersion.Transaction = (SqliteTransaction)transaction;
            setVersion.CommandText = "PRAGMA user_version = 7;";
            await setVersion.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
