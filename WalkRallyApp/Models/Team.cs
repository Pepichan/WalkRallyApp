using System.ComponentModel.DataAnnotations;
namespace WalkRallyApp.Models
// 受付で使う「班」情報をモデル化。班（受付で登録する最小単位）

{
    public class Team
    {
        // 主キー（自動採番）。DBの行を一意に識別します。
        public int Id { get; set; }


        // 参加コード（運営が配布する短いコード）。必須・最大16文字
        [Required, MaxLength(16)]
        public string Code { get; set; } = string.Empty;


        // 班名。必須・最大50文字
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;


        // 人数（1〜100の範囲でチェック）
        [Range(1, 100)]
        public int MemberCount { get; set; }


        // 代表者の氏名（PII最小限）
        [Required, MaxLength(50)]
        public string LeaderName { get; set; } = string.Empty;


        // 学校名（任意）
        [MaxLength(100)]
        public string? SchoolName { get; set; }


        // 代表者メール（任意・メール形式チェック）
        [EmailAddress, MaxLength(200)]
        public string? LeaderEmail { get; set; }


        // 規約・プライバシー同意のタイムスタンプ（同意時に設定）
        // ConsentAt は同意チェックに使うタイムスタンプ。参加時に現在時刻を入れます。
        public DateTimeOffset? ConsentAt { get; set; }


        // 班の走行（1:N）。班の走行履歴にアクセスできるようにする（後で運営画面/CSVで便利）
        public ICollection<Run> Runs { get; set; } = new List<Run>();
    }
}
