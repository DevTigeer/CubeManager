using System.Runtime.Versioning;
using CubeManager.Telegram.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace CubeManager.Telegram;

[SupportedOSPlatform("windows")]
public static class TelegramServiceCollectionExtensions
{
    /// <summary>
    /// 텔레그램 봇 인프라 + 기본 명령(ping, help) 등록.
    /// 추가 명령은 호출자가 별도로 AddSingleton&lt;ICommandHandler, XxxHandler&gt;() 등록.
    /// </summary>
    public static IServiceCollection AddCubeManagerTelegram(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramBotConfigService, TelegramBotConfigService>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<ITelegramBotWorker, TelegramBotWorker>();

        // 기본 명령
        services.AddSingleton<ICommandHandler, PingCommandHandler>();
        services.AddSingleton<ICommandHandler, HelpCommandHandler>();

        return services;
    }
}
