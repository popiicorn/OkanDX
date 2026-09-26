using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class PictureMatchGimmickUI : MonoBehaviour
{
    public static PictureMatchGimmickUI Instance { get; private set; }

    [System.Serializable]
    public class PictureSlot
    {
        [Tooltip("絵柄を表示するButtonコンポーネント（Image付）")]
        public Button slotButton;
        [Tooltip("このスロットに表示される絵柄Image（未指定の場合はslotButtonのImage）")]
        public Image slotImage;
        [Tooltip("正解となる画像のインデックス（Pattern Spritesの要素番号: 0, 1, 2...）")]
        public int targetSpriteIndex;

        [HideInInspector] public int currentIndex = 0;
    }

    [Header("■ 画面全体オーバーレイ")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("■ メインパネル（ギミック枠）")]
    [SerializeField] private Transform mainPanelTransform;
    [SerializeField] private Button mainCloseButton;

    [Header("■ 絵柄スロット設定")]
    [Tooltip("使用する絵柄スプライトのリスト（0番, 1番, 2番...）")]
    [SerializeField] private List<Sprite> patternSprites = new List<Sprite>();

    [Tooltip("各スロット（枠）の設定リスト（任意の数設定可能）")]
    [SerializeField] private List<PictureSlot> slots = new List<PictureSlot>();

    [Header("■ スロットタップ時の拡縮アニメーション設定")]
    [Tooltip("タップ時に膨らむ大きさの倍率（例: 1.15 で1.15倍に膨らむ）")]
    [SerializeField] private float slotClickScale = 1.15f;
    [Tooltip("片道にかかる時間（秒）")]
    [SerializeField] private float slotClickDuration = 0.08f;
    [Tooltip("アニメーションの動き（イージング）")]
    [SerializeField] private Ease slotClickEase = Ease.OutQuad;

    [Header("■ 正解時のメッセージ表示設定")]
    [Tooltip("正解時に表示させるメッセージウィンドウ等の親オブジェクト（任意）")]
    [SerializeField] private GameObject messageWindowObject;
    [Tooltip("正解メッセージ表示用テキスト（TMP）")]
    [SerializeField] private TMP_Text messageText;
    [Tooltip("正解時に表示する文言")]
    [TextArea(2, 4)]
    [SerializeField] private string solvedMessage = "扉が開いた！";

    [Header("■ 正解時のギミック連動（オブジェクト操作）")]
    [Tooltip("正解時に非表示にするオブジェクト群")]
    [SerializeField] private GameObject[] hideObjectsOnSolved;
    [Tooltip("正解時に表示（出現）させるオブジェクト群")]
    [SerializeField] private GameObject[] showObjectsOnSolved;

    [Header("■ モーダル表示演出")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private bool isSolved = false;

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

        SetupSlotEvents();
        HideImmediate();
    }

    private void SetupSlotEvents()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            int index = i;
            var slot = slots[i];

            if (slot.slotImage == null && slot.slotButton != null)
            {
                slot.slotImage = slot.slotButton.GetComponent<Image>();
            }

            if (slot.slotButton != null)
            {
                slot.slotButton.onClick.AddListener(() => OnClickSlot(index));
            }
        }
    }

    /// <summary>
    /// スロットクリック時の処理（絵柄切り替え）
    /// </summary>
    private void OnClickSlot(int slotIndex)
    {
        if (isSolved || patternSprites.Count == 0) return;

        PictureSlot slot = slots[slotIndex];
        // 次の画像へ切り替え（ループ）
        slot.currentIndex = (slot.currentIndex + 1) % patternSprites.Count;
        UpdateSlotImage(slot);

        // ★ タップ時の拡縮アニメーション（インスペクターで設定可能）
        if (slot.slotButton != null)
        {
            slot.slotButton.transform.DOKill();
            slot.slotButton.transform.localScale = Vector3.one;
            slot.slotButton.transform.DOScale(Vector3.one * slotClickScale, slotClickDuration)
                .SetEase(slotClickEase)
                .OnComplete(() =>
                {
                    slot.slotButton.transform.DOScale(Vector3.one, slotClickDuration).SetEase(Ease.InQuad);
                });
        }

        CheckAnswer();
    }

    private void UpdateSlotImage(PictureSlot slot)
    {
        if (slot.slotImage != null && patternSprites.Count > slot.currentIndex)
        {
            slot.slotImage.sprite = patternSprites[slot.currentIndex];
        }
    }

    /// <summary>
    /// 正解判定チェック
    /// </summary>
    private void CheckAnswer()
    {
        if (isSolved) return;

        bool allCorrect = true;
        foreach (var slot in slots)
        {
            if (slot.currentIndex != slot.targetSpriteIndex)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            isSolved = true;
            OnSolved();
        }
    }

    private void OnSolved()
    {
        // ① メッセージウィンドウの表示＆テキスト更新
        if (messageText != null)
        {
            messageText.text = solvedMessage;
        }

        if (messageWindowObject != null)
        {
            messageWindowObject.SetActive(true);
            messageWindowObject.transform.DOKill();
            messageWindowObject.transform.localScale = Vector3.zero;
            messageWindowObject.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        // ② 連動オブジェクトの表示・非表示切り替え
        if (hideObjectsOnSolved != null)
        {
            foreach (var obj in hideObjectsOnSolved)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (showObjectsOnSolved != null)
        {
            foreach (var obj in showObjectsOnSolved)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
    }

    public void OpenPanel()
    {
        gameObject.SetActive(true);

        // 各スロットの表示を更新
        foreach (var slot in slots)
        {
            UpdateSlotImage(slot);
        }

        // メッセージウィンドウは開いた時は非表示（正解済みなら表示）
        if (messageWindowObject != null)
        {
            messageWindowObject.SetActive(isSolved);
        }

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
        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 0f;

        if (mainPanelTransform != null)
        {
            mainPanelTransform.localScale = Vector3.zero;
            mainPanelTransform.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}