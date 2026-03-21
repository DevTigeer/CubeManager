using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V011_ChecklistRole : IMigration
{
    public int Version => 11;
    public string Description => "체크리스트 템플릿에 역할(role) 컬럼 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute(
            "ALTER TABLE checklist_templates ADD COLUMN role TEXT NOT NULL DEFAULT 'all'",
            transaction: tx);
    }
}
