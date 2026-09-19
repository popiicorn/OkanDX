using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HintModalUI : MonoBehaviour
{
    public static HintModalUI Instance { get; private set; }

    [Header("■ 画面全体オーバーレイ")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("■ メインヒントパネル")]
    [SerializeField] private Transform mainPanelTransform;

    [Header("■ 所持へそくり表示")]
    [SerializeField] private TMP_Text hesokuriCountText;

    [Header("■ ヒントボタン（最大3つ）")]
    [SerializeField] private Button[] hintButtons = new Button[3];
    [SerializeField] private TMP_Text[] hintButtonTexts = new TMP_Text[3];

    [Header("■ 詳細表示用 子パネル")]
    [SerializeField] private Transform detailPanelTransform;
    [SerializeField] private TMP_Text detailHintText;
    [SerializeField] private Button detailCloseButton;

    [Header("■ メイン閉じるボタン")]
    [SerializeField] private Button mainCloseButton;

    [Header("■ 演出パラメータ")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private HintData currentEpisodeHintData;

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

        if (mainCloseButton != null) mainCloseButton.onClick.AddListener(CloseMainPanel);
        if (detailCloseButton != null) detailCloseButton.onClick.AddListener(CloseDetailPanel);

        HideImmediate();
    }

    /// <summary>
    /// エピソードシーン開始時に各EpisodeのHintDataをセット
    /// </summary>
    public void SetHintData(HintData data)
    {
        currentEpisodeHintData = data;
    }

    /// <summary>
    /// ヒントモーダルを開く
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateUI();

        // 開く時は子パネル（詳細）を必ず非表示状態にしておく
        if (detailPanelTransform != null)
        {
            detailPanelTransform.localScale = Vector3.zero;
            detailPanelTransform.gameObject.SetActive(false);
        }

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
    /// 所持金・ボタンのアクティブ状態（グレーアウト）更新
    /// </summary>
    private void UpdateUI()
    {
        int currentMoney = HesokuriManager.Instance != null ? HesokuriManager.Instance.CurrentHesokuri : 0;

        // ★ 所持へそくりのテキスト表示（例: 所持へそくり: ￥1,000）
        if (hesokuriCountText != null)
        {
            hesokuriCountText.text = $"￥{currentMoney:N0}";
        }

        if (currentEpisodeHintData == null) return;

        // 3つのヒントボタンの状態制御
        for (int i = 0; i < hintButtons.Length; i++)
        {
            if (i < currentEpisodeHintData.hints.Length && currentEpisodeHintData.hints[i] != null)
            {
                HintItem item = currentEpisodeHintData.hints[i];
                hintButtons[i].gameObject.SetActive(true);
                hintButtons[i].onClick.RemoveAllListeners();

                bool isUnlocked = HesokuriManager.Instance != null && HesokuriManager.Instance.IsHintUnlocked(item.hintKey);

                if (isUnlocked)
                {
                    // 解放済み: 通常表示（ボタン有効）
                    hintButtons[i].interactable = true;
                    if (hintButtonTexts[i] != null) hintButtonTexts[i].text = $"{item.buttonTitle} (見る)";
                }
                else
                {
                    // 未解放時: 所持金が足りているかチェック
                    bool canAfford = currentMoney >= item.cost;

                    // 所持金で足らなければグレーアウト (interactable = false)
                    hintButtons[i].interactable = canAfford;

                    if (hintButtonTexts[i] != null)
                    {
                        // ★ ヒントボタンのテキスト表示（例: ヒント 1 (￥1,000)）
                        hintButtonTexts[i].text = $"{item.buttonTitle} (￥{item.cost:N0})";
                    }
                }

                // ボタンクリックリスナーの登録
                hintButtons[i].onClick.AddListener(() => OnClickHintButton(item));
            }
            else
            {
                // データが設定されていないボタンは非表示
                hintButtons[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ヒントボタン押下時の処理
    /// </summary>
    private void OnClickHintButton(HintItem item)
    {
        bool isUnlocked = HesokuriManager.Instance != null && HesokuriManager.Instance.IsHintUnlocked(item.hintKey);

        if (isUnlocked)
        {
            // すでに解放済みの場合は無料で何回でも見る
            OpenDetailPanel(item.hintText);
        }
        else
        {
            // 未解放の場合、へそくりを消費して解放
            if (HesokuriManager.Instance != null && HesokuriManager.Instance.ConsumeHesokuri(item.cost))
            {
                HesokuriManager.Instance.UnlockHint(item.hintKey);
                UpdateUI(); // UIを更新して解放済みに変更
                OpenDetailPanel(item.hintText);
            }
        }
    }

    /// <summary>
    /// 詳細用 子パネルを開く
    /// </summary>
    private void OpenDetailPanel(string text)
    {
        if (detailHintText != null)
        {
            detailHintText.text = text;
        }

        if (detailPanelTransform != null)
        {
            detailPanelTransform.DOKill();
            detailPanelTransform.gameObject.SetActive(true);
            detailPanelTransform.localScale = Vector3.one * 0.5f;

            detailPanelTransform.DOScale(Vector3.one, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 詳細用 子パネルを閉じる（縮小）
    /// </summary>
    public void CloseDetailPanel()
    {
        if (detailPanelTransform != null && detailPanelTransform.gameObject.activeSelf)
        {
            detailPanelTransform.DOKill();
            detailPanelTransform.DOScale(Vector3.one * 0.5f, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    detailPanelTransform.gameObject.SetActive(false);
                });
        }
    }

    /// <summary>
    /// メインヒントパネル全体を閉じる（縮小＋フェードアウト）
    /// </summary>
    public void CloseMainPanel()
    {
        // 子パネルが開いていれば先に即時閉じる
        CloseDetailPanel();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.DOFade(0f, hideDuration).SetUpdate(true);
        }

        if (mainPanelTransform != null)
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

        if (detailPanelTransform != null)
        {
            detailPanelTransform.localScale = Vector3.zero;
            detailPanelTransform.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}