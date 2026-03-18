# DI 컨테이너 설정

## 1. 라이브러리

```
Microsoft.Extensions.DependencyInjection (NuGet)
→ CubeManager 앱 프로젝트에만 설치
```

---

## 2. 서비스 수명(Lifetime)

| 수명 | 대상 | 이유 |
|------|------|------|
| **Singleton** | `Database` | DB 연결 팩토리는 앱 전체에서 1개 |
| **Singleton** | 모든 `Service` | 상태 없음(stateless), 탭 간 공유 |
| **Singleton** | 모든 `Repository` | 상태 없음, Service와 동일 수명 |
| **Transient** | 각 탭 `UserControl` | 지연 로딩 시 매번 새로 생성 |
| **Transient** | 각 `Dialog` | 열 때마다 새로 생성 |

> Service/Repository는 상태를 갖지 않는다 (DB 연결은 메서드 내에서 `using`으로 생성/해제).
> 따라서 Singleton으로 등록해도 스레드 안전 문제 없음.

---

## 3. Program.cs 등록 코드

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;

namespace CubeManager;

static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // DB 초기화 + 마이그레이션
        var db = ServiceProvider.GetRequiredService<Database>();
        db.Initialize();
        var migrator = ServiceProvider.GetRequiredService<MigrationRunner>();
        migrator.RunAll();

        Application.Run(ServiceProvider.GetRequiredService<MainForm>());
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // --- Infrastructure ---
        services.AddSingleton<Database>();
        services.AddSingleton<MigrationRunner>();

        // --- Repositories ---
        services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
        services.AddSingleton<IScheduleRepository, ScheduleRepository>();
        services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
        services.AddSingleton<IReservationRepository, ReservationRepository>();
        services.AddSingleton<ISalesRepository, SalesRepository>();
        services.AddSingleton<ISalaryRepository, SalaryRepository>();
        services.AddSingleton<IHolidayRepository, HolidayRepository>();
        services.AddSingleton<IHandoverRepository, HandoverRepository>();
        services.AddSingleton<IInventoryRepository, InventoryRepository>();
        services.AddSingleton<IConfigRepository, ConfigRepository>();

        // --- Services ---
        services.AddSingleton<IEmployeeService, EmployeeService>();
        services.AddSingleton<IScheduleService, ScheduleService>();
        services.AddSingleton<IAttendanceService, AttendanceService>();
        services.AddSingleton<IReservationScraperService, ReservationScraperService>();
        services.AddSingleton<ISalesService, SalesService>();
        services.AddSingleton<ISalaryService, SalaryService>();
        services.AddSingleton<IHolidayService, HolidayService>();
        services.AddSingleton<IHandoverService, HandoverService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IDocumentService, DocumentService>();

        // --- UI ---
        services.AddTransient<MainForm>();
    }
}
```

---

## 4. UI에서 Service 사용 패턴

### 4.1 MainForm → 탭 생성 시

```csharp
// MainForm.cs
public partial class MainForm : Form
{
    private readonly IServiceProvider _sp;

    public MainForm(IServiceProvider serviceProvider)
    {
        _sp = serviceProvider;
        InitializeComponent();
    }

    // 탭 지연 로딩 시 Service 주입
    private UserControl CreateTab(int index) => index switch
    {
        0 => new ReservationSalesTab(
                _sp.GetRequiredService<IReservationScraperService>(),
                _sp.GetRequiredService<ISalesService>()),
        1 => new ScheduleTab(
                _sp.GetRequiredService<IScheduleService>(),
                _sp.GetRequiredService<IEmployeeService>()),
        // ... 나머지 탭
        _ => throw new ArgumentOutOfRangeException()
    };
}
```

### 4.2 탭 UserControl 생성자

```csharp
// Forms/ScheduleTab.cs
public partial class ScheduleTab : UserControl
{
    private readonly IScheduleService _scheduleService;
    private readonly IEmployeeService _employeeService;

    public ScheduleTab(
        IScheduleService scheduleService,
        IEmployeeService employeeService)
    {
        _scheduleService = scheduleService;
        _employeeService = employeeService;
        InitializeComponent();
    }
}
```

> UI 컨트롤은 **생성자 주입**으로 필요한 Service만 받는다.
> `IServiceProvider`를 직접 전달하지 않는다 (Service Locator 안티패턴 방지).
> 예외: MainForm만 `IServiceProvider`를 받아 탭 생성 시 사용.

---

## 5. 순환 의존 방지

```
허용:
  SalaryService → IScheduleRepository, IHolidayRepository, IEmployeeRepository
  AttendanceService → IScheduleRepository

금지:
  SalaryService → IScheduleService (Service 간 직접 참조 금지)
  ScheduleService → IAttendanceService (순환)

이유:
  Service → Repository만 의존하면 순환이 원천 불가능.
  Service 간 데이터 공유가 필요하면 같은 Repository를 각각 주입받아 사용.
```

---

## 6. Database.cs 연결 패턴

```csharp
// Data/Database.cs
public class Database
{
    private readonly string _connectionString;

    public Database()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CubeManager", "cubemanager.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        // PRAGMA는 Initialize()에서 1회만 실행
        return conn;
    }

    public void Initialize()
    {
        using var conn = CreateConnection();
        conn.Execute("PRAGMA journal_mode = WAL");
        conn.Execute("PRAGMA synchronous = NORMAL");
        conn.Execute("PRAGMA temp_store = MEMORY");
        conn.Execute("PRAGMA mmap_size = 67108864");
        conn.Execute("PRAGMA cache_size = -8000");
        conn.Execute("PRAGMA foreign_keys = ON");
    }
}
```

> 연결 풀링: Microsoft.Data.Sqlite는 기본적으로 연결 풀링을 지원한다.
> 같은 연결 문자열의 `Open()/Close()` 호출은 내부적으로 풀에서 재사용.
> 추가 설정 불필요. `using var conn` 패턴으로 자동 반환.
