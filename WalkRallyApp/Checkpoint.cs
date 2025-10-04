using System.ComponentModel.DataAnnotations;
using WalkRallyApp.Models;
namespace WalkRallyApp
// コース内の各チェックポイント（緯度経度・到達半径・配点・種別・QRトークン）を表すクラス


{
    public class Checkpoint
    {
        // 主キー（自動採番）。DBの行を一意に識別します。
        public int Id { get; set; }

        // 親のコース（外部キーとナビゲーション）
        public int CourseId { get; set; }
        public Course Course { get; set; } = default!;

        // 種別：クイズ／写真／情報（Enum は Models/Enum.cs で定義済みの想定）
        public CheckpointType Type { get; set; } = CheckpointType.Info;

        // 位置（緯度・経度）
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // 到達判定の半径（メートル）。要件は30m
        [Range(1, 200)]
        public int RadiusMeters { get; set; } = 30;

        // このCPに関連する配点（MVPの既定は10点）
        [Range(0, 1000)]
        public int Points { get; set; } = 10;

        // GPSが使えない場所のフォールバック用（QRコードの埋め込み用トークン）
        [MaxLength(64)]
        public string? QRToken { get; set; }
    }
}
