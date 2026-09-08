using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ClearTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("このエピソードのデータ")]
    [SerializeField] private EpisodeData episodeData;

    [Header("遷移先設定")]
    [SerializeField] private string resultSceneName = "ResultScene"; // リザルト画面のシーン名

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null && episodeData != null)
        {
            // クリア処理 ＆ 自動セーブ実行（GameManager側のメソッド名と統一）
            GameManager.Instance.CompleteCurrentEpisode(episodeData);

            // リザルト画面へ遷移
            SceneManager.LoadScene(resultSceneName);
        }
        else
        {
            Debug.LogWarning("GameManager または EpisodeData が設定されていません。");
        }
    }
}