namespace WalkRallyApp.Models
{
    //最新位置のみ（プライバシー最小化のため、位置履歴ではなく「最新の位置」だけを保持。）
    public class TeamStatus
    {
        public int Id { get; set; }


        // 走行（Run）との関係（1:1）最新位置を参加中のみ更新し、運営ダッシュボードに表示。
        public int RunId { get; set; }
        public Run Run { get; set; } = default!;

        // 位置情報
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTimeOffset? LastUpdatedAt { get; set; }
    }
}
