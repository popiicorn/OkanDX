using UnityEngine;

[CreateAssetMenu(fileName = "EpisodeData_001", menuName = "EscapeGame/EpisodeData")]
public class EpisodeData : ScriptableObject
{
    [Header("エピソード設定")]
    public int episodeIndex = 1;             // エピソード番号 (1, 2, 3...)
    public string episodeTitle = "Episode 1"; // タイトル

    [Header("リザルト表示データ")]
    public Sprite clearSprite;               // クリア時の画像
    [TextArea(3, 5)]
    public string clearText;                 // クリア時のメッセージ

    [Header("遷移設定")]
    public string nextSceneName;             // 次のエピソードのシーン名 (例: "Episode_002")
}