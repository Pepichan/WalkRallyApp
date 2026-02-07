using Microsoft.EntityFrameworkCore;
using WalkRallyApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Razor Pages を使う宣言
builder.Services.AddRazorPages();

// 既存MVC（今後削除してもOKだが、当面は残す）
builder.Services.AddControllersWithViews();

// DbContextを登録（アプリ全体でDB接続可能になる）
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Razor Pages のルーティングを有効化
app.MapRazorPages();

// 既存MVCルーティング（当面残す）
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
