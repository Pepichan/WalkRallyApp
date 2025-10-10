using System.ComponentModel.DataAnnotations;
namespace WalkRallyApp.Models
{
    //チェックポイントに対する提出（回答/写真）
    public class Submission
    {
        // 主キー（自動採番）。DBの行を一意に識別します。
        public int Id { get; set; } 


        //走行（Run）との関係（多:1）
        public int RunId { get; set; }
        public Run Run { get; set; } = default!;


        //チェックポイント（Checkpoint）との関係（多:1）
        public int CheckpointId { get; set; }
        public Checkpoint Checkpoint { get; set; } = default!;


        //提出の種類（クイズ or 写真）
        public SubmissionKind Kind { get; set; }


        //クイズの正誤（写真提出時は null）
        public bool? IsCorrect { get; set; }


        //この提出で得た点数（正解/提出完了などで加点）
        public int Points { get; set; }


        // 記述回答（択一の場合は使わない）
        [MaxLength(1000)]
        public string? AnswerText { get; set; }


        //写真の保存パス（相対・任意）
        [MaxLength(260)]
        public string? PhotoPath { get; set; }


        //提出時の位置（任意）
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }


        // 作成時刻（既定でUTC現在時刻）
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
