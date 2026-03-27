using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V017_ChecklistDayMapping : IMigration
{
    public int Version => 17;
    public string Description => "체크리스트 템플릿-요일 매핑 분리 (중복 제거)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        // 1) 요일 매핑 테이블 생성
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS checklist_template_days (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                template_id INTEGER NOT NULL,
                day_of_week INTEGER NOT NULL,
                UNIQUE(template_id, day_of_week)
            )
        """, transaction: tx);

        // 2) 기존 데이터 읽기
        var existing = conn.Query<(int Id, int DayOfWeek, string TaskText, int SortOrder, int IsActive, string Role)>(
            "SELECT id AS Id, day_of_week AS DayOfWeek, task_text AS TaskText, " +
            "sort_order AS SortOrder, is_active AS IsActive, role AS Role " +
            "FROM checklist_templates ORDER BY sort_order",
            transaction: tx).ToList();

        if (existing.Count == 0) return;

        // 고유 task+role 그룹
        var groups = existing
            .GroupBy(e => new { e.TaskText, e.Role })
            .ToList();

        // 3) records 먼저 삭제 (FK 참조하므로), 그 다음 templates 삭제
        conn.Execute("DELETE FROM checklist_records", transaction: tx);
        conn.Execute("DELETE FROM checklist_templates", transaction: tx);

        // 4) 고유 task만 INSERT + days 매핑
        var order = 1;
        foreach (var group in groups)
        {
            var first = group.First();
            conn.Execute(
                "INSERT INTO checklist_templates (task_text, sort_order, is_active, role, day_of_week) " +
                "VALUES (@task, @order, @active, @role, 0)",
                new { task = group.Key.TaskText, order, active = first.IsActive, role = group.Key.Role },
                tx);
            var newId = conn.ExecuteScalar<int>("SELECT last_insert_rowid()", transaction: tx);

            foreach (var item in group)
            {
                conn.Execute(
                    "INSERT OR IGNORE INTO checklist_template_days (template_id, day_of_week) " +
                    "VALUES (@tid, @dow)",
                    new { tid = newId, dow = item.DayOfWeek },
                    tx);
            }
            order++;
        }
    }
}
