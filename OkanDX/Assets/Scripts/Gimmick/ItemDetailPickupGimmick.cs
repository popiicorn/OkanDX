using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// アイテム詳細パネルの中に置いたオブジェクトをクリックすると、アイテムを獲得できるギミック。
/// ItemDetailUI と同じ GameObject に付け、「どのアイテムの詳細を表示中に」「どのボタンを出して」
/// 「クリックで何のアイテムを渡すか」を entries に登録する。
/// 獲得メッセージはパネル内のメッセージウィンドウに表示し、画面のどこかをクリックすると縮小して消える。
/// </summary>
public class ItemDetailPickupGimmick : MonoBehaviour
{
    [System.Serializable]
    public class PickupEntry
    {
        [Tooltip("このアイテムの詳細を表示しているときだけ pickupButton を表示する")]
        public string detailItemID;

        [Tooltip("クリックでアイテムを獲得するボタン（詳細画像の子に置く。獲得後は非表示になる）")]
        public Button pickupButton;

        [Tooltip("クリックで獲得するアイテム")]
        public Item giveItem;

        [Tooltip("ON: 獲得メッセージを「〇〇」を\\n手に入れた！ と改行して表示する")]
        public bool breakLineInGetMessage = false;

        [HideInInspector] public bool hasPicked = false;
    }

    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 獲得できるアイテムの登録")]
    [SerializeField] private List<PickupEntry> entries = new List<PickupEntry>();

    [Header("■ 獲得時のクリック演出")]
    [SerializeField] private float pickupShrinkScale = 0.85f;
    [SerializeField] private float pickupShrinkDuration = 0.06f;

    [Header("■ 獲得メッセージ（パネル内）")]
    [Tooltip("メッセージウィンドウのルート（最初は非表示にしておく）")]
    [SerializeField] private GameObject messageWindowObject;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float messageStartScale = 0.5f;
    [SerializeField] private float messagePopupDuration = 0.25f;
    [SerializeField] private float messageHideDuration = 0.15f;

    [Tooltip("アイテム欄がいっぱいで獲得できなかったときのメッセージ")]
    [SerializeField] private string inventoryFullMessage = "これ以上持てないようだ。";

    private bool canCloseMessage = false;
    private bool isPicking = false;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        foreach (var entry in entries)
        {
            if (entry.pickupButton == null) continue;

            PickupEntry target = entry;
            entry.pickupButton.onClick.AddListener(() => OnClickPickup(target));
        }
    }

    private void OnEnable()
    {
        // 詳細パネルが開かれるたびに、表示中のアイテムに合わせてボタンを出し分ける
        RefreshPickups();
        HideMessageImmediate();
    }

    private void OnDisable()
    {
        isPicking = false;
        HideMessageImmediate();
    }

    private void OnDestroy()
    {
        foreach (var entry in entries)
        {
            if (entry.pickupButton != null) entry.pickupButton.transform.DOKill();
        }
        if (messageWindowObject != null) messageWindowObject.transform.DOKill();
    }

    private void Update()
    {
        if (canCloseMessage && Input.GetMouseButtonDown(0))
        {
            HideMessage();
        }
    }

    // ================================================
    // ボタンの出し分け
    // ================================================
    /// <summary>
    /// 現在詳細表示しているアイテムに合わせて、獲得ボタンの表示を切り替える
    /// </summary>
    public void RefreshPickups()
    {
        string currentID = null;
        if (ItemDetailUI.Instance != null && ItemDetailUI.Instance.CurrentItem != null)
        {
            currentID = ItemDetailUI.Instance.CurrentItem.id;
        }

        foreach (var entry in entries)
        {
            if (entry.pickupButton == null) continue;

            bool show = !entry.hasPicked
                && !string.IsNullOrEmpty(currentID)
                && entry.detailItemID == currentID;

            entry.pickupButton.transform.DOKill();
            entry.pickupButton.transform.localScale = Vector3.one;
            entry.pickupButton.gameObject.SetActive(show);
        }
    }

    // ================================================
    // 獲得処理
    // ================================================
    // ----
    // 獲得ボタンをクリックしたとき
    // ----
    private void OnClickPickup(PickupEntry entry)
    {
        if (isPicking || entry.hasPicked) return;
        if (InventoryManager.Instance == null || entry.giveItem == null) return;

        if (!InventoryManager.Instance.AddItem(entry.giveItem))
        {
            ShowMessage(inventoryFullMessage);
            return;
        }

        entry.hasPicked = true;
        isPicking = true;

        // ClickableObject のアイテム獲得と同じく、少し縮んでから消える
        Transform buttonTransform = entry.pickupButton.transform;
        buttonTransform.DOKill();
        buttonTransform.localScale = Vector3.one;
        buttonTransform.DOScale(pickupShrinkScale, pickupShrinkDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(buttonTransform.gameObject)
            .OnComplete(() =>
            {
                isPicking = false;
                buttonTransform.gameObject.SetActive(false);
                ShowMessage(BuildItemGetMessage(entry));
            });
    }

    // ----
    // 獲得メッセージの組み立て（ClickableObject と同じ書式）
    // ----
    private string BuildItemGetMessage(PickupEntry entry)
    {
        string itemNameText = !string.IsNullOrEmpty(entry.giveItem.itemName) ? entry.giveItem.itemName : "アイテム";
        string separator = entry.breakLineInGetMessage ? "\n" : "";
        return $"「{itemNameText}」を{separator}手に入れた！";
    }

    // ================================================
    // メッセージ表示
    // ================================================
    // ----
    // メッセージをポヨンと拡大表示（MessageUI と同じ動き）
    // ----
    private void ShowMessage(string text)
    {
        if (string.IsNullOrEmpty(text) || messageWindowObject == null) return;

        if (messageText != null) messageText.text = text;

        canCloseMessage = false;
        Transform messageTransform = messageWindowObject.transform;
        messageWindowObject.SetActive(true);
        messageTransform.DOKill();
        messageTransform.localScale = Vector3.one * messageStartScale;
        messageTransform.DOScale(Vector3.one, messagePopupDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetLink(messageWindowObject)
            .OnComplete(() => canCloseMessage = true);
    }

    // ----
    // メッセージを縮小して消す（MessageUI と同じ動き）
    // ----
    private void HideMessage()
    {
        canCloseMessage = false;
        if (messageWindowObject == null || !messageWindowObject.activeSelf) return;

        Transform messageTransform = messageWindowObject.transform;
        messageTransform.DOKill();
        messageTransform.DOScale(Vector3.one * messageStartScale, messageHideDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .SetLink(messageWindowObject)
            .OnComplete(() => messageWindowObject.SetActive(false));
    }

    // ----
    // メッセージを即時非表示
    // ----
    private void HideMessageImmediate()
    {
        canCloseMessage = false;
        if (messageWindowObject == null) return;

        messageWindowObject.transform.DOKill();
        messageWindowObject.transform.localScale = Vector3.one;
        messageWindowObject.SetActive(false);
    }
}
