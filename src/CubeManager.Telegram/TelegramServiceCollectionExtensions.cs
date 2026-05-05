using System.Runtime.Versioning;
using CubeManager.Telegram.Commands;
using CubeManager.Telegram.Commands.Reports;
using CubeManager.Telegram.Imaging;
using Microsoft.Extensions.DependencyInjection;

namespace CubeManager.Telegram;

[SupportedOSPlatform("windows")]
public static class TelegramServiceCollectionExtensions
{
    /// <summary>
    /// 텔레그램 봇 인프라 + 모든 명령 등록.
    /// </summary>
    public static IServiceCollection AddCubeManagerTelegram(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramBotConfigService, TelegramBotConfigService>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<ITelegramBotWorker, TelegramBotWorker>();
        services.AddSingleton<IBotImageRenderer, BotImageRenderer>();

        // 기본 명령
        services.AddSingleton<ICommandHandler, PingCommandHandler>();
        services.AddSingleton<ICommandHandler, HelpCommandHandler>();

        // 리포트 명령 (이미지 응답)
        services.AddSingleton<ICommandHandler, SalesCommandHandler>();
        services.AddSingleton<ICommandHandler, MonthCommandHandler>();
        services.AddSingleton<ICommandHandler, AttendanceCommandHandler>();
        services.AddSingleton<ICommandHandler, ChecklistCommandHandler>();
        services.AddSingleton<ICommandHandler, ScheduleCommandHandler>();
        services.AddSingleton<ICommandHandler, DashboardCommandHandler>();

        // 관리자 전용 (점주 DM에서만)
        services.AddSingleton<ICommandHandler, SalaryCommandHandler>();
        services.AddSingleton<ICommandHandler, AttendanceAdminCommandHandler>();

        return services;
    }
}
