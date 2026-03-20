using System.Net.Http.Json;
using System.Text.Json;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using Serilog;

namespace CubeManager.Core.Services;

/// <summary>
/// 공공데이터포털 API를 통한 공휴일 자동 동기화 서비스.
/// API: data.go.kr 특일정보 (한국천문연구원)
///
/// 성능 영향:
/// - HTTP 요청 1~2회/년 (최초 로드 시만)
/// - 응답 크기: ~3KB (JSON, 연간 ~17건)
/// - DB 쓰기: INSERT OR IGNORE × 17건 (트랜잭션)
/// - 메모리: 무시 수준 (~1KB 미만)
/// - 이미 동기화된 연도는 API 호출 없이 즉시 반환
/// </summary>
public class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _holidayRepo;
    private readonly IConfigRepository _configRepo;
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    // 공공데이터포털 특일정보 API
    private const string ApiBaseUrl = "http://apis.data.go.kr/B090041/openapi/service/SpcdeInfoService/getRestDeInfo";

    public HolidayService(IHolidayRepository holidayRepo, IConfigRepository configRepo)
    {
        _holidayRepo = holidayRepo;
        _configRepo = configRepo;
    }

    public async Task<int> SyncHolidaysAsync(int year)
    {
        // 이미 충분한 데이터가 있으면 스킵 (연간 공휴일 ~15건 이상이면 동기화 완료로 판단)
        var existingCount = await _holidayRepo.GetCountByYearAsync(year);
        if (existingCount >= 10)
        {
            Log.Debug("공휴일 이미 동기화됨: {Year}년 ({Count}건)", year, existingCount);
            return 0;
        }

        // API 키 조회
        var apiKey = await _configRepo.GetAsync("holiday_api_key");
        if (string.IsNullOrEmpty(apiKey))
        {
            Log.Warning("공휴일 API 키 미설정 — 설정 탭에서 입력 필요");
            return 0;
        }

        try
        {
            var holidays = await FetchFromApiAsync(apiKey, year);
            if (holidays.Count == 0)
            {
                Log.Warning("공휴일 API 응답 0건: {Year}년", year);
                return 0;
            }

            await _holidayRepo.UpsertHolidaysAsync(holidays);
            Log.Information("공휴일 동기화 완료: {Year}년 {Count}건", year, holidays.Count);
            return holidays.Count;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "공휴일 API 호출 실패: {Year}년", year);
            return 0;
        }
    }

    public Task<IEnumerable<Holiday>> GetHolidaysAsync(int year) =>
        _holidayRepo.GetByYearAsync(year);

    public Task<bool> IsWeekdayHolidayAsync(string date) =>
        _holidayRepo.IsWeekdayHolidayAsync(date);

    /// <summary>
    /// 공공데이터포털 특일정보 API 호출.
    /// 1년치를 1~2회 호출로 가져옴 (numOfRows=50이면 1회로 충분).
    /// </summary>
    private static async Task<List<Holiday>> FetchFromApiAsync(string apiKey, int year)
    {
        var url = $"{ApiBaseUrl}?serviceKey={apiKey}&solYear={year}&numOfRows=50&_type=json";

        var response = await SharedHttpClient.GetStringAsync(url);
        var doc = JsonDocument.Parse(response);

        var holidays = new List<Holiday>();

        // API 응답 구조: response > body > items > item (배열 또는 단일)
        if (!doc.RootElement.TryGetProperty("response", out var resp)) return holidays;
        if (!resp.TryGetProperty("body", out var body)) return holidays;
        if (!body.TryGetProperty("items", out var items)) return holidays;

        // items가 빈 문자열이면 0건
        if (items.ValueKind == JsonValueKind.String) return holidays;

        if (!items.TryGetProperty("item", out var itemArr)) return holidays;

        // item이 배열 또는 단일 객체일 수 있음
        var itemList = itemArr.ValueKind == JsonValueKind.Array
            ? itemArr.EnumerateArray().ToList()
            : [itemArr];

        foreach (var item in itemList)
        {
            var locdate = item.GetProperty("locdate").GetInt32().ToString(); // 20260101
            var dateName = item.GetProperty("dateName").GetString() ?? "";
            var isHoliday = item.GetProperty("isHoliday").GetString() == "Y";

            if (!isHoliday) continue; // 비공휴일(기념일 등) 제외

            var dateStr = $"{locdate[..4]}-{locdate[4..6]}-{locdate[6..8]}"; // YYYY-MM-DD
            var date = DateTime.Parse(dateStr);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            holidays.Add(new Holiday
            {
                HolidayDate = dateStr,
                HolidayName = dateName,
                IsWeekend = isWeekend,
                Year = year
            });
        }

        return holidays;
    }
}
