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

        public int TeamId { get; set; } //画面に渡すチームID
        public string TeamName { get; set; } = string.Empty; //画面に渡すチーム名
        public int TimeLimitMinutes { get; set; } //画面に渡す制限時間（分）

        public async Task<IActionResult> OnGetAsync(int teamId)
        {
            var team = await _db.Teams.FindAsync(teamId); //チームIDからチーム情報を取得
            if (team == null)
            {
                return RedirectToPage("Join"); //未登録なら受付へリダイレクト
            }

            var course = await EnsureDefaultCourseAsync();

            TeamId = team.Id; //画面に渡すチームIDをセット
            TeamName = team.Name;
            TimeLimitMinutes = course.TimeLimitMinutes;
            
            return Page(); //スタート画面を表示
        }

        public async Task<IActionResult> OnPostAsync(int teamId)
        {
            var team = await _db.Teams.FindAsync(teamId); //チームIDからチーム情報を取得
            if (team == null)
            {
                return RedirectToPage("Join"); //未登録なら受付へリダイレクト
            }
            var course = await EnsureDefaultCourseAsync(); //コース情報を取得（なければデフォルトコースを作成）

            var run = new Run
            {
                TeamId = team.Id, //参加チーム
                CourseId = course.Id, //参加コース
                StartedAt = DateTimeOffset.UtcNow, //開始日時
                IsFinished = false //未完了
            };

            _db.Runs.Add(run); //ラン情報を保存
            await _db.SaveChangesAsync();

            return RedirectToPage("Map", new { runId = run.Id }); //マップ画面へリダイレクト（ランIDを渡す）
        }

        private async Task<Course> EnsureDefaultCourseAsync()
        {
            var course = await _db.Courses.FirstOrDefaultAsync();
            if (course == null)
            {
                course = new Course
                {
                    NameJa = "デフォルトコース",
                    StartLatitude = 0,
                    StartLongitude = 0,
                    GoalLatitude = 0,
                    GoalLongitude = 0,
                    TimeLimitMinutes = 60,
                    GoalQrToken = "GOAL001"
                };

                _db.Courses.Add(course);
                await _db.SaveChangesAsync();
            }

            if (string.IsNullOrWhiteSpace(course.GoalQrToken))
            {
                course.GoalQrToken = "GOAL001";
                await _db.SaveChangesAsync();
            }

            // チェックポイントを追加（不足分のみ）
            var cp1 = await EnsureCheckpointAsync(course.Id, 1, CheckpointType.Quiz, "CP001", 10);
            await EnsureCheckpointAsync(course.Id, 2, CheckpointType.Photo, "CP002", 20);
            await EnsureCheckpointAsync(course.Id, 3, CheckpointType.Info, "CP003", 10);
            var cp4 = await EnsureCheckpointAsync(course.Id, 4, CheckpointType.Quiz, "CP004", 10);
            var cp5 = await EnsureCheckpointAsync(course.Id, 5, CheckpointType.Quiz, "CP005", 10);
            await EnsureCheckpointAsync(course.Id, 6, CheckpointType.Photo, "CP006", 20);

            // クイズ1（既存があれば維持）
            await EnsureChoiceQuestionAsync(cp1, "シドニーがある州は？",
                ("ニューサウスウェールズ州", true),
                ("ビクトリア州", false),
                ("クイーンズランド州", false),
                ("西オーストラリア州", false));

            // 追加クイズ
            await EnsureChoiceQuestionAsync(cp4, "オペラハウスがある都市は？",
                ("シドニー", true),
                ("メルボルン", false),
                ("キャンベラ", false),
                ("ブリスベン", false));

            await EnsureChoiceQuestionAsync(cp5, "オーストラリアの首都は？",
                ("キャンベラ", true),
                ("シドニー", false),
                ("メルボルン", false),
                ("パース", false));

            return course;
        }

        // チェックポイントを作成（存在する場合は取得）
        private async Task<Checkpoint> EnsureCheckpointAsync(int courseId, int order, CheckpointType type, string qrToken, int points)
        {
            var checkpoint = await _db.Checkpoints
                .FirstOrDefaultAsync(c => c.CourseId == courseId && c.Order == order);

            if (checkpoint != null)
            {
                return checkpoint;
            }

            checkpoint = new Checkpoint
            {
                CourseId = courseId,
                Order = order,
                Type = type,
                Lat = 0,
                Lng = 0,
                Points = points,
                QrToken = qrToken
            };

            _db.Checkpoints.Add(checkpoint);
            await _db.SaveChangesAsync();

            return checkpoint;
        }

        // 択一クイズを作成（存在する場合は維持）
        private async Task EnsureChoiceQuestionAsync(Checkpoint checkpoint, string text,
            params (string Text, bool IsCorrect)[] choices)
        {
            var exists = await _db.Questions.AnyAsync(q => q.CheckpointId == checkpoint.Id);
            if (exists)
            {
                return;
            }

            var question = new Question
            {
                CheckpointId = checkpoint.Id,
                Type = QuestionType.Choice,
                TextJa = text
            };

            _db.Questions.Add(question);
            await _db.SaveChangesAsync();

            foreach (var choice in choices)
            {
                _db.ChoiceOptions.Add(new ChoiceOption
                {
                    QuestionId = question.Id,
                    TextJa = choice.Text,
                    IsCorrect = choice.IsCorrect
                });
            }

            await _db.SaveChangesAsync();
        }
    }
}
