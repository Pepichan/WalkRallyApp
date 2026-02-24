using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;
using WalkRallyApp.Models;

namespace WalkRallyApp.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _db;
        public DashboardModel(AppDbContext db)
        {
            _db = db;
        }

        public List<Row> Rows { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 画面表示用のデータを作る
            Rows = await BuildRowsAsync();
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            // CSV出力用のデータを作る
            var rows = await BuildRowsAsync();

            // CSV文字列を作成
            var csv = BuildCsv(rows);

            // UTF-8(BOM付き)＋charset指定で返す
            var bytes = Encoding.UTF8.GetBytes("\uFEFF" + csv);
            var fileName = $"results_{DateTimeOffset.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        // 画面/CSVの共通データ作成
        private async Task<List<Row>> BuildRowsAsync()
        {
            var runs = await _db.Runs
                .Include(r => r.Team)
                .Include(r => r.Course)
                .Include(r => r.Submissions)
                .Include(r => r.Status)
                .ToListAsync();

            var checkpointCounts = await _db.Checkpoints
                .GroupBy(c => c.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CourseId, x => x.Count);

            var rankedRuns = runs
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.ElapsedSeconds ?? int.MaxValue)
                .ThenBy(r => r.StartedAt)
                .ToList();

            var rows = new List<Row>();
            for (var i = 0; i < rankedRuns.Count; i++)
            {
                var run = rankedRuns[i];

                // 到達済みCP数
                var reached = run.Submissions
                    .Select(s => s.CheckpointId)
                    .Distinct()
                    .Count();

                var total = checkpointCounts.TryGetValue(run.CourseId, out var count) ? count : 0;

                rows.Add(new Row
                {
                    Rank = i + 1,
                    CourseName = run.Course.NameJa,
                    GoalReason = GetGoalReason(run),
                    TeamCode = run.Team.Code,
                    TeamName = run.Team.Name,
                    MemberCount = run.Team.MemberCount,
                    LeaderName = run.Team.LeaderName,
                    SchoolName = run.Team.SchoolName,
                    LeaderEmail = run.Team.LeaderEmail,
                    ConsentAt = run.Team.ConsentAt,
                    StartedAt = run.StartedAt,
                    FinishedAt = run.FinishedAt,
                    ElapsedSeconds = run.ElapsedSeconds,
                    Score = run.Score,
                    ReachedCount = reached,
                    TotalCheckpoints = total,
                    LastUpdatedAt = run.Status?.LastUpdatedAt
                });
            }

            return rows;
        }

        // ゴール理由を作る
        private static string GetGoalReason(Run run)
        {
            if (!run.IsFinished)
            {
                return "未完了";
            }

            var elapsed = run.ElapsedSeconds ?? (int)(run.FinishedAt - run.StartedAt)!.Value.TotalSeconds;
            var limitSeconds = run.Course.TimeLimitMinutes * 60;

            return elapsed >= limitSeconds ? "時間切れ" : "ゴール";
        }

        // CSV文字列を作る
        private static string BuildCsv(List<Row> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("順位,コース,ゴール理由,班コード,班,人数,代表者名,学校名,メール,同意日時,開始時刻,終了時刻,経過秒,進捗,得点,最終更新");

            foreach (var row in rows)
            {
                // Excelで日付変換されないようにする
                var progress = $"{row.ReachedCount} / {row.TotalCheckpoints} CP";
                var last = row.LastUpdatedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
                var consent = row.ConsentAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
                var started = row.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                var finished = row.FinishedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
                var elapsed = row.ElapsedSeconds?.ToString() ?? "-";

                sb.AppendLine(string.Join(",",
                    EscapeCsv(row.Rank.ToString()),
                    EscapeCsv(row.CourseName),
                    EscapeCsv(row.GoalReason),
                    EscapeCsv(row.TeamCode),
                    EscapeCsv(row.TeamName),
                    EscapeCsv(row.MemberCount.ToString()),
                    EscapeCsv(row.LeaderName),
                    EscapeCsv(row.SchoolName ?? string.Empty),
                    EscapeCsv(row.LeaderEmail ?? string.Empty),
                    EscapeCsv(consent),
                    EscapeCsv(started),
                    EscapeCsv(finished),
                    EscapeCsv(elapsed),
                    EscapeCsv(progress),
                    EscapeCsv(row.Score.ToString()),
                    EscapeCsv(last)));
            }

            return sb.ToString();
        }

        // CSV用に文字列をエスケープする
        private static string EscapeCsv(string value)
        {
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        public class Row
        {
            public int Rank { get; set; }
            public string CourseName { get; set; } = string.Empty;
            public string GoalReason { get; set; } = string.Empty;
            public string TeamCode { get; set; } = string.Empty;
            public string TeamName { get; set; } = string.Empty;
            public int MemberCount { get; set; }
            public string LeaderName { get; set; } = string.Empty;
            public string? SchoolName { get; set; }
            public string? LeaderEmail { get; set; }
            public DateTimeOffset? ConsentAt { get; set; }
            public DateTimeOffset StartedAt { get; set; }
            public DateTimeOffset? FinishedAt { get; set; }
            public int? ElapsedSeconds { get; set; }
            public int Score { get; set; }
            public int ReachedCount { get; set; }
            public int TotalCheckpoints { get; set; }
            public DateTimeOffset? LastUpdatedAt { get; set; }
        }
    }
}
