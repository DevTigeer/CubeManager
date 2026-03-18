using Dapper;
using Serilog;

namespace CubeManager.Data.Migrations;

public class MigrationRunner
{
    private readonly Database _db;
    private readonly List<IMigration> _migrations;

    public MigrationRunner(Database db)
    {
        _db = db;
        _migrations =
        [
            new V001_InitBase(),
            new V002_Schedule(),
            new V003_Attendance(),
            new V004_ReservationSales(),
            new V005_Salary(),
            new V006_HandoverInventory()
        ];
    }

    public void RunAll()
    {
        using var conn = _db.CreateConnection();

        conn.Execute("""
            CREATE TABLE IF NOT EXISTS schema_version (
                version INTEGER PRIMARY KEY,
                description TEXT NOT NULL,
                applied_at TEXT NOT NULL DEFAULT (datetime('now', 'localtime'))
            )
            """);

        var applied = conn.Query<int>("SELECT version FROM schema_version")
            .ToHashSet();

        foreach (var migration in _migrations.OrderBy(m => m.Version))
        {
            if (applied.Contains(migration.Version))
                continue;

            Log.Information("마이그레이션 실행: V{Version} - {Desc}",
                migration.Version.ToString("D3"), migration.Description);

            using var tx = conn.BeginTransaction();
            try
            {
                migration.Execute(conn, tx);

                conn.Execute(
                    "INSERT INTO schema_version (version, description) VALUES (@Version, @Description)",
                    new { migration.Version, migration.Description },
                    tx);

                tx.Commit();
                Log.Information("마이그레이션 완료: V{Version}", migration.Version.ToString("D3"));
            }
            catch (Exception ex)
            {
                tx.Rollback();
                Log.Error(ex, "마이그레이션 실패: V{Version}", migration.Version.ToString("D3"));
                throw;
            }
        }
    }
}

public interface IMigration
{
    int Version { get; }
    string Description { get; }
    void Execute(System.Data.IDbConnection conn, System.Data.IDbTransaction tx);
}
