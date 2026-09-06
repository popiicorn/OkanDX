using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

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
    /// 選択枠の表示切り替え（アニメーションなし）
    /// </summary>
    public void SetSelectState(bool isSelected)
    {
        if (selectOutlineImage != null)
        {
            // アニメーションは行わず、非表示/表示の切り替えのみを行う
            selectOutlineImage.enabled = isSelected;
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