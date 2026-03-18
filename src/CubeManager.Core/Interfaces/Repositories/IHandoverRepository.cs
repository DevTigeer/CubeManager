using CubeManager.Core.Models;

namespace CubeManager.Core.Interfaces.Repositories;

public interface IHandoverRepository
{
    Task<(IEnumerable<Handover> items, int total)> GetPagedAsync(int page, int pageSize, string? keyword = null);
    Task<IEnumerable<HandoverComment>> GetCommentsAsync(int handoverId);
    Task<int> InsertHandoverAsync(string authorName, string content);
    Task<bool> DeleteHandoverAsync(int id);
    Task<int> InsertCommentAsync(int handoverId, string authorName, string content, int? parentCommentId = null);
    Task<bool> DeleteCommentAsync(int id);
}
