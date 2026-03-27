using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V016_OperationChecklist : IMigration
{
    public int Version => 16;
    public string Description => "운영 매뉴얼 기반 체크리스트 시드 데이터";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        // 기존 시드가 있으면 스킵 (이미 사용자가 추가한 데이터 보존)
        var existing = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM checklist_templates", transaction: tx);
        if (existing > 0) return;

        var order = 1;

        // ═══════════════════════════════════════
        // 매일 오픈 업무
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 2, 3, 4, 5, 6, 0 }) // 월~일
        {
            Insert(conn, tx, dow, "open", "환기: 장기밀매 창문 윗 고리 열기 (외부 철창문 열지 않기)", ref order);
            Insert(conn, tx, dow, "open", "환기: 계단창문/출입문/큐브출입문 열어 30분 환기", ref order);
        }

        // 토요일 오픈 추가
        Insert(conn, tx, 6, "open", "스케줄 단톡에 올리기", ref order);

        // ═══════════════════════════════════════
        // 월/수/금/일 오픈: 알코올 닦기
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 3, 5, 0 }) // 월,수,금,일
        {
            Insert(conn, tx, dow, "open", "알코올 물건 닦기 (상자/자물쇠 내부)", ref order);
            Insert(conn, tx, dow, "open", "물건 고장/자물쇠 고장/힌트종이 교체 확인", ref order);
        }

        // ═══════════════════════════════════════
        // 화/목 오픈: 장치 확인
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 2, 4 }) // 화,목
        {
            Insert(conn, tx, dow, "open", "장치 확인: 타워링 버튼 + 캐비넷 파손 확인", ref order);
            Insert(conn, tx, dow, "open", "장치 확인: 타이타닉 냄비/버튼/2층 EM락", ref order);
            Insert(conn, tx, dow, "open", "장치 확인: 신데렐라 땔감", ref order);
            Insert(conn, tx, dow, "open", "장치 확인: 장기밀매 컵 확인 후 메모", ref order);
            Insert(conn, tx, dow, "open", "모든 테마 낙서 확인 후 지우기", ref order);
            Insert(conn, tx, dow, "open", "장치 확인 결과 보고 (인수인계)", ref order);
        }

        // ═══════════════════════════════════════
        // 월~금 17:30~18:00 비품 체크 (마감)
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 2, 3, 4, 5 })
        {
            Insert(conn, tx, dow, "close", "17:30~18:00 모든 테마 물건 닦기", ref order);
            Insert(conn, tx, dow, "close", "비품 체크: 랜턴 건전지(흰색) 교체", ref order);
            Insert(conn, tx, dow, "close", "장치 작동 유무 확인", ref order);
        }

        // ═══════════════════════════════════════
        // 매일 마감: 청소
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 2, 3, 4, 5, 6, 0 })
        {
            Insert(conn, tx, dow, "close", "마감 청소 후 사진 전송", ref order);
        }

        // 월/화/수: 2테마 청소
        foreach (var dow in new[] { 1, 2, 3 })
        {
            Insert(conn, tx, dow, "close", "마감 청소: 2테마 집중 청소", ref order);
        }

        // 목: 1테마 청소
        Insert(conn, tx, 4, "close", "마감 청소: 1테마 집중 청소", ref order);

        // 목요일 마감: 잔돈 교체
        Insert(conn, tx, 4, "close", "잔돈 교체: 천원 50장↑ / 만원 10장↑", ref order);

        // ═══════════════════════════════════════
        // 전 파트 공통 (all)
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 2, 3, 4, 5, 6, 0 })
        {
            Insert(conn, tx, dow, "all", "예약 10분전 미도착 손님 전화 확인", ref order);
            Insert(conn, tx, dow, "all", "힌트 가이드: 15분/40분 체크 및 전화", ref order);
        }
    }

    private static void Insert(IDbConnection conn, IDbTransaction tx,
        int dayOfWeek, string role, string taskText, ref int order)
    {
        conn.Execute(
            "INSERT INTO checklist_templates (day_of_week, task_text, sort_order, is_active, role) " +
            "VALUES (@dow, @task, @order, 1, @role)",
            new { dow = dayOfWeek, task = taskText, order, role },
            tx);
        order++;
    }
}
