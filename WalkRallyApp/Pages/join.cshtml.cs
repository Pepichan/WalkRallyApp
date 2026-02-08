using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WalkRallyApp.Pages
{
    public class joinModel : PageModel
    {
        public void OnGet() // 画面表示時に呼ばれる
        {
            // 今は表示だけ（後でDB保存処理を追加）
        }

        public void OnPost() // 「開始」ボタンを押した時に呼ばれる
        {
            // 次のステップで登録処理を実装する
        }
    }
}
