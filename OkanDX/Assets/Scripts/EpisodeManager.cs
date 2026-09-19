using UnityEngine;

public class EpisodeManager : MonoBehaviour
{
    [Header("■ このエピソード用ヒントデータ")]
    [SerializeField] private HintData episodeHintData;

    private void Start()
    {
        // ★テスト用に 1,000 円追加する
        HesokuriManager.Instance.AddHesokuri(1000);

        // ★シーン開始時に HintModalUI へこのエピソードのヒントデータを渡す
        if (HintModalUI.Instance != null && episodeHintData != null)
        {
            HintModalUI.Instance.SetHintData(episodeHintData);
        }
    }

    /// <summary>
    /// 脱出成功（ギミッククリア）した時に呼び出す処理
    /// </summary>
    public void OnClearEpisode()
    {
        if (GameManager.Instance != null)
        {
            Debug.Log($"エピソード {GameManager.Instance.CurrentEpisodeNumber} クリア！");

            // 1. Easy Saveでクリア状況を自動保存
            GameManager.Instance.CompleteCurrentEpisode();

            // 2. エピソード選択画面へ戻る（またはリザルトUIを表示）
            GameManager.Instance.GoToEpisodeSelect();
        }
        else
        {
            Debug.LogError("GameManagerが見つかりません。Bootシーンから開始してください。");
        }
    }

    /// <summary>
    /// アプリを諦めてエピソード選択に戻るボタン用
    /// </summary>
    public void OnClickGiveUp()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToEpisodeSelect();
        }
    }
}