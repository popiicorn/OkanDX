using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening; // DOTweenを使うために必要

public class ItemSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI要素")]
    [SerializeField] private Image itemIconImage;      // アイテム画像のImage
    [SerializeField] private Image selectOutlineImage; // 選択状態を示す外枠Image

    private Item currentItem;
    private int slotIndex;
    private InventoryManager inventoryManager;

    public Item CurrentItem => currentItem;

    public void SetupSlot(int index, InventoryManager manager)
    {
        slotIndex = index;
        inventoryManager = manager;
        ClearSlot();
    }

    /// <summary>
    /// スロットにアイテムを割り当てる
    /// </summary>
    public void SetItem(Item item)
    {
        currentItem = item;
        itemIconImage.sprite = item.icon;
        itemIconImage.enabled = true;
    }

    /// <summary>
    /// スロットを空にする
    /// </summary>
    public void ClearSlot()
    {
        currentItem = null;
        itemIconImage.sprite = null;
        itemIconImage.enabled = false;
        SetSelectState(false);
    }

    /// <summary>
    /// 選択状態の切り替え（外枠の切り替え ＋ 1.1倍拡大アニメーション）
    /// </summary>
    public void SetSelectState(bool isSelected)
    {
        // 1. 外枠の表示/非表示
        if (selectOutlineImage != null)
        {
            selectOutlineImage.enabled = isSelected;
        }

        // 2. スロット全体の拡大/縮小アニメーション
        transform.DOKill(); // 連打時の誤作動防止

        if (isSelected)
        {
            // 選択時: 0.1秒かけて1.1倍に拡大
            transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad);
        }
        else
        {
            // 解除時: 0.1秒かけて元のサイズ(1.0)に戻す
            transform.DOScale(1.0f, 0.1f).SetEase(Ease.OutQuad);
        }
    }

    // --- マウスクリック時の処理 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;
        inventoryManager.OnSelectSlot(slotIndex);
    }

    // --- マウスオーバー（ホバー開始） ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null) return;
        inventoryManager.ShowItemTooltip(currentItem, transform.position);
    }

    // --- マウスアウト（ホバー終了） ---
    public void OnPointerExit(PointerEventData eventData)
    {
        inventoryManager.HideItemTooltip();
    }
}