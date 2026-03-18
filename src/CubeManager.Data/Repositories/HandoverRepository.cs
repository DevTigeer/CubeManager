using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Models;
using Dapper;

namespace CubeManager.Data.Repositories;

public class HandoverRepository : IHandoverRepository
{
    private readonly Database _db;
    public HandoverRepository(Database db) => _db = db;

    public async Task<(IEnumerable<Handover> items, int total)> GetPagedAsync(int page, int pageSize, string? keyword = null)
    {
        using var conn = _db.CreateConnection();
        var where = string.IsNullOrEmpty(keyword) ? "" : "WHERE author_name LIKE @kw OR content LIKE @kw";
        var kw = $"%{keyword}%";

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM handovers {where}", new { kw });

        var items = await conn.QueryAsync<Handover>(
            $"SELECT id, author_name, content, created_at, updated_at FROM handovers {where} " +
            "ORDER BY created_at DESC LIMIT @limit OFFSET @offset",
            new { kw, limit = pageSize, offset = (page - 1) * pageSize });

        return (items, total);
    }

    public async Task<IEnumerable<HandoverComment>> GetCommentsAsync(int handoverId)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryAsync<HandoverComment>(
            "SELECT id, handover_id, parent_comment_id, author_name, content, created_at " +
            "FROM handover_comments WHERE handover_id = @handoverId ORDER BY created_at",
            new { handoverId });
    }

    public async Task<int> InsertHandoverAsync(string authorName, string content)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO handovers (author_name, content) VALUES (@authorName, @content); " +
            "SELECT last_insert_rowid()", new { authorName, content });
    }

    public async Task<bool> DeleteHandoverAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM handovers WHERE id = @id", new { id }) > 0;
    }

    public async Task<int> InsertCommentAsync(int handoverId, string authorName, string content, int? parentCommentId = null)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO handover_comments (handover_id, author_name, content, parent_comment_id) " +
            "VALUES (@handoverId, @authorName, @content, @parentCommentId); SELECT last_insert_rowid()",
            new { handoverId, authorName, content, parentCommentId });
    }

    public async Task<bool> DeleteCommentAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM handover_comments WHERE id = @id", new { id }) > 0;
    }
}
