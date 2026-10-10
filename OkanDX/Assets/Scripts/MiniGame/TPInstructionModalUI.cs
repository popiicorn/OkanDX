using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TPInstructionModalUI : MonoBehaviour
{
    public static TPInstructionModalUI Instance { get; private set; }

    [Header("■ 画面全体オーバーレイ")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("■ メインパネル")]
    [SerializeField] private Transform mainPanelTransform;

    [Header("■ 説明テキストUI（任意・インスペクターで差し替え可能）")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("■ スタートボタン")]
    [SerializeField] private Button startButton;

    [Header("■ 演出パラメータ")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private TPCatchMiniGame miniGameManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        if (startButton != null) startButton.onClick.AddListener(OnClickStart);

        HideImmediate();
    }

    /// <summary>
    /// 説明ダイアログを表示（ミニゲーム読み込み時に呼び出し）
    /// </summary>
    public void Show(TPCatchMiniGame manager, string title = "あそびかた", string desc = "カートをドラッグして、落ちてくるトイレットペーパーをたくさんキャッチしよう！\n金色のペーパーは高額へそくりGET！")
    {
        miniGameManager = manager;
        gameObject.SetActive(true);

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = desc;

        // ① オーバーレイのフェードイン
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.DOFade(overlayAlpha, popupDuration).SetUpdate(true);
        }

        // ② メインパネルのポヨン拡大表示
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
    /// 「スタート」ボタン押下時：ダイアログを閉じてゲームを開始させる
    /// </summary>
    private void OnClickStart()
    {
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.DOFade(0f, hideDuration).SetUpdate(true);
        }

        if (mainPanelTransform != null && mainPanelTransform.gameObject.activeSelf)
        {
            mainPanelTransform.DOKill();
            mainPanelTransform.DOScale(Vector3.one * 0.5f, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    HideImmediate();

                    // ダイアログが閉じ切ったらミニゲームのカウント＆生成を開始！
                    if (miniGameManager != null)
                    {
                        miniGameManager.OnInstructionClosedAndStartGame();
                    }
                });
        }
        else
        {
            HideImmediate();
            if (miniGameManager != null) miniGameManager.OnInstructionClosedAndStartGame();
        }
    }

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