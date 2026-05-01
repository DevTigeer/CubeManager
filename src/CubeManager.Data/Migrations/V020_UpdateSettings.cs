using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V020_UpdateSettings : IMigration
{
    public int Version => 20;
    public string Description => "앱 업데이트 설정 기본값 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            INSERT OR IGNORE INTO app_config (key, value, updated_at) VALUES
              ('update_check_enabled', '1', datetime('now','localtime')),
              ('update_manifest_url', 'https://github.com/DevTigeer/CubeManager/releases/latest/download/update.json', datetime('now','localtime')),
              ('update_last_check_at', '', datetime('now','localtime'))
            """, transaction: tx);
    }
}
