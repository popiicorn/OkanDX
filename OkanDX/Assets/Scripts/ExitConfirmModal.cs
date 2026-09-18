using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ExitConfirmModal : MonoBehaviour
{
    public static ExitConfirmModal Instance { get; private set; }

    [Header("■ UI参照")]
    [Tooltip("画面全域を覆う暗がりレイヤー (Image + CanvasGroup)")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;

    [Tooltip("確認ダイアログ本体のTransform (Panel)")]
    [SerializeField] private Transform dialogPanelTransform;

    [Tooltip("画面遷移用フェードパネル (白または黒の全域Image)")]
    [SerializeField] private Image sceneFadePanel;

    [Header("■ ボタン参照")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("■ 演出パラメータ")]
    [Tooltip("オーバーレイの最終アルファ値 (例: 0.6)")]
    [SerializeField] private float overlayAlpha = 0.6f;

    [Tooltip("パネル表示の拡大アニメーション時間")]
    [SerializeField] private float popupDuration = 0.25f;

    [Tooltip("パネル非表示の縮小アニメーション時間")]
    [SerializeField] private float hideDuration = 0.15f;

    [Tooltip("「はい」選択時のシーン遷移フェード時間（秒）")]
    [SerializeField] private float sceneFadeDuration = 0.5f;

    [Header("■ 遷移先設定")]
    [Tooltip("「はい」選択時に遷移するシーン名")]
    [SerializeField] private string selectSceneName = "EpisodeSelectScene";

    private bool isTransitioning = false;

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

        // ボタンのイベントリスナー設定
        if (yesButton != null) yesButton.onClick.AddListener(OnClickYes);
        if (noButton != null) noButton.onClick.AddListener(OnClickNo);

        // 初期状態は非表示
        HideImmediate();
    }

    /// <summary>
    /// 戻るボタン等から呼び出す（確認ダイアログを表示）
    /// </summary>
    public void Show()
    {
        if (isTransitioning) return;

        gameObject.SetActive(true);

        // ① オーバーレイのフェードイン
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.DOFade(overlayAlpha, popupDuration).SetUpdate(true);
        }

        // ② パネルのポヨン拡大アニメーション
        if (dialogPanelTransform != null)
        {
            dialogPanelTransform.DOKill();
            dialogPanelTransform.gameObject.SetActive(true);
            dialogPanelTransform.localScale = Vector3.one * 0.5f; // 0.5からスタート

            dialogPanelTransform.DOScale(Vector3.one, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 「いいえ」ボタン押下時（ダイアログを閉じる）
    /// </summary>
    public void OnClickNo()
    {
        if (isTransitioning) return;

        // ① オーバーレイのフェードアウト
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.DOFade(0f, hideDuration).SetUpdate(true);
        }

        // ② パネルの縮小アニメーション
        if (dialogPanelTransform != null)
        {
            dialogPanelTransform.DOKill();
            dialogPanelTransform.DOScale(Vector3.one * 0.5f, hideDuration)
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
    /// 「はい」ボタン押下時（フェードアウトしてエピソード選択画面へ遷移）
    /// </summary>
    public void OnClickYes()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (sceneFadePanel != null)
        {
            sceneFadePanel.gameObject.SetActive(true);
            sceneFadePanel.DOKill();

            // 暗く（または白く）フェードアウトさせてからシーン遷移
            sceneFadePanel.DOFade(1f, sceneFadeDuration)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SceneManager.LoadScene(selectSceneName);
                });
        }
        else
        {
            SceneManager.LoadScene(selectSceneName);
        }
    }

    /// <summary>
    /// 即時非表示（初期化用）
    /// </summary>
    public void HideImmediate()
    {
        isTransitioning = false;

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.alpha = 0f;
        }

        if (dialogPanelTransform != null)
        {
            dialogPanelTransform.DOKill();
            dialogPanelTransform.localScale = Vector3.zero;
            dialogPanelTransform.gameObject.SetActive(false);
        }

        if (sceneFadePanel != null)
        {
            sceneFadePanel.DOKill();
            Color c = sceneFadePanel.color;
            c.a = 0f;
            sceneFadePanel.color = c;
            sceneFadePanel.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}