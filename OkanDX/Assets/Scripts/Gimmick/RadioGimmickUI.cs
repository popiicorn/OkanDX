using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class RadioGimmickUI : MonoBehaviour
{
    public static RadioGimmickUI Instance { get; private set; }

    [Header("■ 画面全体オーバーレイ")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("■ メインパネル（ラジオギミック枠）")]
    [SerializeField] private Transform mainPanelTransform;
    [SerializeField] private Button mainCloseButton;

    [Header("■ ギミック構成UI要素")]
    [SerializeField] private TMP_Text frequencyText;       // 周波数表示（例: 78.4ch）
    [SerializeField] private RectTransform redBarTransform;// 赤いバー
    [SerializeField] private RectTransform blackLineBar;   // 黒いゲージの土台ライン
    [SerializeField] private EventTrigger leftArrowTrigger; // 左矢印ボタン
    [SerializeField] private EventTrigger rightArrowTrigger;// 右矢印ボタン

    [Header("■ 周波数設定")]
    [SerializeField] private float minFrequency = 70.0f;   // 最小周波数
    [SerializeField] private float maxFrequency = 90.0f;   // 最大周波数
    [SerializeField] private float startFrequency = 70.0f; // 初期周波数
    [SerializeField] private float moveSpeed = 5.0f;       // 長押し時の周波数変化スピード（1秒あたりの変化量）
    [SerializeField] private float longPressThreshold = 0.2f; // 長押しとみなすまでの待機時間（秒）

    [Header("■ 正解判定設定")]
    [Tooltip("チェックを入れると正解判定が有効になります")]
    [SerializeField] private bool hasCorrectAnswer = true;
    [Tooltip("正解の周波数（例: 78.4）")]
    [SerializeField] private float targetFrequency = 78.4f;
    [Tooltip("許容誤差（例: 0.05 で 78.4 付近を正解と判定）")]
    [SerializeField] private float tolerance = 0.05f;

    [Header("■ 正解時の演出設定")]
    [SerializeField] private Color normalTextColor = Color.blue;
    [SerializeField] private Color correctTextColor = Color.red;
    [SerializeField] private float popupScale = 1.3f;    // 膨らむ大きさ倍率
    [SerializeField] private float popupSpeed = 0.15f;   // 片道にかかる時間

    [Header("■ 正解時のギミック連動（正解の数値に合っている時だけ連動）")]
    [Tooltip("正解時に非表示にし、不正解時に再表示するオブジェクト群")]
    [SerializeField] private GameObject[] hideObjectsOnSolved;
    [Tooltip("正解時に表示し、不正解時に非表示にするオブジェクト群")]
    [SerializeField] private GameObject[] showObjectsOnSolved;

    [Header("■ モーダル表示演出")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private float currentFrequency;
    private bool isCurrentlyCorrect = false; // 現在正解の数値に合っているか
    private int pressDirection = 0; // -1: 左, 1: 右, 0: なし
    private float pressHoldTime = 0f; // ボタンを押している時間
    private bool isLongPressing = false; // 長押しモードに突入したか

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

        if (mainCloseButton != null) mainCloseButton.onClick.AddListener(ClosePanel);

        SetupButtonTriggers();

        // ★ Awakeの時点で初期値を代入しておく
        currentFrequency = startFrequency;

        HideImmediate();
    }

    private void Start()
    {
        // Awakeで初期化するため、Start側は空（または削除）でOKです
        currentFrequency = startFrequency;
    }

    private void Update()
    {
        if (pressDirection != 0 && gameObject.activeSelf)
        {
            pressHoldTime += Time.deltaTime;

            // ボタンを押してから一定時間（例: 0.2秒）経過したら「長押しモード」へ移行
            if (pressHoldTime >= longPressThreshold)
            {
                isLongPressing = true;
                float delta = pressDirection * moveSpeed * Time.deltaTime;
                SetFrequency(currentFrequency + delta);
            }
        }
    }

    private void SetupButtonTriggers()
    {
        if (leftArrowTrigger != null)
        {
            AddTriggerEvent(leftArrowTrigger, EventTriggerType.PointerDown, () => OnPointerDownArrow(-1));
            AddTriggerEvent(leftArrowTrigger, EventTriggerType.PointerUp, OnPointerUpArrow);
        }

        if (rightArrowTrigger != null)
        {
            AddTriggerEvent(rightArrowTrigger, EventTriggerType.PointerDown, () => OnPointerDownArrow(1));
            AddTriggerEvent(rightArrowTrigger, EventTriggerType.PointerUp, OnPointerUpArrow);
        }
    }

    private void AddTriggerEvent(EventTrigger trigger, EventTriggerType type, System.Action action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((_) => action());
        trigger.triggers.Add(entry);
    }

    private void OnPointerDownArrow(int dir)
    {
        pressDirection = dir;
        pressHoldTime = 0f;
        isLongPressing = false;
    }

    private void OnPointerUpArrow()
    {
        // 長押しモードに入らずに指を離した場合（＝チョンとタップした時）は、正確に 0.1 増減させる
        if (!isLongPressing && pressDirection != 0)
        {
            // 小数点の丸め誤差を防ぐため、10倍して整数で計算してから 0.1f 単位に戻す
            float step = pressDirection * 0.1f;
            float targetFreq = Mathf.Round((currentFrequency + step) * 10f) / 10f;
            SetFrequency(targetFreq);
        }

        pressDirection = 0;
        pressHoldTime = 0f;
        isLongPressing = false;
    }

    /// <summary>
    /// 周波数の更新 ＆ バー位置・テキスト反映 ＆ 正解チェック
    /// </summary>
    private void SetFrequency(float newFreq)
    {
        currentFrequency = Mathf.Clamp(newFreq, minFrequency, maxFrequency);

        // ① テキスト更新 (小数点第1位表示)
        if (frequencyText != null)
        {
            frequencyText.text = $"{currentFrequency:F1}ch";
        }

        // ② 赤いバーの位置更新
        if (redBarTransform != null && blackLineBar != null)
        {
            float ratio = (currentFrequency - minFrequency) / (maxFrequency - minFrequency);
            float lineWidth = blackLineBar.rect.width;
            float minX = -lineWidth / 2f;
            float maxX = lineWidth / 2f;
            float posX = Mathf.Lerp(minX, maxX, ratio);

            Vector2 pos = redBarTransform.anchoredPosition;
            pos.x = posX;
            redBarTransform.anchoredPosition = pos;
        }

        // ③ 状態変化＆正解判定チェック
        CheckAnswer();
    }

    private void CheckAnswer()
    {
        if (!hasCorrectAnswer) return;

        bool checkCorrect = Mathf.Abs(currentFrequency - targetFrequency) <= tolerance;

        // 【正解の数値にピッタリ合った瞬間】
        if (checkCorrect && !isCurrentlyCorrect)
        {
            isCurrentlyCorrect = true;

            if (frequencyText != null)
            {
                frequencyText.color = correctTextColor;

                frequencyText.transform.DOKill();
                frequencyText.transform.localScale = Vector3.one;
                frequencyText.transform.DOScale(Vector3.one * popupScale, popupSpeed)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        frequencyText.transform.DOScale(Vector3.one, popupSpeed).SetEase(Ease.InQuad);
                    });
            }

            ApplySolvedObjects(true);
        }
        // 【正解の数値から外れた瞬間】
        else if (!checkCorrect && isCurrentlyCorrect)
        {
            isCurrentlyCorrect = false;

            if (frequencyText != null)
            {
                frequencyText.color = normalTextColor;
                frequencyText.transform.DOKill();
                frequencyText.transform.localScale = Vector3.one;
            }

            ApplySolvedObjects(false);
        }
    }

    private void ApplySolvedObjects(bool isCorrect)
    {
        if (hideObjectsOnSolved != null)
        {
            foreach (var obj in hideObjectsOnSolved)
            {
                if (obj != null) obj.SetActive(!isCorrect);
            }
        }

        if (showObjectsOnSolved != null)
        {
            foreach (var obj in showObjectsOnSolved)
            {
                if (obj != null) obj.SetActive(isCorrect);
            }
        }
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);
        SetFrequency(currentFrequency);

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.DOFade(overlayAlpha, popupDuration).SetUpdate(true);
        }

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

    public void ClosePanel()
    {
        pressDirection = 0;

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
                });
        }
        else
        {
            HideImmediate();
        }
    }

    public void HideImmediate()
    {
        pressDirection = 0;
        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 0f;

        if (mainPanelTransform != null)
        {
            mainPanelTransform.localScale = Vector3.zero;
            mainPanelTransform.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// ボタンの OnClick から呼び出す用（正解判定 ON）
    /// </summary>
    public void OpenPanelWithCorrect()
    {
        OpenPanel(true, targetFrequency);
    }

    /// <summary>
    /// ボタンの OnClick から呼び出す用（正解判定 OFF）
    /// </summary>
    public void OpenPanelWithoutCorrect()
    {
        OpenPanel(false, targetFrequency);
    }

    /// <summary>
    /// パネルを表示（パラメータを動的に変更して開く）
    /// </summary>
    public void OpenPanel(bool enableCorrect, float targetFreq)
    {
        hasCorrectAnswer = enableCorrect;
        targetFrequency = targetFreq;

        isCurrentlyCorrect = false;

        OpenPanel();
    }
}