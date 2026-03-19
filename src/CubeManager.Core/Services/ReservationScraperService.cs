using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using CubeManager.Core.Helpers;
using CubeManager.Core.Interfaces.Repositories;
using CubeManager.Core.Interfaces.Services;
using CubeManager.Core.Models;
using Serilog;

namespace CubeManager.Core.Services;

public class ReservationScraperService : IReservationScraperService
{
    private readonly IConfigRepository _configRepo;
    private IBrowsingContext? _context;

    public ReservationScraperService(IConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public async Task<IEnumerable<Reservation>> FetchReservationsAsync(DateTime date)
    {
        try
        {
            var (id, pw) = await GetCredentialsAsync();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            {
                Log.Warning("웹 자격증명 미설정 - 스크래핑 건너뜀");
                return [];
            }

            await EnsureLoggedInAsync(id, pw);

            var baseUrl = await _configRepo.GetAsync("web_base_url")
                          ?? "http://www.cubeescape.co.kr";
            var dateStr = date.ToString("yy-MM-dd");
            var url = $"{baseUrl}/adm/room_list.php?sfl=r_date&stx={dateStr}";

            Log.Information("예약 조회: {Url}", url);

            var page = await _context!.OpenAsync(url);
            return ParseReservations(page, date);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "예약 스크래핑 실패: {Date}", date.ToString("yyyy-MM-dd"));
            return [];
        }
    }

    public async Task<bool> TestConnectionAsync(string id, string password)
    {
        try
        {
            var config = Configuration.Default
                .WithDefaultLoader(new LoaderOptions { IsResourceLoadingEnabled = false })
                .WithCookies();
            var ctx = BrowsingContext.New(config);

            var baseUrl = await _configRepo.GetAsync("web_base_url")
                          ?? "http://www.cubeescape.co.kr";

            var loginPage = await ctx.OpenAsync($"{baseUrl}/bbs/login.php");
            var form = loginPage.QuerySelector<IHtmlFormElement>("form[name='flogin']")
                       ?? loginPage.QuerySelector<IHtmlFormElement>("form");

            if (form == null)
            {
                Log.Warning("로그인 폼을 찾을 수 없음");
                return false;
            }

            var result = await form.SubmitAsync(new { mb_id = id, mb_password = password });

            // 로그인 성공 판별: 관리자 페이지 접근 가능 여부
            var testPage = await ctx.OpenAsync($"{baseUrl}/adm/");
            var bodyText = testPage.Body?.TextContent ?? "";

            var success = !bodyText.Contains("로그인") || bodyText.Contains("예약");
            Log.Information("연결 테스트 결과: {Result}", success ? "성공" : "실패");

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

    private async Task EnsureLoggedInAsync(string id, string pw)
    {
        if (_context != null) return;

        var config = Configuration.Default
            .WithDefaultLoader(new LoaderOptions { IsResourceLoadingEnabled = false })
            .WithCookies();
        _context = BrowsingContext.New(config);

        var baseUrl = await _configRepo.GetAsync("web_base_url")
                      ?? "http://www.cubeescape.co.kr";

        var loginPage = await _context.OpenAsync($"{baseUrl}/bbs/login.php");
        var form = loginPage.QuerySelector<IHtmlFormElement>("form[name='flogin']")
                   ?? loginPage.QuerySelector<IHtmlFormElement>("form");

        if (form == null)
            throw new InvalidOperationException("로그인 폼을 찾을 수 없습니다.");

        await form.SubmitAsync(new { mb_id = id, mb_password = pw });
        Log.Information("웹 로그인 완료");
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
                RoomName = themeText,
                CustomerName = nameText,
                Headcount = headcount,
                CustomerPhone = phoneMatch.Success ? phoneMatch.Groups[1].Value : null,
                Status = "confirmed",
                SyncedAt = DateTime.Now
            });
        }

        Log.Information("예약 파싱 완료: {Count}건 ({Branch})",
            reservations.Count,
            reservations.FirstOrDefault()?.RoomName ?? "N/A");
        return reservations;
    }

    private static string SafeCell(List<AngleSharp.Dom.IElement> cells, int index) =>
        index >= 0 && index < cells.Count ? cells[index].TextContent.Trim() : "";
}
