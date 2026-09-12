using UnityEngine;

[CreateAssetMenu(fileName = "EpisodeData_001", menuName = "EscapeGame/EpisodeData")]
public class EpisodeData : ScriptableObject
{
    [Header("エピソード設定")]
    public int episodeIndex = 1;              // エピソード番号 (1, 2, 3...)
    public string episodeTitle = "おかんの部屋"; // タイトル名

    [Header("リザルト表示データ")]
    public Sprite clearSprite;                // クリア時の画像
    [TextArea(3, 5)]
    public string clearText;                  // クリア時のメッセージ

    [Header("遷移設定")]
    public string nextSceneName;              // 次のエピソードのシーン名 (例: "Episode_002")

    // --- 表示用プロパティ ---
    /// <summary>「エピソード1」形式のテキスト</summary>
    public string EpisodeNumberText => $"エピソード{episodeIndex}";

    /// <summary>「おかんの部屋」タイトルテキスト</summary>
    public string EpisodeTitleText => episodeTitle;

    /// <summary>「エピソード1  おかんの部屋」繋げたテキスト（ボタン等で一括表示したい時用）</summary>
    public string FullTitleText => $"{EpisodeNumberText}  {EpisodeTitleText}";
}