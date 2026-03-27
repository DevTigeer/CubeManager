using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class ChecklistRepository : IChecklistRepository
{
    private readonly Database _db;
    public ChecklistRepository(Database db) => _db = db;

    public async Task<IEnumerable<ChecklistTemplate>> GetAllTemplatesAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<ChecklistTemplate>(
            "SELECT id, day_of_week, role, task_text, sort_order, is_active " +
            "FROM checklist_templates ORDER BY role, sort_order");
    }

    public async Task<IEnumerable<ChecklistTemplate>> GetTemplatesByDayAsync(int dayOfWeek)
    {
        using var conn = _db.CreateConnection();
        // V017+: checklist_template_days 테이블이 있으면 JOIN 사용
        var hasDaysTable = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='checklist_template_days'");

        if (hasDaysTable > 0)
        {
            return await conn.QueryAsync<ChecklistTemplate>(
                "SELECT DISTINCT t.id, 0 AS day_of_week, t.role, t.task_text, t.sort_order, t.is_active " +
                "FROM checklist_templates t " +
                "INNER JOIN checklist_template_days d ON t.id = d.template_id " +
                "WHERE d.day_of_week = @dayOfWeek AND t.is_active = 1 " +
                "ORDER BY t.role, t.sort_order",
                new { dayOfWeek });
        }

        // 폴백: 기존 day_of_week 컬럼 사용
        return await conn.QueryAsync<ChecklistTemplate>(
            "SELECT id, day_of_week, role, task_text, sort_order, is_active " +
            "FROM checklist_templates WHERE day_of_week=@dayOfWeek AND is_active=1 ORDER BY role, sort_order",
            new { dayOfWeek });
    }

    public async Task<int> InsertTemplateAsync(ChecklistTemplate template)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO checklist_templates (day_of_week, role, task_text, sort_order, is_active) " +
            "VALUES (0, @Role, @TaskText, @SortOrder, @IsActive); SELECT last_insert_rowid()", template);
    }

    public async Task UpdateTemplateAsync(ChecklistTemplate template)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE checklist_templates SET task_text=@TaskText, role=@Role, sort_order=@SortOrder, " +
            "is_active=@IsActive WHERE id=@Id", template);
    }

    public async Task DeleteTemplateAsync(int id)
    {
        using var conn = _db.CreateConnection();
        // 매핑도 CASCADE로 삭제됨
        await conn.ExecuteAsync("DELETE FROM checklist_templates WHERE id=@id", new { id });
    }

    // ═══ Template-Day 매핑 (V017+) ═══

    public async Task<IEnumerable<int>> GetDaysForTemplateAsync(int templateId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<int>(
            "SELECT day_of_week FROM checklist_template_days WHERE template_id = @templateId ORDER BY day_of_week",
            new { templateId });
    }

    public async Task SetDaysForTemplateAsync(int templateId, IEnumerable<int> days)
    {
        using var conn = _db.CreateConnection();
        using var tx = conn.BeginTransaction();
        try
        {
            // 기존 매핑 삭제
            await conn.ExecuteAsync(
                "DELETE FROM checklist_template_days WHERE template_id = @templateId",
                new { templateId }, tx);

            // 새 매핑 삽입
            foreach (var day in days)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO checklist_template_days (template_id, day_of_week) VALUES (@templateId, @day)",
                    new { templateId, day }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ═══ Records (체크 기록) ═══

    public async Task<IEnumerable<ChecklistRecord>> GetRecordsForDateAsync(string date)
    {
        using var conn = _db.CreateConnection();
        var dayOfWeek = (int)DateTime.Parse(date).DayOfWeek;

        var hasDaysTable = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='checklist_template_days'");

        if (hasDaysTable > 0)
        {
            return await conn.QueryAsync<ChecklistRecord>(
                "SELECT t.id AS template_id, t.task_text, t.role, t.sort_order, " +
                "COALESCE(r.is_checked, 0) AS is_checked, r.checked_by, r.checked_at, " +
                "r.check_date, r.id " +
                "FROM checklist_templates t " +
                "INNER JOIN checklist_template_days d ON t.id = d.template_id " +
                "LEFT JOIN checklist_records r ON t.id = r.template_id AND r.check_date = @date " +
                "WHERE d.day_of_week = @dayOfWeek AND t.is_active = 1 " +
                "ORDER BY t.role, t.sort_order",
                new { date, dayOfWeek });
        }

        // 폴백
        return await conn.QueryAsync<ChecklistRecord>(
            "SELECT t.id AS template_id, t.task_text, t.role, t.sort_order, " +
            "COALESCE(r.is_checked, 0) AS is_checked, r.checked_by, r.checked_at, " +
            "r.check_date, r.id " +
            "FROM checklist_templates t " +
            "LEFT JOIN checklist_records r ON t.id = r.template_id AND r.check_date = @date " +
            "WHERE t.day_of_week = @dayOfWeek AND t.is_active = 1 " +
            "ORDER BY t.role, t.sort_order",
            new { date, dayOfWeek });
    }

    public async Task UpsertRecordAsync(int templateId, string date, bool isChecked, string? checkedBy)
    {
        using var conn = _db.CreateConnection();
        var now = DateTime.Now.ToString("HH:mm");
        await conn.ExecuteAsync(
            "INSERT INTO checklist_records (template_id, check_date, is_checked, checked_by, checked_at) " +
            "VALUES (@templateId, @date, @isChecked, @checkedBy, @now) " +
            "ON CONFLICT(template_id, check_date) DO UPDATE SET " +
            "is_checked=@isChecked, checked_by=@checkedBy, checked_at=@now",
            new { templateId, date, isChecked = isChecked ? 1 : 0, checkedBy, now });
    }
}
