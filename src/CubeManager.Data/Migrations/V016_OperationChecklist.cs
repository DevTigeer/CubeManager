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
        // 매일 오픈: 환기
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 2, 3, 4, 5, 6, 0 }) // 월~일
        {
            Insert(conn, tx, dow, "open", "환기: 장기밀매 창문 윗 고리 열기 (외부철창문은 열지않기)", ref order);
            Insert(conn, tx, dow, "open", "환기: 계단창문/문/큐브출입문도 열어서 30분환기", ref order);
        }

        // 토요일 오픈 추가
        Insert(conn, tx, 6, "open", "스케줄 단톡에 올리기", ref order);

        // ═══════════════════════════════════════
        // 월/수/금/일 오픈: 알코올로 물건닦기
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 3, 5, 0 })
        {
            Insert(conn, tx, dow, "open", "알코올로 물건닦기 (상자나, 자물쇠 열어서 내부 물건 닦기)", ref order);
            Insert(conn, tx, dow, "open", "이때 물건고장, 자물쇠고장, 힌트종이 교체 필요시 교체", ref order);
        }

        // ═══════════════════════════════════════
        // 화/목 오픈: 장치 확인 후 보고
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 2, 4 })
        {
            Insert(conn, tx, dow, "open", "타워링 모든 버튼, 타워링캐비넷 파손된것 확인후 고치기", ref order);
            Insert(conn, tx, dow, "open", "타이타닉 냄비, 버튼, 2층올라가는 이엠락 확인", ref order);
            Insert(conn, tx, dow, "open", "신데 땔감 확인", ref order);
            Insert(conn, tx, dow, "open", "장기밀매 컵 확인후 메모", ref order);
            Insert(conn, tx, dow, "open", "모든 테마 낙서 확인 후 지우기", ref order);
        }

        // ═══════════════════════════════════════
        // 월~금 5:30~6:00 비품 체크 (마감)
        // ═══════════════════════════════════════
        foreach (var dow in new[] { 1, 2, 3, 4, 5 })
        {
            Insert(conn, tx, dow, "close", "5:30~6:00 모든 테마 : 열어서 물건닦기, 및 비품체크 랜턴건전지 (흰색)교체", ref order);
            Insert(conn, tx, dow, "close", "장치 한번씩 작동시켜서 작동유무확인", ref order);
        }

        // ═══════════════════════════════════════
        // 마감 청소 (요일별 배정)
        // ═══════════════════════════════════════
        // 공통: 매일 마감
        foreach (var dow in new[] { 1, 2, 3, 4, 5, 6, 0 })
        {
            Insert(conn, tx, dow, "close", "마감 청소, 손에 잘안닿는 곳까지 청소후 사진 전송", ref order);
        }

        // 월: 신데, 타타
        Insert(conn, tx, 1, "close", "청소: 신데렐라 - 책상아래 + 첫방 / 막방+ 서랍장아래, 호박마차뒤,위", ref order);
        Insert(conn, tx, 1, "close", "청소: 타이타닉 - 첫방 침대아래 / 두번째방 / 막방 계단", ref order);

        // 화: 장기, 로비
        Insert(conn, tx, 2, "close", "청소: 장기밀매 - 첫방 작은캐비넷 빼서 아래, 마네킹 아래 / 막방 냉장고 밀어서 아래", ref order);
        Insert(conn, tx, 2, "close", "청소: 로비 - 책상위 의자위 청소, 정수기 근처 자물쇠두는곳 아래, 사진기근처", ref order);

        // 수: 카운터
        Insert(conn, tx, 3, "close", "청소: 카운터 - 모니터, 노트북, 청소, 물걸레", ref order);

        // 목: 타워링, 로비
        Insert(conn, tx, 4, "close", "청소: 타워링 - 첫방 / 막방 (마스크끼고 러그털기)", ref order);
        Insert(conn, tx, 4, "close", "청소: 로비 - 책상위 의자위 청소, 정수기 근처 자물쇠두는곳 아래, 사진기근처", ref order);

        // 목요일 마감: 잔돈 교체
        Insert(conn, tx, 4, "close", "잔돈교체 (천원 : 50장 이상 / 만원 10장 이상)", ref order);

        // ═══════════════════════════════════════
        // 금요일 마감
        // ═══════════════════════════════════════
        Insert(conn, tx, 5, "close", "비품재고파악 및 부족분 확인 후 단톡에 고지", ref order);
        Insert(conn, tx, 5, "close", "장기밀매 물통 비우기 및 페트병 물받기", ref order);
        Insert(conn, tx, 5, "close", "스피커의 케이블정상작동 유무 파악 및 보조배터리 작동유무 파악", ref order);
        Insert(conn, tx, 5, "close", "건전지 부족시 사오기", ref order);

        // ═══════════════════════════════════════
        // 일요일
        // ═══════════════════════════════════════
        // 5:30~7:00 미들
        Insert(conn, tx, 0, "middle1", "손님 퇴장 후 10분안에 미들과 인터폰 점검", ref order);

        // 일요일 마감: 테마별 장치 점검
        Insert(conn, tx, 0, "close", "타워링: 캐비넷 버튼 잘되는지 + 라디오 소리 잘들리는지 + 버튼누르는곳 달랑거리면 나사풀고 다시 조이기", ref order);
        Insert(conn, tx, 0, "close", "장기밀매: xray기계 안떨어졌는지, 컵 잘작동하는지, 선빠지지않았는지 확인", ref order);
        Insert(conn, tx, 0, "close", "타이타닉: 2층올라가는 이엠락 빠지려고하는지, 선빠지려는지 확인", ref order);
        Insert(conn, tx, 0, "close", "신데: 땔감확인", ref order);
        Insert(conn, tx, 0, "close", "집착: 두번째방천장 경첩 빠지려는지확인", ref order);
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
