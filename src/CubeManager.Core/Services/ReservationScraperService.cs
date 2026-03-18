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

    private static List<Reservation> ParseReservations(IDocument page, DateTime date)
    {
        var reservations = new List<Reservation>();

        // 테이블 찾기 (여러 선택자 시도)
        var table = page.QuerySelector("table.room-table")
                    ?? page.QuerySelector("table#room_table")
                    ?? page.QuerySelector("table.table")
                    ?? page.QuerySelectorAll("table").LastOrDefault();

        if (table == null)
        {
            Log.Warning("예약 테이블을 찾을 수 없음");
            return reservations;
        }

        // 헤더에서 방 이름 추출
        var headers = table.QuerySelectorAll("thead th, tr:first-child th, tr:first-child td")
            .Select(h => h.TextContent.Trim())
            .ToList();

        var rows = table.QuerySelectorAll("tbody tr, tr").Skip(1); // 헤더 제외

        foreach (var row in rows)
        {
            var cells = row.QuerySelectorAll("td").ToList();
            if (cells.Count < 2) continue;

            var timeSlot = cells[0].TextContent.Trim();

            for (var i = 1; i < cells.Count; i++)
            {
                var cellText = cells[i].TextContent.Trim();
                if (string.IsNullOrEmpty(cellText)) continue;

                var roomName = i < headers.Count ? headers[i] : $"방{i}";

                reservations.Add(ParseCell(date, timeSlot, roomName, cellText));
            }
        }

        Log.Information("예약 파싱 완료: {Count}건", reservations.Count);
        return reservations;
    }

    private static Reservation ParseCell(DateTime date, string timeSlot, string roomName, string cellText)
    {
        var nameMatch = Regex.Match(cellText, @"([가-힣a-zA-Z]+)");
        var countMatch = Regex.Match(cellText, @"(\d+)\s*명");
        var phoneMatch = Regex.Match(cellText, @"(01[016789]-?\d{3,4}-?\d{4})");

        return new Reservation
        {
            ReservationDate = date.ToString("yyyy-MM-dd"),
            TimeSlot = timeSlot,
            RoomName = roomName,
            CustomerName = nameMatch.Success ? nameMatch.Groups[1].Value : cellText,
            Headcount = countMatch.Success ? int.Parse(countMatch.Groups[1].Value) : 0,
            CustomerPhone = phoneMatch.Success ? phoneMatch.Groups[1].Value : null,
            Status = "confirmed",
            SyncedAt = DateTime.Now
        };
    }
}
