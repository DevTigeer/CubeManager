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
            "FROM checklist_templates ORDER BY day_of_week, role, sort_order");
    }

    public async Task<IEnumerable<ChecklistTemplate>> GetTemplatesByDayAsync(int dayOfWeek)
    {
        using var conn = _db.CreateConnection();
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
            "VALUES (@DayOfWeek, @Role, @TaskText, @SortOrder, @IsActive); SELECT last_insert_rowid()", template);
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
        await conn.ExecuteAsync("DELETE FROM checklist_templates WHERE id=@id", new { id });
    }

    public async Task<IEnumerable<ChecklistRecord>> GetRecordsForDateAsync(string date)
    {
        using var conn = _db.CreateConnection();
        var dayOfWeek = (int)DateTime.Parse(date).DayOfWeek;
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
