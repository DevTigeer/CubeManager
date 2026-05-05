namespace CubeManager.Telegram.Commands;

/// <summary>
/// 점주 DM에서만 응답할 명령. 그룹 chat에서는 거부.
/// 라우터가 OwnerChatId와 비교하여 권한 체크.
/// </summary>
public interface IOwnerOnlyCommand
{
}
