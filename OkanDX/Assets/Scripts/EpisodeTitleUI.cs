using UnityEngine;
using TMPro;
using DG.Tweening;

public class EpisodeTitleUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI numberText; // 「エピソード1」用
    [SerializeField] private TextMeshProUGUI titleText;  // 「おかんの部屋」用
    [SerializeField] private CanvasGroup canvasGroup;    // フェード制御用

    [Header("演出時間設定")]
    [Tooltip("タイトルが表示され続ける時間（秒）")]
    [SerializeField] private float displayDuration = 2.0f;

    [Tooltip("フェードアウトにかかる時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    private void Start()
    {
        EpisodeData currentEpisode = GameManager.Instance != null ? GameManager.Instance.CurrentEpisode : null;

        if (currentEpisode != null && canvasGroup != null)
        {
            PlayTitleAnimation(currentEpisode);
        }
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// タイトル演出再生
    /// </summary>
    public void PlayTitleAnimation(EpisodeData episodeData)
    {
        // それぞれのテキストにセット
        if (numberText != null) numberText.text = episodeData.EpisodeNumberText;
        if (titleText != null) titleText.text = episodeData.EpisodeTitleText;

        canvasGroup.alpha = 1f;

        // n秒表示 ➔ m秒でフェードアウト
        Sequence titleSequence = DOTween.Sequence();
        titleSequence
            .AppendInterval(displayDuration)
            .Append(canvasGroup.DOFade(0f, fadeDuration))
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }
}