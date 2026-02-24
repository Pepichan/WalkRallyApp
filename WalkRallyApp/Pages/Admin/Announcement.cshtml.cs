using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;
using WalkRallyApp.Models;

namespace WalkRallyApp.Pages.Admin
{
    public class AnnouncementModel : PageModel
    {
        private readonly AppDbContext _db;

        public AnnouncementModel(AppDbContext db)
        {
            _db = db;
        }

        // 入力メッセージ
        [BindProperty, Required, MaxLength(200)]
        public string Message { get; set; } = string.Empty;

        // 直近の送信履歴
        public List<Announcement> Latest { get; set; } = new();

        public async Task OnGetAsync()
        {
            // 直近10件を表示
            Latest = await _db.Announcements
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // 送信内容を保存
            _db.Announcements.Add(new Announcement { Message = Message });
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
