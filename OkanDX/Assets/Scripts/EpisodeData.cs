using UnityEngine;

public enum EpisodeType
{
    NormalEscape, // 通常の脱出ゲーム
    MiniGame      // ミニゲーム
}

[CreateAssetMenu(fileName = "EpisodeData_001", menuName = "EscapeGame/EpisodeData")]
public class EpisodeData : ScriptableObject
{
    [Header("エピソード設定")]
    [Tooltip("ゲームの全体の進行順序（クリア判定用通し番号: 1, 2, 3, 4, 5...）")]
    public int episodeIndex = 1;

    // ★追加: ミニゲームの場合の表示用番号（1, 2, 3...）
    [Tooltip("ミニゲーム時の表示番号（ミニゲーム1, ミニゲーム2...）")]
    public int miniGameIndex = 1;

    public string episodeTitle = "おかんの部屋"; // タイトル名

    [Header("エピソードの種類")]
    public EpisodeType episodeType = EpisodeType.NormalEscape;

    [Header("リザルト表示データ")]
    public Sprite clearSprite;                // クリア時の画像
    [TextArea(3, 5)]
    public string clearText;                  // クリア時のメッセージ

    public string nextButtonText = "次へ";

    [Header("遷移設定")]
    public string nextSceneName;              // 次のシーン名

    // --- 表示用プロパティ ---
    public string EpisodeNumberText
    {
        get
        {
            // ★ ミニゲームの場合は miniGameIndex を使って "ミニゲーム1" と表示
            if (episodeType == EpisodeType.MiniGame)
            {
                return $"ミニゲーム{miniGameIndex}";
            }

            // 通常エピソードはそのまま "エピソード5" など
            return $"エピソード{episodeIndex}";
        }
    }

    public string EpisodeTitleText => episodeTitle;

    public string FullTitleText => $"{EpisodeNumberText}  {EpisodeTitleText}";
}