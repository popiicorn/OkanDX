using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ItemPickable : MonoBehaviour, IPointerClickHandler
{
    [Header("拾えるアイテムのデータ")]
    [SerializeField] private Item itemData;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryManager.Instance == null) return;

        // ぽよんと跳ねてからインベントリへ吸い込まれる演出
        transform.DOKill();
        transform.DOScale(1.2f, 0.1f).OnComplete(() =>
        {
            transform.DOScale(0f, 0.15f).OnComplete(() =>
            {
                // アイテムボックスへ追加成功したら自身を削除
                if (InventoryManager.Instance.AddItem(itemData))
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    // 満タンで入れなかった場合は元に戻す
                    transform.localScale = Vector3.one;
                }
            });
        });
    }
}