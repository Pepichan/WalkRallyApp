using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;
using WalkRallyApp.Models;

namespace WalkRallyApp.Pages
{
    public class joinModel : PageModel
    {
        private readonly AppDbContext _db;

        public joinModel(AppDbContext db)
        {
            _db = db;
        }

        // 入力項目（画面のフォームと結びつく）
        [BindProperty, Required, MaxLength(50)]
        public string TeamName { get; set; } = string.Empty;

        [BindProperty, Range(1, 100)]
        public int MemberCount { get; set; }

        [BindProperty, Required, MaxLength(50)]
        public string RepresentativeName { get; set; } = string.Empty;

        [BindProperty, MaxLength(100)]
        public string? School { get; set; }

        [BindProperty, EmailAddress, MaxLength(200)]
        public string? Email { get; set; }

        [BindProperty]
        public bool Consent { get; set; } // 同意チェックボックス


        public void OnGet() // 画面表示時に呼ばれる
        {
            // 今は表示だけ（後でDB保存処理を追加）
        }

        public async Task<IActionResult> OnPostAsync() 
        {
            if (!Consent)
            {
                ModelState.AddModelError(string.Empty, "同意が必要です。");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var code = await GenerateUniqueCodeAsync();

            var team = new Team
            {
                Code = code,
                Name = TeamName,
                MemberCount = MemberCount,
                LeaderName = RepresentativeName,
                SchoolName = School,
                LeaderEmail = Email,
                ConsentAt = DateTimeOffset.UtcNow
            };

            _db.Teams.Add(team);
            await _db.SaveChangesAsync();

            return RedirectToPage("Start", new { teamId = team.Id });
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            for (var i = 0; i < 5; i++)
            {
                var code = new string(Enumerable.Range(0, 6)
                    .Select(_ => chars[random.Next(chars.Length)]).ToArray());

                var exists = await _db.Teams.AnyAsync(t => t.Code == code);
                if (!exists)
                {
                    return code;
                }
            }

            return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        }
    }
}
