using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// ★テスト用：リリース前にこのスクリプトとシーン上のコンポーネントを削除すること
/// <summary>
/// シーン開始時に、指定した ID のアイテムをアイテムボックスへ入れておくデバッグ用コンポーネント。
/// アイテムの中身（名前・説明・アイコン）は、同じシーン内でそのアイテムを渡す ClickableObject から取得する。
/// </summary>
public class DebugStartItems : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ デバッグ設定")]
    [Tooltip("オフにするとアイテムを追加しない")]
    [SerializeField] private bool isEnabled = true;

    [Tooltip("シーン開始時に持たせるアイテムの ID（例: Item_0506）")]
    [SerializeField] private List<string> startItemIDs = new List<string>();

    // ClickableObject の private フィールド名（デバッグ用途のためリフレクションで参照する）
    private const string ITEM_DATA_FIELD_NAME = "itemData";

    // ================================================
    // 初期化
    // ================================================
    private IEnumerator Start()
    {
        if (!isEnabled) yield break;

        // InventoryManager.Start でスロットが初期化されるのを 1 フレーム待つ
        yield return null;

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[DebugStartItems] InventoryManager が見つかりません");
            yield break;
        }

        foreach (string id in startItemIDs)
        {
            if (string.IsNullOrEmpty(id)) continue;

            Item item = FindItemInScene(id);
            if (item == null)
            {
                Debug.LogWarning($"[DebugStartItems] ID [{id}] のアイテムを持つ ClickableObject がシーン内に見つかりません");
                continue;
            }

            InventoryManager.Instance.AddItem(item);
            Debug.Log($"[DebugStartItems] ★テスト用に [{item.itemName}] を追加しました");
        }
    }

    // ----
    // シーン内の ClickableObject（非表示含む）から指定 ID のアイテムデータを探す
    // ----
    private Item FindItemInScene(string id)
    {
        FieldInfo field = typeof(ClickableObject).GetField(ITEM_DATA_FIELD_NAME, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null)
        {
            Debug.LogWarning($"[DebugStartItems] ClickableObject に {ITEM_DATA_FIELD_NAME} フィールドがありません");
            return null;
        }

        ClickableObject[] objects = FindObjectsByType<ClickableObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ClickableObject obj in objects)
        {
            Item data = field.GetValue(obj) as Item;
            if (data != null && data.id == id) return data;
        }

        return null;
    }
}
