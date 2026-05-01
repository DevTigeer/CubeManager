using System.Data;
using Dapper;

namespace CubeManager.Data.Migrations;

public class V023_ThemeJsonTemplateMetadata : IMigration
{
    public int Version => 23;
    public string Description => "themes에 JSON 템플릿 메타데이터 추가";

    public void Execute(IDbConnection conn, IDbTransaction tx)
    {
        var columns = conn.Query<string>(
            "SELECT name FROM pragma_table_info('themes')", transaction: tx);
        var colSet = new HashSet<string>(columns);

        AddColumnIfMissing("theme_key", "TEXT");
        AddColumnIfMissing("bg_color", "TEXT");
        AddColumnIfMissing("accent_color", "TEXT");
        AddColumnIfMissing("icon", "TEXT");
        AddColumnIfMissing("code_prefix", "TEXT");

        BackfillTemplateMetadata(
            "obsession", "#15081a", "#e11d48", "🧠", "a",
            "집착", "obsession");
        BackfillTemplateMetadata(
            "cinderella", "#0b1226", "#60a5fa", "🥿", "b",
            "신데렐라", "cinderella");
        BackfillTemplateMetadata(
            "towering", "#0a1414", "#22c55e", "🏙️", "c",
            "타워링", "Towering", "towering");
        BackfillTemplateMetadata(
            "organ_trafficking", "#1a0e0e", "#f59e0b", "🩸", "d",
            "장기밀매", "organ_trafficking");
        BackfillTemplateMetadata(
            "titanic", "#001f3f", "#00bcd4", "🚢", "e",
            "타이타닉", "titanic");

        conn.Execute("""
            CREATE UNIQUE INDEX IF NOT EXISTS idx_themes_theme_key
            ON themes(theme_key)
            WHERE theme_key IS NOT NULL AND theme_key <> ''
            """, transaction: tx);

        void AddColumnIfMissing(string name, string type)
        {
            if (!colSet.Contains(name))
                conn.Execute($"ALTER TABLE themes ADD COLUMN {name} {type}", transaction: tx);
        }

        void BackfillTemplateMetadata(
            string themeKey,
            string bgColor,
            string accentColor,
            string icon,
            string codePrefix,
            params string[] names)
        {
            foreach (var name in names)
            {
                conn.Execute("""
                    UPDATE themes
                    SET theme_key = CASE WHEN theme_key IS NULL OR theme_key = '' THEN @ThemeKey ELSE theme_key END,
                        bg_color = CASE WHEN bg_color IS NULL OR bg_color = '' THEN @BgColor ELSE bg_color END,
                        accent_color = CASE WHEN accent_color IS NULL OR accent_color = '' THEN @AccentColor ELSE accent_color END,
                        icon = CASE WHEN icon IS NULL OR icon = '' THEN @Icon ELSE icon END,
                        code_prefix = CASE WHEN code_prefix IS NULL OR code_prefix = '' THEN @CodePrefix ELSE code_prefix END
                    WHERE id = (
                        SELECT candidate.id
                        FROM themes AS candidate
                        WHERE candidate.theme_name = @Name
                          AND NOT EXISTS (
                              SELECT 1
                              FROM themes AS existing
                              WHERE existing.id <> candidate.id
                                AND existing.theme_key = @ThemeKey
                          )
                        ORDER BY candidate.id
                        LIMIT 1
                    )
                    """,
                    new
                    {
                        ThemeKey = themeKey,
                        BgColor = bgColor,
                        AccentColor = accentColor,
                        Icon = icon,
                        CodePrefix = codePrefix,
                        Name = name
                    },
                    tx);
            }
        }
    }
}
