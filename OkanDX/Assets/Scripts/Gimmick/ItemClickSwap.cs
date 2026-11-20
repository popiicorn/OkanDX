using UnityEngine;

/// <summary>
/// 指定したアイテムを選んだ状態でクリックすると、指定したオブジェクトを非表示にして、別のオブジェクトを表示する。
/// ClickableObject の「アイテム使用」と違い、何度でも動く（アイテムは消費しない）。
/// ClickableObject と同じオブジェクトに付ける。アイテムを選んでいないときは ClickableObject の通常の処理（メッセージなど）になる。
/// </summary>
[RequireComponent(typeof(ClickableObject))]
public class ItemClickSwap : MonoBehaviour, IClickInterceptor
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 使うアイテム")]
    [Tooltip("このアイテムを選んでクリックしたときに切り替える")]
    [SerializeField] private string requiredItemID;

    [Header("■ 切り替え")]
    [Tooltip("非表示にするオブジェクト（空なら自分自身）")]
    [SerializeField] private GameObject[] hideObjects;

    [Tooltip("表示するオブジェクト")]
    [SerializeField] private GameObject[] showObjects;

    // ================================================
    // クリック処理（ClickableObject から呼ばれる）
    // ================================================
    /// <summary>
    /// 指定したアイテムを選んでいれば、表示を切り替える
    /// </summary>
    public bool TryInterceptClick()
    {
        if (InventoryManager.Instance == null || string.IsNullOrEmpty(requiredItemID)) return false;

        Item selected = InventoryManager.Instance.SelectedItem;
        if (selected == null || selected.id != requiredItemID) return false;

        if (showObjects != null)
        {
            foreach (var obj in showObjects)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (hideObjects == null || hideObjects.Length == 0)
        {
            gameObject.SetActive(false);
        }
        else
        {
            foreach (var obj in hideObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        return true;
    }
}
