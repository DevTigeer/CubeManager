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

    /// <summary>
    /// DB 파일을 백업 폴더에 복사.
    /// SQLite VACUUM INTO로 안전하게 일관된 상태의 복사본 생성.
    /// </summary>
    public async Task<string> BackupAsync(string? customPath = null)
    {
        var backupDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CubeManager", "backups");
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = customPath ?? Path.Combine(backupDir, $"cubemanager_{timestamp}.db");

        // WAL 체크포인트 후 안전한 복사
        using var conn = CreateConnection();
        await conn.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        await conn.ExecuteAsync($"VACUUM INTO @path", new { path = backupPath });

        // 오래된 백업 정리 (최근 14개만 유지)
        CleanOldBackups(backupDir, 14);

        Log.Information("DB 백업 완료: {Path}", backupPath);
        return backupPath;
    }

    private static void CleanOldBackups(string dir, int keepCount)
    {
        try
        {
            var files = Directory.GetFiles(dir, "cubemanager_*.db")
                .OrderByDescending(f => f)
                .Skip(keepCount)
                .ToList();

            foreach (var f in files)
            {
                File.Delete(f);
                Log.Debug("오래된 백업 삭제: {File}", Path.GetFileName(f));
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "오래된 백업 정리 실패");
        }
    }
}
