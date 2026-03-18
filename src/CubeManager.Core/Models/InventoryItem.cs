namespace CubeManager.Core.Models;

public class InventoryItem
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int RequiredQty { get; set; }
    public int CurrentQty { get; set; }
    public int ShortageQty => RequiredQty - CurrentQty;
    public string? Category { get; set; }
    public string? Note { get; set; }
    public DateTime UpdatedAt { get; set; }
}
