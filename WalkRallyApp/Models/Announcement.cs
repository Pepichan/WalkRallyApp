using System.ComponentModel.DataAnnotations;

namespace WalkRallyApp.Models
{

    // アナウンスの本文と送信時刻を保存するためのモデル
    public class Announcement
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Message { get; set; } = string.Empty;

        // 送信時刻（UTC）
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
