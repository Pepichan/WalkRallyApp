using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using WalkRallyApp.Data;

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

            // チームとその最新の走行情報を取得
            var runs = await _db.Runs
                .Include(r => r.Team)
                .Include(r => r.Submissions)
                .Include(r => r.Status)
                .ToListAsync();

            // コースごとのチェックポイント数
            var checkpointCounts = await _db.Checkpoints
                .GroupBy(c => c.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CourseId, x => x.Count);

            // 順位付け（得点→経過→開始時間）
            var rankedRuns = runs
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.ElapsedSeconds ?? int.MaxValue)
                .ThenBy(r => r.StartedAt)
                .ToList();

            for (var i = 0; i < rankedRuns.Count; i++)
            {
                var run = rankedRuns[i];

                // 到達済のチェックポイント数
                var reached = run.Submissions
                    .Select(s => s.CheckpointId)
                    .Distinct()
                    .Count();

                var total = checkpointCounts.TryGetValue(run.CourseId, out var cound) ? cound : 0;

                Rows.Add(new Row
                {
                    Rank = i + 1,
                    TeamName = run.Team.Name,
                    Score = run.Score,
                    ReachedCount = reached,
                    TotalCheckpoints = total,
                    LastUpdatedAt = run.Status?.LastUpdatedAt
                });
            }
        }

        public class  Row
        {
            public int Rank { get; set; }
            public string TeamName { get; set; } = string.Empty;
            public int Score { get; set; }
            public int ReachedCount { get; set; }
            public int TotalCheckpoints { get; set; }
            public DateTimeOffset? LastUpdatedAt { get; set; }
        }
    }
}
