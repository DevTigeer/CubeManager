using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V018_FridayCloseChecklist : IMigration
{
    public int Version => 18;
    public string Description => "금요일 마감 체크리스트 추가 (비품/물통/스피커/건전지)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        // checklist_template_days 테이블 존재 여부로 V017 적용됐는지 확인
        var hasMapping = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='checklist_template_days'",
            transaction: tx) > 0;

        var maxOrder = conn.ExecuteScalar<int>(
            "SELECT COALESCE(MAX(sort_order), 0) FROM checklist_templates",
            transaction: tx);
        var order = maxOrder + 1;

        var items = new[]
        {
            "비품재고파악 및 부족분 확인 후 단톡에 고지",
            "장기밀매 물통 비우기 및 페트병 물받기",
            "스피커의 케이블정상작동 유무 파악 및 보조배터리 작동유무 파악",
            "건전지 부족시 사오기"
        };

        foreach (var task in items)
        {
            // 이미 동일 내용이 있으면 스킵
            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM checklist_templates WHERE task_text = @task",
                new { task }, tx);
            if (exists > 0) continue;

            if (hasMapping)
            {
                // V017 이후: templates + days 매핑 방식
                conn.Execute(
                    "INSERT INTO checklist_templates (task_text, sort_order, is_active, role, day_of_week) " +
                    "VALUES (@task, @order, 1, 'close', 0)",
                    new { task, order }, tx);
                var newId = conn.ExecuteScalar<int>("SELECT last_insert_rowid()", transaction: tx);
                conn.Execute(
                    "INSERT OR IGNORE INTO checklist_template_days (template_id, day_of_week) VALUES (@tid, 5)",
                    new { tid = newId }, tx); // 5 = 금요일
            }
            else
            {
                // V017 이전: 기존 방식
                conn.Execute(
                    "INSERT INTO checklist_templates (day_of_week, task_text, sort_order, is_active, role) " +
                    "VALUES (5, @task, @order, 1, 'close')",
                    new { task, order }, tx);
            }
            order++;
        }
    }
}
