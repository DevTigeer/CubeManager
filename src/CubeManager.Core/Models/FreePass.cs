namespace CubeManager.Core.Models;

/// <summary>
/// 무료이용권. 월최고기록/장치보상/기타 사유로 발급.
/// 번호: A2000부터 순차 증가, 사용 시 used_date 자동 기록.
/// </summary>
public class FreePass
{
    public int Id { get; set; }
    public string PassNumber { get; set; } = "";       // "A2000", "A2001"...
    public string CustomerName { get; set; } = "";      // 이름
    public int Headcount { get; set; } = 1;             // 인원수
    public string? Phone { get; set; }                  // 전화번호
    public string Reason { get; set; } = "record";      // "record"=월최고기록, "device"=장치보상, "other"=기타
    public string? Note { get; set; }                   // 비고
    public string IssuedDate { get; set; } = "";        // 발급일 YYYY-MM-DD
    public string? UsedDate { get; set; }               // 사용일 YYYY-MM-DD (null=미사용)
    public bool IsUsed { get; set; }                    // 사용 여부
}
