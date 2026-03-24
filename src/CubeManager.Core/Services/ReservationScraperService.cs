using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using Serilog;

namespace CubeManager.Core.Services;

[SupportedOSPlatform("windows")]
public class ReservationScraperService : IReservationScraperService
{
    private readonly IConfigRepository _configRepo;
    private HttpClient? _httpClient;
    private System.Net.CookieContainer? _cookieContainer;

    public ReservationScraperService(IConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public async Task<IEnumerable<Reservation>> FetchReservationsAsync(DateTime date)
    {
        var (id, pw) = await GetCredentialsAsync();
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            throw new InvalidOperationException("웹 자격증명이 설정되지 않았습니다. 설정 탭에서 입력하세요.");

        var baseUrl = await _configRepo.GetAsync("web_base_url")
                      ?? "http://www.cubeescape.co.kr";

        // 1) 클라이언트가 없으면 새로 로그인
        if (_httpClient == null)
            await LoginAsync(id, pw, baseUrl);

        // 2) 세션 유효성 검증: 관리자 페이지 접근 시도
        if (!await IsSessionValidAsync(baseUrl))
        {
            Log.Information("세션 만료 확인 — 재로그인");
            DisposeClient();
            await LoginAsync(id, pw, baseUrl);
        }

        // 3) 실제 예약 조회
        var dateStr = date.ToString("yy-MM-dd");
        var url = $"{baseUrl}/adm/room_list.php?sfl=r_date&stx={dateStr}";
        Log.Information("예약 조회: {Url}", url);

        string html;
        try
        {
            html = await _httpClient!.GetStringAsync(url);
        }
        catch (HttpRequestException ex)
        {
            // 네트워크 에러 → 재로그인 1회 시도
            Log.Warning(ex, "HTTP 요청 실패 — 재로그인 시도");
            DisposeClient();
            await LoginAsync(id, pw, baseUrl);
            html = await _httpClient!.GetStringAsync(url);
        }

        // 응답이 로그인 페이지면 재시도 (이중 방어)
        if (IsLoginPage(html))
        {
            Log.Warning("응답이 로그인 페이지 — 재로그인");
            DisposeClient();
            await LoginAsync(id, pw, baseUrl);
            html = await _httpClient!.GetStringAsync(url);
        }

        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var page = await context.OpenAsync(req => req.Content(html));

        var results = ParseReservations(page, date);
        Log.Information("예약 조회 완료: {Count}건", results.Count);
        return results;
    }

    public async Task<bool> TestConnectionAsync(string id, string password)
    {
        try
        {
            var baseUrl = await _configRepo.GetAsync("web_base_url")
                          ?? "http://www.cubeescape.co.kr";

            // HttpClient + CookieContainer로 직접 POST 로그인
            var cookieContainer = new System.Net.CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookieContainer };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };

            var loginData = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("mb_id", id),
                new KeyValuePair<string, string>("mb_password", password),
                new KeyValuePair<string, string>("url", $"{baseUrl}/adm")
            ]);

            var loginResult = await client.PostAsync($"{baseUrl}/bbs/login_check.php", loginData);
            Log.Information("로그인 POST 응답: {StatusCode}", loginResult.StatusCode);

            // 로그인 성공 판별: 관리자 페이지 접근 가능 여부
            var testResponse = await client.GetStringAsync($"{baseUrl}/adm/");

            // 관리자 페이지에 "예약" 또는 "관리"가 있으면 성공
            var success = testResponse.Contains("예약") || testResponse.Contains("관리");
            Log.Information("연결 테스트 결과: {Result}, 본문길이: {Len}",
                success ? "성공" : "실패", testResponse.Length);

            return success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "연결 테스트 실패");
            return false;
        }
    }

    private async Task<(string id, string pw)> GetCredentialsAsync()
    {
        var encId = await _configRepo.GetAsync("web_login_id") ?? "";
        var encPw = await _configRepo.GetAsync("web_login_pw") ?? "";

        return (CredentialHelper.Decrypt(encId), CredentialHelper.Decrypt(encPw));
    }

    /// <summary>세션 유효성 검증: 관리자 페이지에 실제 접근 가능한지 확인</summary>
    private async Task<bool> IsSessionValidAsync(string baseUrl)
    {
        if (_httpClient == null) return false;
        try
        {
            var html = await _httpClient.GetStringAsync($"{baseUrl}/adm/");
            // 로그인 페이지로 리다이렉트되면 세션 만료
            if (IsLoginPage(html)) return false;
            // 관리자 페이지 컨텐츠가 있으면 유효
            return html.Contains("예약") || html.Contains("관리") || html.Contains("adm");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>응답 HTML이 로그인 페이지인지 판별</summary>
    private static bool IsLoginPage(string html) =>
        html.Contains("login_check") || html.Contains("mb_password") || html.Contains("로그인");

    /// <summary>새로 로그인</summary>
    private async Task LoginAsync(string id, string pw, string baseUrl)
    {
        _cookieContainer = new System.Net.CookieContainer();
        var handler = new HttpClientHandler { CookieContainer = _cookieContainer };
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        var loginData = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("mb_id", id),
            new KeyValuePair<string, string>("mb_password", pw),
            new KeyValuePair<string, string>("url", $"{baseUrl}/adm")
        ]);

        await _httpClient.PostAsync($"{baseUrl}/bbs/login_check.php", loginData);
        Log.Information("웹 로그인 완료");
    }

    /// <summary>기존 HttpClient 정리</summary>
    private void DisposeClient()
    {
        _httpClient?.Dispose();
        _httpClient = null;
    }

    /// <summary>
    /// 실제 cubeescape.co.kr/adm/room_list.php HTML 구조:
    /// <table>
    ///   <thead> 체크 | 지점명 | 예약번호 | 예약일 | 시간 | 선택테마 | 인원 | 예약자 | 연락처 | IP | 등록일 | 관리 </thead>
    ///   <tbody>
    ///     <tr> 한 행 = 한 예약건 </tr>
    ///   </tbody>
    /// </table>
    /// </summary>
    private static List<Reservation> ParseReservations(IDocument page, DateTime date)
    {
        var reservations = new List<Reservation>();

        var table = page.QuerySelector("table");
        if (table == null)
        {
            Log.Warning("예약 테이블을 찾을 수 없음");
            return reservations;
        }

        // 헤더 인덱스 매핑 (유연하게 처리)
        var headers = table.QuerySelectorAll("thead th")
            .Select((h, i) => (text: h.TextContent.Trim(), index: i))
            .ToList();

        int ColIndex(string keyword) =>
            headers.FirstOrDefault(h => h.text.Contains(keyword)).index;

        var idxTime = ColIndex("시간");
        var idxTheme = ColIndex("테마");
        var idxCount = ColIndex("인원");
        var idxName = ColIndex("예약자");
        var idxPhone = ColIndex("연락처");
        var idxBranch = ColIndex("지점");

        var rows = table.QuerySelectorAll("tbody tr");

        foreach (var row in rows)
        {
            var cells = row.QuerySelectorAll("td").ToList();
            if (cells.Count < 6) continue;

            var timeText = SafeCell(cells, idxTime);
            var themeText = SafeCell(cells, idxTheme);
            var countText = SafeCell(cells, idxCount);
            var nameText = SafeCell(cells, idxName);
            var phoneText = SafeCell(cells, idxPhone);
            var branchText = SafeCell(cells, idxBranch);

            // 인원 파싱: "2 명" → 2
            var countMatch = Regex.Match(countText, @"(\d+)");
            var headcount = countMatch.Success ? int.Parse(countMatch.Groups[1].Value) : 0;

            // 전화번호 정규화
            var phoneMatch = Regex.Match(phoneText, @"(01[016789]-?\d{3,4}-?\d{4})");

            reservations.Add(new Reservation
            {
                ReservationDate = date.ToString("yyyy-MM-dd"),
                TimeSlot = timeText,
                ThemeName = themeText,
                CustomerName = nameText,
                Headcount = headcount,
                CustomerPhone = phoneMatch.Success ? phoneMatch.Groups[1].Value : null,
                Status = "confirmed",
                SyncedAt = DateTime.Now
            });
        }

        Log.Information("예약 파싱 완료: {Count}건 ({Branch})",
            reservations.Count,
            reservations.FirstOrDefault()?.ThemeName ?? "N/A");
        return reservations;
    }

    private static string SafeCell(List<AngleSharp.Dom.IElement> cells, int index) =>
        index >= 0 && index < cells.Count ? cells[index].TextContent.Trim() : "";
}
