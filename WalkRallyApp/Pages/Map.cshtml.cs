using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;
using WalkRallyApp.Models;

namespace WalkRallyApp.Pages
{
    public class MapModel : PageModel
    {
        private readonly AppDbContext _db;
        
        public MapModel(AppDbContext db)
        {
            _db = db;
        }

        public string TeamName { get; set; } = string.Empty; // 画面に渡すチーム名
        public string RemainingTimeText { get; set; } = "00:00"; // 画面に渡す残り時間テキスト（初期値は00:00）
        public List<Checkpoint> Checkpoints { get; set; } = new(); // 画面に渡すチェックポイントのリスト

        public async Task<IActionResult> OnGetAsync(int runId)
        {
            var run = await _db.Runs
                .Include(r => r.Team) // チーム情報も一緒に取得
                .Include(r => r.Course) // コース情報も一緒に取得（時間制限のため）
                .FirstOrDefaultAsync(r => r.Id == runId); // ランIDからラン情報を取得

            if (run == null)
            {
                return RedirectToPage("Join"); // ランが見つからない場合は受付へリダイレクト
            }

            TeamName = run.Team.Name; //チーム名を画面に渡す

            var limit = TimeSpan.FromMinutes(run.Course.TimeLimitMinutes); // コースの時間制限をTimeSpanに変換
            var elapsed = DateTimeOffset.UtcNow - run.StartedAt; // 経過時間を計算（現在時刻 - 開始時刻）
            var remaining = limit - elapsed; // 残り時間を計算（制限時間 - 経過時間）

            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero; // 残り時間がマイナスになる場合は0にする（時間切れ扱い）
            }
            
            RemainingTimeText = remaining .ToString(@"mm\:ss"); // 残り時間を「分:秒」形式のテキストに変換

            // ? チェックポイント一覧を取得
            Checkpoints = await _db.Checkpoints
                .Where(c => c.CourseId == run.CourseId) // ランのコースIDに紐づくチェックポイントを取得
                .OrderBy(c => c.Order) // チェックポイントの順番でソート
                .ToListAsync();

            return Page(); // マップ画面を表示
        }
    }
}
