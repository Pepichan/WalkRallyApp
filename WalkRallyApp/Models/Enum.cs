namespace WalkRallyApp.Models;
//アプリ内で使う種別（チェックポイント種別、問題種別、提出種別）を「列挙型」で表現します。
//列挙型は、固定の選択肢を「安全に」「読みやすく」コード化するための型です。


//チェックポイントの種類（クイズ/写真/情報）
public enum  CheckpointType
{
    Quiz = 0,
    Photo = 1,
    Info = 2
}

//問題の種類（選択式/記述式）
public enum QuestionType
{
    Choice = 0,
    Answer = 1
}

//提出の種類（クイズ回答/写真提出）
public enum SubmissionType
{
    QuizAnswer = 0,
    PhotoUpload = 1
}

// 提出の種類（Submission.cs で使用）
public enum SubmissionKind
{
    Quiz = 0,
    Photo = 1,
    Reach = 2
}
