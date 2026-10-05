using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TPResultModalUI : MonoBehaviour
{
    public static TPResultModalUI Instance { get; private set; }

    [Header("■ 画面全体オーバーレイ")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("■ メインリザルトパネル")]
    [SerializeField] private Transform mainPanelTransform;

    [Header("■ リザルト表示用テキスト")]
    [SerializeField] private TMP_Text totalCountText;  // ① 個数テキスト
    [SerializeField] private TMP_Text goldenCountText; // (オプション)
    [SerializeField] private TMP_Text gainedMoneyText; // ② 今回の獲得額テキスト
    [SerializeField] private TMP_Text totalMoneyText;  // ③ 合計所持額テキスト

    [Header("■ カウントアップ・演出パラメータ")]
    [Tooltip("カウントアップにかける時間（秒）")]
    [SerializeField] private float countUpDuration = 0.6f;

    [Tooltip("カウントアップ完了時の拡大率（例: 1.25 = 125%拡大）")]
    [SerializeField] private float punchScaleRatio = 1.25f;

    [Tooltip("拡大アニメーションにかける時間（秒）")]
    [SerializeField] private float punchDuration = 0.2f;

    [Tooltip("次のテキストアニメーションに移るまでの待ち時間（秒）")]
    [SerializeField] private float stepDelay = 0.15f;

    [Header("■ ボタン関係")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button retryButton;

    [Header("■ 開閉演出パラメータ")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private TPCatchMiniGame miniGameManager;
    private Coroutine countUpCoroutine;

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

        if (closeButton != null) closeButton.onClick.AddListener(CloseMainPanel);
        if (retryButton != null) retryButton.onClick.AddListener(OnClickRetry);

        HideImmediate();
    }

    /// <summary>
    /// リザルト画面を開く（カウントアップ演出開始）
    /// </summary>
    public void Show(int totalCount, int goldenCount, int gainedMoney, int totalMoney, TPCatchMiniGame manager = null)
    {
        miniGameManager = manager;
        gameObject.SetActive(true);

        // 初期表示をすべて0にする
        ResetTextValues();

        // コルーチンで順番にカウントアップ開始
        if (countUpCoroutine != null) StopCoroutine(countUpCoroutine);

        // ① オーバーレイのフェードイン
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.DOFade(overlayAlpha, popupDuration).SetUpdate(true);
        }

        // ② メインパネルのポヨン拡大表示 ➔ 表示完了後にシーケンスカウントアップ開始
        if (mainPanelTransform != null)
        {
            mainPanelTransform.DOKill();
            mainPanelTransform.gameObject.SetActive(true);
            mainPanelTransform.localScale = Vector3.one * 0.5f;

            mainPanelTransform.DOScale(Vector3.one, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    countUpCoroutine = StartCoroutine(AnimateSequenceRoutine(totalCount, gainedMoney, totalMoney));
                });
        }
        else
        {
            countUpCoroutine = StartCoroutine(AnimateSequenceRoutine(totalCount, gainedMoney, totalMoney));
        }
    }

    private void ResetTextValues()
    {
        if (totalCountText != null) totalCountText.text = "0個";
        if (gainedMoneyText != null) gainedMoneyText.text = "￥0";
        if (totalMoneyText != null) totalMoneyText.text = "￥0";
    }

    /// <summary>
    /// ①個数 ➔ ②獲得額 ➔ ③総額 の順番でカウントアップ＆拡大表示するルーチン
    /// </summary>
    private IEnumerator AnimateSequenceRoutine(int targetCount, int targetGained, int targetTotal)
    {
        // --- 1. TotalCountText (個数) のカウントアップ ---
        if (totalCountText != null)
        {
            yield return StartCoroutine(CountUpValueRoutine(0, targetCount, countUpDuration, (val) =>
            {
                totalCountText.text = $"{val}個";
            }));

            // カウント完了時にn%拡縮アニメ（PunchScale）
            PlayPunchAnimation(totalCountText.transform);
            yield return new WaitForSecondsRealtime(stepDelay);
        }

        // --- 2. GainedMoneyText (今回獲得額) のカウントアップ ---
        if (gainedMoneyText != null)
        {
            yield return StartCoroutine(CountUpValueRoutine(0, targetGained, countUpDuration, (val) =>
            {
                gainedMoneyText.text = $"￥{val:N0}";
            }));

            PlayPunchAnimation(gainedMoneyText.transform);
            yield return new WaitForSecondsRealtime(stepDelay);
        }

        // --- 3. TotalMoneyText (総所持へそくり) のカウントアップ ---
        if (totalMoneyText != null)
        {
            // 開始は（目標額 - 今回獲得額）からカウントアップするとさらにリアル
            int startTotal = Mathf.Max(0, targetTotal - targetGained);

            yield return StartCoroutine(CountUpValueRoutine(startTotal, targetTotal, countUpDuration, (val) =>
            {
                totalMoneyText.text = $"￥{val:N0}";
            }));

            PlayPunchAnimation(totalMoneyText.transform);
        }
    }

    /// <summary>
    /// 数値を指定秒数かけてパラパラ増やす汎用コルーチン
    /// </summary>
    private IEnumerator CountUpValueRoutine(int startVal, int endVal, float duration, System.Action<int> onUpdate)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // イージング（OutQuad）で最後に向けて減速しながらパラパラカウント
            float easedT = 1f - (1f - t) * (1f - t);
            int currentVal = Mathf.RoundToInt(Mathf.Lerp(startVal, endVal, easedT));

            onUpdate?.Invoke(currentVal);
            yield return null;
        }

        onUpdate?.Invoke(endVal);
    }

    /// <summary>
    /// カウント完了時にテキストを n% ポンッとスカルプ拡大させるアニメ
    /// </summary>
    private void PlayPunchAnimation(Transform targetTransform)
    {
        if (targetTransform == null) return;

        targetTransform.DOKill();
        targetTransform.localScale = Vector3.one;

        // punchScaleRatio (例: 1.25なら +0.25 拡大して元の大きさへ戻る)
        Vector3 punchVector = Vector3.one * (punchScaleRatio - 1.0f);
        targetTransform.DOPunchScale(punchVector, punchDuration, 1, 0.5f).SetUpdate(true);
    }

    public void CloseMainPanel()
    {
        if (countUpCoroutine != null) StopCoroutine(countUpCoroutine);

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
                    if (miniGameManager != null) miniGameManager.OnCloseResultAndReturnMain();
                });
        }
        else
        {
            HideImmediate();
        }
    }

    private void OnClickRetry()
    {
        CloseMainPanel();
        if (miniGameManager != null) miniGameManager.StartMiniGame();
    }

    public void HideImmediate()
    {
        if (countUpCoroutine != null) StopCoroutine(countUpCoroutine);

        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 0f;

        if (mainPanelTransform != null)
        {
            mainPanelTransform.localScale = Vector3.zero;
            mainPanelTransform.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}