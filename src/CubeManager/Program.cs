using Microsoft.Extensions.DependencyInjection;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Services;
using CubeManager.Data;
using CubeManager.Data.Migrations;
using CubeManager.Data.Repositories;
using CubeManager.Dialogs;
using Serilog;

namespace CubeManager;

static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Serilog 설정
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CubeManager", "logs", "cube-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("=== CubeManager 시작 ===");

        try
        {
            // DI 컨테이너
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // DB 초기화 + 마이그레이션
            var db = ServiceProvider.GetRequiredService<Database>();
            db.Initialize();
            var migrator = ServiceProvider.GetRequiredService<MigrationRunner>();
            migrator.RunAll();

            // 최초 실행: 관리자 비밀번호 설정
            EnsureAdminPassword();

            // 최초 실행: 웹 자격증명 설정 (cubeescape.co.kr)
            EnsureWebCredentials();

            // 메인 폼 실행
            Application.Run(new MainForm(ServiceProvider));
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "앱 시작 실패");
            MessageBox.Show($"앱 시작 중 오류가 발생했습니다.\n{ex.Message}",
                "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.Information("=== CubeManager 종료 ===");
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<Database>();
        services.AddSingleton<MigrationRunner>();

        // Repositories
        services.AddSingleton<IConfigRepository, ConfigRepository>();
        services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<IHolidayRepository, HolidayRepository>();
        services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
        services.AddSingleton<ISalesRepository, SalesRepository>();
        services.AddSingleton<ISalaryRepository, SalaryRepository>();
        services.AddSingleton<IHandoverRepository, HandoverRepository>();
        services.AddSingleton<IInventoryRepository, InventoryRepository>();

        // Services
        services.AddSingleton<IEmployeeService, EmployeeService>();
        services.AddSingleton<IScheduleService, ScheduleService>();
        services.AddSingleton<IAttendanceService, AttendanceService>();
        services.AddSingleton<ISalesService, SalesService>();
        services.AddSingleton<ISalaryService, SalaryService>();
        services.AddSingleton<IReservationScraperService, ReservationScraperService>();
    }

    private static void EnsureAdminPassword()
    {
        var configRepo = ServiceProvider.GetRequiredService<IConfigRepository>();
        var hash = configRepo.GetAsync("admin_password_hash").GetAwaiter().GetResult();

        if (!string.IsNullOrEmpty(hash))
            return;

        Log.Information("최초 실행: 관리자 비밀번호 설정 필요");

        using var dialog = new AdminPasswordSetupDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            configRepo.SetAsync("admin_password_hash", dialog.PasswordHash)
                .GetAwaiter().GetResult();
            Log.Information("관리자 비밀번호 설정 완료");
        }
        else
        {
            Log.Warning("관리자 비밀번호 미설정 - 앱 종료");
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// 웹 자격증명(cubeescape.co.kr)이 미설정이면 입력 다이얼로그 표시.
    /// 건너뛰기 가능 — 이후 설정 탭에서 입력 가능.
    /// </summary>
    private static void EnsureWebCredentials()
    {
        var configRepo = ServiceProvider.GetRequiredService<IConfigRepository>();
        var encId = configRepo.GetAsync("web_login_id").GetAwaiter().GetResult();

        // 이미 설정되어 있으면 건너뜀
        if (!string.IsNullOrEmpty(encId))
        {
            var decrypted = CredentialHelper.Decrypt(encId);
            if (!string.IsNullOrEmpty(decrypted))
                return;
        }

        Log.Information("최초 실행: 웹 자격증명 설정 필요");

        var scraperService = ServiceProvider.GetRequiredService<IReservationScraperService>();
        using var dialog = new WebCredentialSetupDialog(scraperService);

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            configRepo.SetAsync("web_login_id", CredentialHelper.Encrypt(dialog.WebId))
                .GetAwaiter().GetResult();
            configRepo.SetAsync("web_login_pw", CredentialHelper.Encrypt(dialog.WebPw))
                .GetAwaiter().GetResult();
            Log.Information("웹 자격증명 설정 완료");
        }
        else
        {
            Log.Information("웹 자격증명 건너뛰기 - 설정 탭에서 나중에 설정 가능");
        }
    }
}
