namespace CubeManager.Telegram.Commands;

public interface ICommandHandler
{
    /// <summary>슬래시 없는 영문 소문자 명령어 (예: "ping", "today").</summary>
    string Command { get; }

    /// <summary>한글 별칭들. 비어있을 수 있음 (예: ["오늘매출"]).</summary>
    IReadOnlyList<string> Aliases => Array.Empty<string>();

    /// <summary>/help 자동 생성에 사용되는 1줄 설명.</summary>
    string Description { get; }

    Task HandleAsync(CommandContext ctx);
}
