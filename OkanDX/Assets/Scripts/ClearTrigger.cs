using UnityEngine;
using UnityEngine.EventSystems;

public class ClearTrigger : MonoBehaviour, IPointerClickHandler
{
    [Header("■ このエピソードのデータ（通常エピソード用：空でもOK）")]
    [SerializeField] private EpisodeData currentEpisodeData;

    [Header("■ 遷移先設定")]
    [Tooltip("遷移先のエピソードデータ（手動でセットする場合）")]
    [SerializeField] private EpisodeData targetEpisodeData;

    [Tooltip("手動セットしない場合のエピソード番号（例: 1 にするとエピソード1へ）")]
    [SerializeField] private int nextEpisodeNumber = 1;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[ClearTrigger] GameManager が見つかりません。");
            return;
        }

        // ① 現在のエピソードのクリア処理・セーブ
        if (currentEpisodeData != null)
        {
            GameManager.Instance.CompleteCurrentEpisode(currentEpisodeData);
        }
        else
        {
            GameManager.Instance.CompleteCurrentEpisode();
        }

        // ② 次のエピソードの EpisodeData を GameManager にセットして遷移！
        if (targetEpisodeData != null)
        {
            // エピソードデータが直接アタッチされている場合
            GameManager.Instance.StartEpisode(targetEpisodeData);
        }
        else
        {
            // エピソード番号指定（例: 1）で GameManager にロードさせて遷移
            GameManager.Instance.StartEpisode(nextEpisodeNumber);
        }
    }
}