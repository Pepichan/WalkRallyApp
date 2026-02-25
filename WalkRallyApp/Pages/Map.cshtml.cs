using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
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
        public string RemainingTimeText { get; set; } = "00:00"; // 画面に渡す残り時間テキスト
        public List<Checkpoint> Checkpoints { get; set; } = new(); // 画面に渡すチェックポイントのリスト
        public Checkpoint? CurrentCheckPoint { get; set; } // 画面に渡す現在のチェックポイント
        public Question? CurrentQuestion { get; set; } // 画面に渡す現在の問題
        public List<ChoiceOption> CurrentChoices { get; set; } = new(); // 画面に渡す現在の選択肢のリスト
        public Checkpoint? PhotoCheckpoint { get; set; } // 画面に渡す写真CP
        public List<Announcement> Announcements { get; set; } = new(); // 直近アナウンス

        // 画面から送信される選択された選択肢ID
        [BindProperty]
        public int SelectedChoiceId { get; set; }

        // GPS送信用
        [BindProperty]
        public double? Lat { get; set; }

        [BindProperty]
        public double? Lng { get; set; }

        // QR入力
        [BindProperty]
        public string? QrInput { get; set; }

        // ゴールQR入力
        [BindProperty]
        public string? GoalQrInput { get; set; }

        // 写真ファイルを受け取るためのプロパティ
        [BindProperty]
        public IFormFile? PhotoFile { get; set; }

        // 結果表示用の一時データ（正解/不正解メッセージなど）
        [TempData]
        public string? AnswerResult { get; set; }

        // GPS/QR/写真の到達判定の結果を表示するための一時データ
        [TempData]
        public string? ReachResult { get; set; }

        // 写真提出の結果を表示するための一時データ
        [TempData]
        public string? PhotoResult { get; set; }

        // ゴール判定の結果を表示するための一時データ
        [TempData]
        public string? GoalResult { get; set; }


        public async Task<IActionResult> OnGetAsync(int runId, int? checkpointId)
        {
            var run = await _db.Runs
                .Include(r => r.Team) // チーム情報を取得
                .Include(r => r.Course) // コース情報を取得
                .FirstOrDefaultAsync(r => r.Id == runId); // ランIDから取得

            if (run == null)
            {
                return RedirectToPage("Join"); // 見つからなければ受付へ
            }

            if (run.IsFinished)
            {
                return RedirectToPage("Result", new { runId }); // 終了済みなら結果へ
            }

            TeamName = run.Team.Name; // チーム名を画面へ

            var limit = TimeSpan.FromMinutes(run.Course.TimeLimitMinutes); // 制限時間
            var elapsed = DateTimeOffset.UtcNow - run.StartedAt; // 経過時間
            var remaining = limit - elapsed; // 残り時間

            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            RemainingTimeText = remaining.ToString(@"mm\:ss"); // 表示用

            Checkpoints = await _db.Checkpoints
                .Where(c => c.CourseId == run.CourseId)
                .OrderBy(c => c.Order)
                .ToListAsync();

            CurrentCheckPoint = checkpointId.HasValue
                ? Checkpoints.FirstOrDefault(c => c.Id == checkpointId.Value)
                : Checkpoints.FirstOrDefault();
            if (CurrentCheckPoint != null && CurrentCheckPoint.Type == CheckpointType.Quiz)
            {
                CurrentQuestion = await _db.Questions
                    .FirstOrDefaultAsync(q => q.CheckpointId == CurrentCheckPoint.Id);
                if (CurrentQuestion != null)
                {
                    CurrentChoices = await _db.ChoiceOptions
                        .Where(c => c.QuestionId == CurrentQuestion.Id)
                        .ToListAsync();
                }
            }

            PhotoCheckpoint = CurrentCheckPoint?.Type == CheckpointType.Photo ? CurrentCheckPoint : null;

            Announcements = await _db.Announcements
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            return Page();
        }


        // GPS到達判定
        public async Task<IActionResult> OnPostReachGpsAsync(int runId, int checkpointId)
        {
            var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
            if (run == null)
            {
                return RedirectToPage("Join");
            }

            var checkpoint = await _db.Checkpoints.FindAsync(checkpointId);
            if (checkpoint == null || Lat == null || Lng == null)
            {
                ReachResult = "位置情報が取得できませんでした。";
                return RedirectToPage("Map", new { runId, checkpointId });
            }

            var distance = CalculateDistanceMeters(Lat.Value, Lng.Value, checkpoint.Lat, checkpoint.Lng);

            if (distance <= checkpoint.RadiusMeters)
            {
                // ? 到達保存＆スコア加算
                var added = await SaveReachAsync(run, checkpoint);
                ReachResult = added
                    ? $"到達しました！（+{checkpoint.Points}点）距離: {Math.Round(distance)}m"
                    : "すでに到達済みです。";
            }
            else
            {
                ReachResult = $"未到達です（距離 {Math.Round(distance)}m）";
            }

            return RedirectToPage("Map", new { runId, checkpointId });
        }


        // QR到達判定
        public async Task<IActionResult> OnPostReachQrAsync(int runId, int checkpointId)
        {
            var run = await _db.Runs.FirstOrDefaultAsync(r => r.Id == runId);
            if (run == null)
            {
                return RedirectToPage("Join");
            }

            var checkpoint = await _db.Checkpoints.FindAsync(checkpointId);
            if (checkpoint == null || string.IsNullOrWhiteSpace(QrInput))
            {
                ReachResult = "QRコードが読み取れませんでした。";
                return RedirectToPage("Map", new { runId, checkpointId });
            }

            if (checkpoint.QrToken == QrInput)
            {
                // ? 到達保存＆スコア加算
                var added = await SaveReachAsync(run, checkpoint);
                ReachResult = added
                    ? $"QR到達判定OK！(+{checkpoint.Points}点)"
                    : "すでに到達済みです。";
            }
            else
            {
                ReachResult = "QRコードが一致しません。";
            }

            return RedirectToPage("Map", new { runId, checkpointId });
        }


        public async Task<IActionResult> OnPostAnswerAsync(int runId, int checkpointId)
        {
            var run = await _db.Runs
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null)
            {
                return RedirectToPage("Join");
            }

            var checkpoint = await _db.Checkpoints
                .FirstOrDefaultAsync(c => c.Id == checkpointId && c.CourseId == run.CourseId);

            if (checkpoint == null)
            {
                return RedirectToPage("Map", new { runId, checkpointId });
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
            return RedirectToPage("Map", new { runId, checkpointId }); // マップにリダイレクトして結果を表示
        }

        public async Task<IActionResult> OnPostPhotoAsync(int runId, int checkpointId)
        {
            var run = await _db.Runs
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == runId);

            // ? run が無ければ Join に戻す（正しい挙動）
            if (run == null)
            {
                return RedirectToPage("Join");
            }

            // ? ファイル未選択なら Map に戻す（Join に飛ばさない）
            if (PhotoFile == null || PhotoFile.Length == 0)
            {
                PhotoResult = "ファイルが選択されていません。";
                return RedirectToPage("Map", new { runId, checkpointId });
            }

            // ? 写真用チェックポイント取得
            var photoCheckpoint = await _db.Checkpoints
                .FirstOrDefaultAsync(c => c.Id == checkpointId && c.Type == CheckpointType.Photo);

            if (photoCheckpoint == null)
            {
                PhotoResult = "写真用のチェックポイントが見つかりませんでした。";
                return RedirectToPage("Map", new { runId, checkpointId });
            }

            // ? 拡張子チェック（jpg/pngのみ許可）
            var extension = Path.GetExtension(PhotoFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(extension))
            {
                PhotoResult = "無効なファイル形式です。jpgまたはpngをアップロードしてください。";
                return RedirectToPage("Map", new { runId, checkpointId });
            }

            // ? 保存先（wwwroot/uploads）
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

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
                IsCorrect = true,
                Points = photoCheckpoint.Points,
                PhotoPath = $"/uploads/{fileName}"
            };

            _db.Submissions.Add(submission);
            run.Score += photoCheckpoint.Points;

            await _db.SaveChangesAsync();

            PhotoResult = "写真を提出しました！＋１０点";
            return RedirectToPage("Map", new { runId, checkpointId });
        }


        // ゴール到達判定（QR）
        public async Task<IActionResult> OnPostReachGoalQrAsync(int runId)
        {
            var run = await _db.Runs
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null)
            {
                return RedirectToPage("Join");
            }

            if (run.Course == null || string.IsNullOrWhiteSpace(run.Course.GoalQrToken))
            {
                GoalResult = "ゴールQRが設定されていません。";
                return RedirectToPage("Map", new { runId });
            }

            if (string.IsNullOrWhiteSpace(GoalQrInput))
            {
                GoalResult = "QRコードが読み取れませんでした。";
                return RedirectToPage("Map", new { runId });
            }

            if (run.Course.GoalQrToken != GoalQrInput)
            {
                GoalResult = "QRコードが一致しません。";
                return RedirectToPage("Map", new { runId });
            }

            if (!run.IsFinished) // ゴール到達で終了
            {
                run.IsFinished = true;
                run.FinishedAt = DateTimeOffset.UtcNow;
                run.ElapsedSeconds = (int)Math.Max(0, (run.FinishedAt.Value - run.StartedAt).TotalSeconds);
                await _db.SaveChangesAsync();
            }

            return RedirectToPage("Result", new { runId });
        }


        // ゴール到達判定（GPS）
        public async Task<IActionResult> OnPostReachGoalGpsAsync(int runId)
        {
            var run = await _db.Runs
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == runId);

            if (run == null)
            {
                return RedirectToPage("Join");
            }

            if (run.Course == null)
            {
                GoalResult = "コース情報が取得できませんでした。";
                return RedirectToPage("Map", new { runId });
            }

            if (Lat == null || Lng == null)
            {
                GoalResult = "位置情報が取得できませんでした。";
                return RedirectToPage("Map", new { runId });
            }

            // ゴール半径（要件に合わせて30m）
            const int goalRadiusMeters = 30;
            var distance = CalculateDistanceMeters(Lat.Value, Lng.Value, run.Course.GoalLatitude, run.Course.GoalLongitude);

            if (distance <= goalRadiusMeters)
            {
                // ゴール到達で終了
                if (!run.IsFinished)
                {
                    run.IsFinished = true;
                    run.FinishedAt = DateTimeOffset.UtcNow;
                    run.ElapsedSeconds = (int)Math.Max(0, (run.FinishedAt.Value - run.StartedAt).TotalSeconds);
                    await _db.SaveChangesAsync();
                }

                return RedirectToPage("Result", new { runId });
            }

            GoalResult = $"ゴール未到達です（距離 {Math.Round(distance)}m）";
            return RedirectToPage("Map", new { runId });
        }


        // 距離計算（ハバースイン）
        private static double CalculateDistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000; // 地球の半径（メートル）
            var dLat = ToRad(lat2 - lat1);
            var dLng = ToRad(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; // 距離をメートルで返す
        }

        private static double ToRad(double deg) => deg * (Math.PI / 180); // 度をラジアンに変換

        // ? 到達保存の共通処理
        private async Task<bool> SaveReachAsync(Run run, Checkpoint checkpoint)
        {
            // 既に到達済みか確認（2重加算防止）
            var exists = await _db.Submissions.AnyAsync(s =>
                s.RunId == run.Id &&
                s.CheckpointId == checkpoint.Id &&
                s.Kind == SubmissionKind.Reach);

            if (exists)
            {
                return false;
            }

            var submission = new Submission
            {
                RunId = run.Id,
                CheckpointId = checkpoint.Id,
                Kind = SubmissionKind.Reach,
                Points = checkpoint.Points,
                IsCorrect = null // 到達なので正誤は不要
            };

            _db.Submissions.Add(submission);
            run.Score += checkpoint.Points; // ? スコア加算

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
