namespace CubeManager.Core.Models;

public class Handover
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<HandoverComment> Comments { get; set; } = [];
}

public class HandoverComment
{
    public int Id { get; set; }
    public int HandoverId { get; set; }
    public int? ParentCommentId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<HandoverComment> Replies { get; set; } = [];
}
