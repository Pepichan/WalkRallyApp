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
            var course = await _db.Courses.FirstOrDefaultAsync(); //コースが存在するか確認
            if (course == null)
            {
                course = new Course
                {
                    NameJa = "デフォルトコース",
                    StartLatitude = 0,
                    StartLongitude = 0,
                    GoalLatitude = 0,
                    GoalLongitude = 0,
                    TimeLimitMinutes = 60
                };

                _db.Courses.Add(course); //新規コースを追加
                await _db.SaveChangesAsync();
            }

            // ✅ デフォルトのチェックポイントを追加（MVP用の仮データ）
            var hasCheckpoint = await _db.Checkpoints.AnyAsync(c => c.CourseId == course.Id);
            if (!hasCheckpoint)
            {
                _db.Checkpoints.AddRange( new Checkpoint
                {
                        CourseId = course.Id,
                        Order = 1,
                        Type = CheckpointType.Quiz,
                        Lat = 0,
                        Lng = 0,
                        Points = 10,
                        QrToken = "CP001"
                });

                await _db.SaveChangesAsync();
            }

            // ✅ CP#1 にクイズを追加
            var quizCheckpoint = await _db.Checkpoints
                .FirstAsync(c => c.CourseId == course.Id && c.Order == 1);

            var hasQuestion = await _db.Questions.AnyAsync(q => q.CheckpointId == quizCheckpoint.Id);

            if (!hasQuestion)
            {
                var question = new Question
                {
                    CheckpointId = quizCheckpoint.Id,
                    Type = QuestionType.Choice,
                    TextJa = "シドニーがある州は？"

                };

                _db.Questions.Add(question);
                await _db.SaveChangesAsync();

                _db.ChoiceOptions.AddRange(
                    new ChoiceOption { QuestionId = question.Id, TextJa = "ニューサウスウェールズ州", IsCorrect = true },
                    new ChoiceOption { QuestionId = question.Id, TextJa = "ビクトリア州", IsCorrect = false },
                    new ChoiceOption { QuestionId = question.Id, TextJa = "クイーンズランド州", IsCorrect = false },
                    new ChoiceOption { QuestionId = question.Id, TextJa = "西オーストラリア州", IsCorrect = false }
                );

                await _db.SaveChangesAsync();
            }

            return course; //新規作成したコースを返す
        }
    }
}
