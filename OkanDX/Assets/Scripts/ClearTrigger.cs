using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ClearTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("■ このエピソードのデータ（通常エピソード用：空でもOK）")]
    [SerializeField] private EpisodeData currentEpisodeData;

    [Header("■ 遷移先設定")]
    [Tooltip("遷移先のエピソードデータ（手動でセットする場合）")]
    [SerializeField] private EpisodeData targetEpisodeData;

    [Tooltip("手動セットしない場合のエピソード番号（例: 1 にするとエピソード1へ）")]
    [SerializeField] private int nextEpisodeNumber = 1;

    [Header("■ リザルト画面設定")]
    [SerializeField] private string resultSceneName = "ResultScene";

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[ClearTrigger] GameManager が見つかりません。");
            return;
        }

        // ① クリア処理・自動セーブを実行
        if (currentEpisodeData != null)
        {
            GameManager.Instance.CompleteCurrentEpisode(currentEpisodeData);
        }
        else
        {
            GameManager.Instance.CompleteCurrentEpisode();
        }

        // ② ミニゲーム判定：ミニゲームならリザルトをスキップして直接次のステージへ！
        bool isMiniGame = (currentEpisodeData != null && currentEpisodeData.episodeType == EpisodeType.MiniGame)
                       || (GameManager.Instance.CurrentEpisodeData != null && GameManager.Instance.CurrentEpisodeData.episodeType == EpisodeType.MiniGame);

        if (isMiniGame)
        {
            // --- ミニゲームの場合：リザルト画面を挟まず直接遷移 ---
            if (targetEpisodeData != null)
            {
                GameManager.Instance.StartEpisode(targetEpisodeData);
            }
            else
            {
                GameManager.Instance.StartEpisode(nextEpisodeNumber);
            }
        }
        else
        {
            // --- 通常エピソードの場合：リザルト画面（ResultScene）へ遷移 ---
            SceneManager.LoadScene(resultSceneName);
        }
    }
}