using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V019_SundayChecklist : IMigration
{
    public int Version => 19;
    public string Description => "일요일 체크리스트 추가 (미들 인터폰 + 마감 테마별 장치점검)";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        var hasMapping = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='checklist_template_days'",
            transaction: tx) > 0;

        var maxOrder = conn.ExecuteScalar<int>(
            "SELECT COALESCE(MAX(sort_order), 0) FROM checklist_templates",
            transaction: tx);
        var order = maxOrder + 1;

        var items = new (string role, string task)[]
        {
            ("middle1", "손님 퇴장 후 10분안에 미들과 인터폰 점검"),
            ("close", "타워링: 캐비넷 버튼 잘되는지 + 라디오 소리 잘들리는지 + 버튼누르는곳 달랑거리면 나사풀고 다시 조이기"),
            ("close", "장기밀매: xray기계 안떨어졌는지, 컵 잘작동하는지, 선빠지지않았는지 확인"),
            ("close", "타이타닉: 2층올라가는 이엠락 빠지려고하는지, 선빠지려는지 확인"),
            ("close", "신데: 땔감확인"),
            ("close", "집착: 두번째방천장 경첩 빠지려는지확인")
        };

        foreach (var (role, task) in items)
        {
            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM checklist_templates WHERE task_text = @task",
                new { task }, tx);
            if (exists > 0) continue;

            if (hasMapping)
            {
                conn.Execute(
                    "INSERT INTO checklist_templates (task_text, sort_order, is_active, role, day_of_week) " +
                    "VALUES (@task, @order, 1, @role, 0)",
                    new { task, order, role }, tx);
                var newId = conn.ExecuteScalar<int>("SELECT last_insert_rowid()", transaction: tx);
                conn.Execute(
                    "INSERT OR IGNORE INTO checklist_template_days (template_id, day_of_week) VALUES (@tid, 0)",
                    new { tid = newId }, tx); // 0 = 일요일
            }
            else
            {
                conn.Execute(
                    "INSERT INTO checklist_templates (day_of_week, task_text, sort_order, is_active, role) " +
                    "VALUES (0, @task, @order, 1, @role)",
                    new { task, order, role }, tx);
            }
            order++;
        }
    }
}
