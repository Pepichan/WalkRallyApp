using System.ComponentModel.DataAnnotations; //[Required], [Range], [MaxLength] などの属性（アノテーション）を使うための宣言
namespace WalkRallyApp.Models
// 1つのコース（スタート/ゴール、時間、名前など）を表すクラス

{
    public class Course
    {
        // 主キー（自動採番）。DBの行を一意に識別します。
        public int Id { get; set; }

        // コース名（日本語）。必須、最大100文字。
        [Required, MaxLength(100)]
        public string NameJa { get; set; } = string.Empty;

        // 任意の英語名（将来の英語切替用）
        [MaxLength(100)]
        public string? NameEn { get; set; }

        // スタート地点（緯度・経度）
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }

        // ゴール地点（緯度・経度）
        public double GoalLatitude { get; set; }
        public double GoalLongitude { get; set; }

        // 制限時間（分）。1分〜24時間（1440分）の範囲で制限。
        [Range(1, 24 * 60)]
        public int TimeLimiMinutes { get; set; }

        // 公開中フラグ（無効にして将来非表示などに使えます）
        public bool IsActive { get; set; } = true;

        // コースに含まれるチェックポイントのリスト（1対多の関係）
        public ICollection<Checkpoint> Checkpoints { get; set; } = new List<Checkpoint>();
    }
}
