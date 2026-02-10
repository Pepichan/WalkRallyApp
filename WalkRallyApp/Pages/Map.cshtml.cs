using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;

namespace WalkRallyApp.Pages
{
    public class MapModel : PageModel
    {
        private readonly AppDbContext _db;
        
        public MapModel(AppDbContext db)
        {
            _db = db;
        }

        public string TeamName { get; set; } = string.Empty;
        public string RemainingTimeText { get; set; } = "00:00";

        public async Task<IActionResult> OnGetAsync(int runId)
        {
            var run = await _db.Runs
                .Include(r => r.Team)
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null)
            {
                return RedirectToPage("Join");
            }

            TeamName = run.Team.Name;

            var limit = TimeSpan.FromMinutes(run.Course.TimeLimitMinutes);
            var elapsed = DateTimeOffset.UtcNow - run.StartedAt;
            var remaining = limit - elapsed;

            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
            
            RemainingTimeText = remaining .ToString(@"mm\:ss");

            return Page();
        }
    }
}
