using System.ComponentModel.DataAnnotations;
namespace WalkRallyApp.Models
// 目的: 班（Team）がコース（Course）を走った1回分の記録。開始/終了、スコア、提出一覧などを保持。

{
    public class Run
    {
        // 主キー（自動採番）。DBの行を一意に識別します。
        public int Id { get; set; }


        // 班（Team）との関係（多:1）
        public int TeamId { get; set; }
        public Team Team { get; set; } = default!;


        // コース（Course）との関係（多:1）
        public int CourseId { get; set; }
        public Course Course { get; set; } = default!;


        // 開始・終了
        public DateTimeOffset StartedAt { get; set; } //制限時間やゴール時に確定
        public DateTimeOffset? FinishedAt { get; set; }
        public bool IsFinished { get; set; } // IsFinishedは「終了理由が何であれ完了扱い」フラグ。


        // 経過秒（FinishedAt-StartedAt をサーバ側で確定時に保存）
        public int? ElapsedSeconds { get; set; }  // 順位計算に使用


        // 合計スコア（提出や正解に応じて加点）
        public int TotalScore { get; set; } // 順位計算に使用


        // この走行での提出（回答や写真）一覧（1:N）
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();


        // 最新位置（1:1）
        public TeamStatus? Status { get; set; }
    }
}
