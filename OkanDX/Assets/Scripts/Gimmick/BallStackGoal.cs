using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// BallPhysicsArea のキャラクターなどに、決めた数以上のボールを乗せたまま決めた秒数たつと成功になるギミック。
/// 乗せている間はウィンドウを出してカウントダウンし、0 になったらウィンドウを閉じて
/// 指定したオブジェクトの表示・非表示を切り替える。成功した後は何も起きない。
/// </summary>
public class BallStackGoal : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 乗せる場所")]
    [Tooltip("ボールを計算している BallPhysicsArea")]
    [SerializeField] private BallPhysicsArea area;

    [Tooltip("ボールを乗せる先（BallPhysicsArea の線の followTarget に指定したオブジェクト）")]
    [SerializeField] private RectTransform target;

    [Header("■ 成功の条件")]
    [Tooltip("この数以上乗せると、カウントダウンが始まる")]
    [SerializeField] private int requiredCount = 5;

    [Tooltip("カウントダウンの秒数（この数字から 1 まで表示する）")]
    [SerializeField] private int countdownSeconds = 5;

    [Tooltip("線やほかのボールからこの距離まで浮いていても、乗っているとみなす")]
    [SerializeField] private float contactMargin = 12f;

    [Tooltip("数が足りない状態がこの秒数続いたら、カウントダウンをやめる（揺れで一瞬浮いただけでは止めない）")]
    [SerializeField] private float dropGraceTime = 0.3f;

    [Header("■ カウントダウンの表示")]
    [Tooltip("カウントダウン中に表示するウィンドウ")]
    [SerializeField] private GameObject countdownWindow;

    [Tooltip("残り秒数を表示するテキスト")]
    [SerializeField] private TMP_Text countdownText;

    [Tooltip("ウィンドウが出るとき・消えるときの大きさ")]
    [SerializeField] private float windowStartScale = 0.5f;

    [Tooltip("ウィンドウが出るときの時間")]
    [SerializeField] private float windowPopupDuration = 0.25f;

    [Tooltip("ウィンドウが消えるときの時間")]
    [SerializeField] private float windowHideDuration = 0.15f;

    [Header("■ 成功したとき")]
    [Tooltip("非表示にするオブジェクト")]
    [SerializeField] private GameObject[] hideOnSuccess;

    [Tooltip("表示するオブジェクト")]
    [SerializeField] private GameObject[] showOnSuccess;

    private bool isCleared;
    private bool isCountingDown;
    private float remainingTime;
    private float shortageTime;
    private int displayedNumber;

    // ================================================
    // 初期化
    // ================================================
    private void Start()
    {
        if (countdownWindow != null) countdownWindow.SetActive(false);
    }

    // ================================================
    // 更新（ボールは毎フレーム動くので、乗っている数を毎フレーム調べる）
    // ================================================
    private void Update()
    {
        if (isCleared || area == null) return;

        bool isEnough = area.CountBallsOn(target, contactMargin) >= requiredCount;

        if (!isCountingDown)
        {
            if (isEnough) StartCountdown();
            return;
        }

        // 数が足りない状態が続いたら、やり直し
        shortageTime = isEnough ? 0f : shortageTime + Time.deltaTime;
        if (shortageTime >= dropGraceTime)
        {
            StopCountdown();
            return;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            Succeed();
            return;
        }

        UpdateCountdownText();
    }

    // ================================================
    // カウントダウン
    // ================================================
    // ----
    // カウントダウンを最初から始めて、ウィンドウを出す
    // ----
    private void StartCountdown()
    {
        isCountingDown = true;
        remainingTime = countdownSeconds;
        shortageTime = 0f;
        displayedNumber = -1;
        UpdateCountdownText();
        ShowWindow();
    }

    // ----
    // カウントダウンをやめて、ウィンドウを閉じる
    // ----
    private void StopCountdown()
    {
        isCountingDown = false;
        HideWindow();
    }

    // ----
    // 残り秒数を切り上げた数字を表示する（5 → 1）
    // ----
    private void UpdateCountdownText()
    {
        int number = Mathf.CeilToInt(remainingTime);
        if (number == displayedNumber) return;

        displayedNumber = number;
        if (countdownText != null) countdownText.text = number.ToString();
    }

    // ----
    // 成功：ウィンドウを閉じて、表示を切り替える
    // ----
    private void Succeed()
    {
        isCleared = true;
        isCountingDown = false;
        HideWindow();

        SetActiveAll(hideOnSuccess, false);
        SetActiveAll(showOnSuccess, true);
    }

    private static void SetActiveAll(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (var obj in objects)
        {
            if (obj != null) obj.SetActive(active);
        }
    }

    // ================================================
    // ウィンドウの演出（MessageUI と同じポヨンとした動き）
    // ================================================
    private void ShowWindow()
    {
        if (countdownWindow == null) return;

        Transform windowTransform = countdownWindow.transform;
        countdownWindow.SetActive(true);
        windowTransform.DOKill();
        windowTransform.localScale = Vector3.one * windowStartScale;
        windowTransform.DOScale(Vector3.one, windowPopupDuration)
            .SetEase(Ease.OutBack)
            .SetLink(countdownWindow);
    }

    private void HideWindow()
    {
        if (countdownWindow == null || !countdownWindow.activeSelf) return;

        Transform windowTransform = countdownWindow.transform;
        windowTransform.DOKill();
        windowTransform.DOScale(Vector3.one * windowStartScale, windowHideDuration)
            .SetEase(Ease.InBack)
            .SetLink(countdownWindow)
            .OnComplete(() => countdownWindow.SetActive(false));
    }
}
