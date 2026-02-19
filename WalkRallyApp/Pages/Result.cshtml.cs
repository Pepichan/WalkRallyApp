using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;

namespace WalkRallyApp.Pages
{
    public class ResultModel : PageModel
    {
        private readonly AppDbContext _db;

        public ResultModel(AppDbContext db)
        {
            _db = db;
        }

        public string TeamName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Rank { get; set; }
        public int TotalTeams { get; set; }

        public async Task<IActionResult> OnGetAsync(int runId)
        {
            var run = await _db.Runs
                .Include(r => r.Team)
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null)
            {
                return RedirectToPage("Join");
            }

            var runs = await _db.Runs
                .Include(r => r.Team)
                .Where(r => r.CourseId == run.CourseId)
                .ToListAsync();

            var rankedRuns = runs
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.ElapsedSeconds ?? int.MaxValue)
                .ThenBy(r => r.StartedAt)
                .ToList();

            var index = rankedRuns.FindIndex(r => r.Id == run.Id);

            TeamName = run.Team.Name;
            Score = run.Score;
            Rank = index >= 0 ? index + 1 : rankedRuns.Count;
            TotalTeams = rankedRuns.Count;

            return Page();
        }
    }
}
