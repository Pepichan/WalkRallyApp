using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Models;

namespace WalkRallyApp.Data
{
    // DBの入口（EF Coreがテーブルを管理するためのクラス）
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        // ここに「テーブル一覧」を登録する
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<ChoiceOption> ChoiceOptions => Set<ChoiceOption>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Run> Runs => Set<Run>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<TeamStatus> TeamStatuses => Set<TeamStatus>();
        public DbSet<Announcement> Announcements => Set<Announcement>();


        // DBの細かいルール（制約やリレーション）をここで指定
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Team.Codeはユニーク（重複禁止）
            b.Entity<Team>()
                .HasIndex(x => x.Code)
                .IsUnique();

            // Checkpointは「コース内のOrder」が重複しない
            b.Entity<Checkpoint>()
                .HasIndex(x => new { x.CourseId, x.Order })
                .IsUnique();

            // QRトークンはユニーク（Nullは許可）
            b.Entity<Checkpoint>()
                .HasIndex(x => x.QrToken)
                .IsUnique()
                .HasFilter("[QrToken] IS NOT NULL");

            // RunとTeamStatusの1:1リレーション設定
            b.Entity<Run>()
                .HasOne(r => r.Status)  // Run 1件につき TeamStatus 1件（最新位置のみ）
                .WithOne(s => s.Run)
                .HasForeignKey<TeamStatus>(s => s.RunId);

            // Submissions -> Run は Cascade（Run削除で提出も消す）
            b.Entity<Submission>()
                .HasOne(s => s.Run)
                .WithMany(r => r.Submissions)
                .HasForeignKey(s => s.RunId)
                .OnDelete(DeleteBehavior.Cascade);

            // Submissions -> Checkpoint は NoAction（連鎖削除しない）
            b.Entity<Submission>()
                .HasOne(s => s.Checkpoint)
                .WithMany()
                .HasForeignKey(s => s.CheckpointId)
                .OnDelete(DeleteBehavior.NoAction);

            // 既定値（DB側での初期値）
            b.Entity<Checkpoint>().Property(x => x.RadiusMeters).HasDefaultValue(30);
            b.Entity<Checkpoint>().Property(x => x.Points).HasDefaultValue(10);
            b.Entity<Run>().Property(x => x.Score).HasDefaultValue(0);
        }
    }
}