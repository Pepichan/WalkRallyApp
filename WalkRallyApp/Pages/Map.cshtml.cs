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
        private readonly IWebHostEnvironment _env;

        public MapModel(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public string TeamName { get; set; } = string.Empty; // 画面に渡すチーム名
        public string RemainingTimeText { get; set; } = "00:00"; // 画面に渡す残り時間テキスト（初期値は00:00）
        public List<Checkpoint> Checkpoints { get; set; } = new(); // 画面に渡すチェックポイントのリスト


        public Checkpoint? CurrentCheckPoint { get; set; } // 画面に渡す現在のチェックポイント（初期値はnull）
        public Question? CurrentQuestion { get; set; } // 画面に渡す現在の問題（初期値はnull）
        public List<ChoiceOption> CurrentChoices { get; set; } = new(); // 画面に渡す現在の選択肢のリスト（初期値は空リスト）
        public Checkpoint? PhotoCheckpoint { get; set; } // 画面に渡す写真用チェックポイント（初期値はnull）


        // 画面から送信される選択された選択肢ID
        [BindProperty]
        public int SelectedChoiceId { get; set; }

        // ? 写真ファイルを受け取るためのプロパティ
        [BindProperty]
        public IFormFile? PhotoFile { get; set; }

        // 結果表示用の一時データ（正解/不正解メッセージなど）
        [TempData]
        public string? AnswerResult { get; set; }

        [TempData]
        public string? PhotoResult { get; set; }


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

            RemainingTimeText = remaining.ToString(@"mm\:ss"); // 残り時間を「分:秒」形式のテキストに変換

            // ? チェックポイント一覧を取得
            Checkpoints = await _db.Checkpoints
                .Where(c => c.CourseId == run.CourseId) // ランのコースIDに紐づくチェックポイントを取得
                .OrderBy(c => c.Order) // チェックポイントの順番でソート
                .ToListAsync();

            CurrentCheckPoint = Checkpoints.FirstOrDefault(); // とりあえず最初のチェックポイントを現在のチェックポイントとしてセット（後で進行に応じて変更する）
            if (CurrentCheckPoint != null)
            {
                CurrentQuestion = await _db.Questions
                    .FirstOrDefaultAsync(q => q.CheckpointId == CurrentCheckPoint.Id); // 現在のチェックポイントに紐づく問題を取得
                if (CurrentQuestion != null)
                {
                    CurrentChoices = await _db.ChoiceOptions
                        .Where(c => c.QuestionId == CurrentQuestion.Id) // 現在の問題IDに紐づく選択肢を取得
                        .ToListAsync();
                }
            }

            // ? 写真用のCP（最初のPhoto）
            PhotoCheckpoint = Checkpoints.FirstOrDefault(c => c.Type == CheckpointType.Photo);

            return Page(); // マップ画面を表示
        }

        public async Task<IActionResult> OnPostAnswerAsync(int runId)
        {
            var run = await _db.Runs
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null)
            {
                return RedirectToPage("Join");
            }

            var checkpoint = await _db.Checkpoints
                .OrderBy(c => c.Order) // チェックポイントの順番でソート
                .FirstOrDefaultAsync(c => c.CourseId == run.CourseId); // とりあえず最初のチェックポイントを取得（後で進行に応じて変更する）

            if (checkpoint == null)
            {
                return RedirectToPage("Map", new { runId }); // チェックポイントが見つからない場合はマップにリダイレクト
            }

            var choice = await _db.ChoiceOptions
                 .FirstOrDefaultAsync(c => c.Id == SelectedChoiceId); // 選択された選択肢が現在のチェックポイントの問題に紐づいているか確認

            var isCorrect = choice != null && choice.IsCorrect; // 選択された選択肢が正解かどうか
            var points = isCorrect ? checkpoint.Points : 0; // 正解ならチェックポイントの点数、そうでなければ0点

            var submission = new Submission
            {
                RunId = run.Id,
                CheckpointId = checkpoint.Id,
                Kind = SubmissionKind.Quiz,
                IsCorrect = isCorrect,
                Points = points
            };

            _db.Submissions.Add(submission); // 提出情報をデータベースに追加
            run.Score += points; // ランのスコアに加算

            await _db.SaveChangesAsync();

            AnswerResult = isCorrect ? "正解！＋１０点" : "不正解！（加点なし）"; // 結果表示用のメッセージをセット
            return RedirectToPage("Map", new { runId }); // マップにリダイレクトして結果を表示
        }

        public async Task<IActionResult> OnPostPhotoAsync(int runId)
        {
            var run = await _db.Runs
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null || PhotoFile == null)
            {
                return RedirectToPage("Join");
            }

            // ? 写真用チェックポイント取得
            var photoCheckpoint = await _db.Checkpoints
                .Where(c => c.CourseId == run.CourseId && c.Type == CheckpointType.Photo) // ランのコースIDに紐づく写真用チェックポイントを取得
                .OrderBy(c => c.Order) // チェックポイントの順番でソート
                .FirstOrDefaultAsync(); // とりあえず最初の写真用チェックポイントを取得

            if (photoCheckpoint == null)
            {
                PhotoResult = "写真用のチェックポイントが見つかりませんでした。"; // 結果表示用のメッセージをセット
                return RedirectToPage("Map", new { runId }); // 写真用チェックポイントが見つからない場合はマップにリダイレクト
            }

            // ? 拡張子チェック（jpg/pngのみ許可）
            var extension = Path.GetExtension(PhotoFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(extension))
            {
                PhotoResult = "無効なファイル形式です。jpgまたはpngをアップロードしてください。"; // 結果表示用のメッセージをセット
                return RedirectToPage("Map", new { runId }); // 無効なファイル形式の場合はマップにリダイレクト
            }

            // ? 保存先（wwwroot/uploads）
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads); // ディレクトリが存在しない場合は作成

            // ? ファイル名をユニークに
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploads, fileName);

            // ? ファイル保存
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await PhotoFile.CopyToAsync(stream);
            }

            // ? Submission 保存
            var submission = new Submission
            {
                RunId = run.Id,
                CheckpointId = photoCheckpoint.Id,
                Kind = SubmissionKind.Photo,
                IsCorrect = true, // 写真提出はとりあえず常に正解扱い（後で審査機能を追加する場合は変更）
                Points = photoCheckpoint.Points, // チェックポイントの点数を加算
                PhotoPath = $"/uploads/{fileName}" // 保存したファイルのパスを保存
            };

            _db.Submissions.Add(submission); // 提出情報をデータベースに追加
            run.Score += photoCheckpoint.Points; // ランのスコアに加算

            await _db.SaveChangesAsync();

            PhotoResult = "写真を提出しました！＋１０点"; // 結果表示用のメッセージをセット
            return RedirectToPage("Map", new { runId }); // マップにリダイレクトして結果を表示
        }
    }
}
