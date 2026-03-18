using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V005_Salary : IMigration
{
    public int Version => 5;
    public string Description => "salary_records 테이블";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS salary_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                employee_id INTEGER NOT NULL REFERENCES employees(id),
                year_month TEXT NOT NULL,
                week1_hours REAL DEFAULT 0,
                week2_hours REAL DEFAULT 0,
                week3_hours REAL DEFAULT 0,
                week4_hours REAL DEFAULT 0,
                week5_hours REAL DEFAULT 0,
                total_hours REAL DEFAULT 0,
                holiday_hours REAL DEFAULT 0,
                holiday_bonus INTEGER DEFAULT 0,
                base_salary INTEGER DEFAULT 0,
                meal_allowance INTEGER DEFAULT 0,
                taxi_allowance INTEGER DEFAULT 0,
                gross_salary INTEGER DEFAULT 0,
                tax_33 INTEGER DEFAULT 0,
                net_salary INTEGER DEFAULT 0,
                is_manual_edit INTEGER DEFAULT 0,
                created_at TEXT NOT NULL DEFAULT (datetime('now','localtime')),
                updated_at TEXT NOT NULL DEFAULT (datetime('now','localtime'))
            )
            """, transaction: tx);

        conn.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_salary_emp_month ON salary_records(employee_id, year_month)", transaction: tx);
    }
}
