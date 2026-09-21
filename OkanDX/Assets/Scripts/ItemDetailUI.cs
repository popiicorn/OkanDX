using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ItemDetailUI : MonoBehaviour
{
    public static ItemDetailUI Instance { get; private set; }

    [Header("■ 画面全体オーバーレイ")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("■ メインパネル（拡大画像枠）")]
    [SerializeField] private Transform mainPanelTransform;

    [Header("■ 表示UI要素")]
    [SerializeField] private Image detailImage;          // 拡大画像用 Image
    [SerializeField] private TMP_Text detailText;        // 説明用 Text (任意)

    [Header("■ 閉じるボタン")]
    [SerializeField] private Button mainCloseButton;

    [Header("■ 演出パラメータ")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (mainCloseButton != null) mainCloseButton.onClick.AddListener(CloseDetail);

        HideImmediate();
    }

    /// <summary>
    /// アイテムの詳細画面を表示（HintModalUIと同じアニメーション）
    /// </summary>
    public void OpenDetail(Item item)
    {
        if (item == null || !item.canInspect) return;

        // 画像とテキストの設定
        if (detailImage != null && item.detailSprite != null)
        {
            detailImage.sprite = item.detailSprite;
        }

        if (detailText != null)
        {
            detailText.text = item.detailMessage;
            detailText.gameObject.SetActive(!string.IsNullOrEmpty(item.detailMessage));
        }

        gameObject.SetActive(true);

        // ① オーバーレイのフェードイン
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.DOFade(overlayAlpha, popupDuration).SetUpdate(true);
        }

        // ② メインパネルのポヨン拡大表示 (0.5倍 -> 1.0倍, Ease.OutBack)
        if (mainPanelTransform != null)
        {
            mainPanelTransform.DOKill();
            mainPanelTransform.gameObject.SetActive(true);
            mainPanelTransform.localScale = Vector3.one * 0.5f;

            mainPanelTransform.DOScale(Vector3.one, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 詳細画面を閉じる（縮小＋フェードアウト）
    /// </summary>
    public void CloseDetail()
    {
        // ① オーバーレイのフェードアウト
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.DOFade(0f, hideDuration).SetUpdate(true);
        }

        // ② メインパネルのポヨン縮小非表示 (1.0倍 -> 0.5倍, Ease.InBack)
        if (mainPanelTransform != null && mainPanelTransform.gameObject.activeSelf)
        {
            mainPanelTransform.DOKill();
            mainPanelTransform.DOScale(Vector3.one * 0.5f, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    HideImmediate();
                });
        }
        else
        {
            HideImmediate();
        }
    }

    /// <summary>
    /// 即時非表示
    /// </summary>
    public void HideImmediate()
    {
        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 0f;

        if (mainPanelTransform != null)
        {
            mainPanelTransform.localScale = Vector3.zero;
            mainPanelTransform.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}