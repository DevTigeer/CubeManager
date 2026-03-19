using Microsoft.Data.Sqlite;
using Dapper;
using Serilog;

namespace CubeManager.Data;

public class Database
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public Database()
    {
        _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CubeManager", "cubemanager.db");

        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        _connectionString = $"Data Source={_dbPath}";
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        // FK는 per-connection 설정이므로 매 연결마다 활성화 필수
        conn.Execute("PRAGMA foreign_keys = ON");
        return conn;
    }

    public void Initialize()
    {
        // Dapper snake_case → PascalCase 자동 매핑
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        Log.Information("DB 초기화: {Path}", _dbPath);

        using var conn = CreateConnection();
        conn.Execute("PRAGMA journal_mode = WAL");
        conn.Execute("PRAGMA synchronous = NORMAL");
        conn.Execute("PRAGMA temp_store = MEMORY");
        conn.Execute("PRAGMA mmap_size = 67108864");
        conn.Execute("PRAGMA cache_size = -8000");
        conn.Execute("PRAGMA page_size = 4096");
        conn.Execute("PRAGMA foreign_keys = ON");

        Log.Information("DB PRAGMA 설정 완료");
    }

    public void Shutdown()
    {
        using var conn = CreateConnection();
        conn.Execute("PRAGMA wal_checkpoint(TRUNCATE)");
        conn.Execute("PRAGMA optimize");
        Log.Information("DB 종료 처리 완료");
    }

    public string DbPath => _dbPath;
}
