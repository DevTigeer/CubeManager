using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IChecklistRepository
{
    // Templates (관리자용)
    Task<IEnumerable<ChecklistTemplate>> GetAllTemplatesAsync();
    Task<IEnumerable<ChecklistTemplate>> GetTemplatesByDayAsync(int dayOfWeek);
    Task<int> InsertTemplateAsync(ChecklistTemplate template);
    Task UpdateTemplateAsync(ChecklistTemplate template);
    Task DeleteTemplateAsync(int id);

    // Records (체크 기록)
    Task<IEnumerable<ChecklistRecord>> GetRecordsForDateAsync(string date);
    Task UpsertRecordAsync(int templateId, string date, bool isChecked, string? checkedBy);
}
