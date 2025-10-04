using System.ComponentModel.DataAnnotations;
namespace WalkRallyApp.Models
// 択一の「選択肢」を表すクラス

{
    public class ChoiceOption
    {
        public int Id { get; set; }


        // 親の設問（外部キー + ナビゲーション）
        public int QuestionId { get; set; }
        public Question Question { get; set; } = default!;


        // 選択肢本文（日本語必須、英語は任意）
        [Required, MaxLength(300)]
        public string TextJa { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? TextEn { get; set; }


        // この選択肢が正解かどうか（単一正解運用を想定）
        public bool IsCorrect { get; set; }

    }
}
