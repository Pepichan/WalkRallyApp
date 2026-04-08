using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
namespace WalkRallyApp.Models
// チェックポイントにひもづく「クイズの本文・形式（択一/記述）」を表すクラス

{
    public class Question
    {
        // 主キー（自動採番）。DBの行を一意に識別します。
        public int Id { get; set; }


        // 親チェックポイント（外部キー + ナビゲーション）
        public int CheckpointId { get; set; }
        public Checkpoint Checkpoint { get; set; } = default!;


        // 設問タイプ（Choice=択一, Text=記述）※EnumはEnum.csに定義
        public QuestionType Type { get; set; } = QuestionType.Choice;


        // 設問本文（日本語必須、英語は任意）
        [Required, MaxLength(500)]
        public string TextJa { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? TextEn { get; set; }


        // 択一の選択肢（Text型では空でもOK）
        public ICollection<ChoiceOption> Choices { get; set; } = new List<ChoiceOption>();


        // 単一正解のとき、正解選択肢のIdを保持（任意）
        public int? CorrectChoiceId { get; set; }
    }
}
