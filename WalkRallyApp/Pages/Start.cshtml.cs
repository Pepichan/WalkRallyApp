using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;
using WalkRallyApp.Models;

namespace WalkRallyApp.Pages
{
    public class StartModel : PageModel
    {
        private readonly AppDbContext _db;

        public StartModel(AppDbContext db)
        {
            _db = db;
        }

        public int TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int TimeLimitMinutes { get; set; }

        public async Task<IActionResult> OnGetAsync(int teamId)
        {
            var team = await _db.Teams.FindAsync(teamId);
            if (team == null)
            {
                return RedirectToPage("Join");
            }

            var course = await EnsureDefaultCourseAsync();

            TeamId = team.Id;
            TeamName = team.Name;
            TimeLimitMinutes = course.TimeLimitMinutes;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int teamId)
        {
            var team = await _db.Teams.FindAsync(teamId);
            if (team == null)
            {
                return RedirectToPage("Join");
            }
            var course = await EnsureDefaultCourseAsync();

            var run = new Run
            {
                TeamId = team.Id,
                CourseId = course.Id,
                StartedAt = DateTimeOffset.UtcNow,
                IsFinished = false
            };

            _db.Runs.Add(run);
            await _db.SaveChangesAsync();

            return RedirectToPage("Map", new { runId = run.Id });
        }

        private async Task<Course> EnsureDefaultCourseAsync()
        {
            var course = await _db.Courses.FirstOrDefaultAsync();
            if (course != null)
            {
                return course;
            }
            
            course = new Course
            {
               NameJa = "デフォルトコース",
               StartLatitude = 0,
               StartLongitude = 0,
               GoalLatitude = 0,
               GoalLongitude = 0,
               TimeLimitMinutes = 60
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();

            return course;
        }
    }
}
