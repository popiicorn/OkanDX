using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 指定アイテムを選択中にクリックされたら、スクラッチ塗りギミックのパネルを開くトリガー。
/// アイテム未選択・別アイテムのときは指定メッセージを表示する。
/// 同じオブジェクトの ClickableObject（Examine）はクリック演出用に残し、そちらのメッセージは空にしておく。
/// </summary>
public class ScratchPaintGimmickTrigger : MonoBehaviour, IPointerClickHandler
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 開くパネル")]
    [SerializeField] private ScratchPaintGimmickUI targetPanel;

    [Header("■ 必要なアイテム")]
    [Tooltip("このアイテムを選択中にクリックするとパネルが開く")]
    [SerializeField] private string requiredItemID = "Item_0603";

    [Header("■ 指定アイテム以外でクリックしたとき")]
    [TextArea]
    [SerializeField] private string wrongItemMessage;

    [Tooltip("塗り完成後に指定アイテム以外でクリックしたときのメッセージ（空なら wrongItemMessage を使う）")]
    [TextArea]
    [SerializeField] private string wrongItemMessageAfterCompleted;

    /// <summary>
    /// クリック時の処理
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Item selected = InventoryManager.Instance != null ? InventoryManager.Instance.SelectedItem : null;

        if (selected != null && !string.IsNullOrEmpty(selected.id) && selected.id == requiredItemID)
        {
            if (targetPanel != null)
            {
                targetPanel.OpenPanel();
            }
            else
            {
                Debug.LogWarning("[ScratchPaintGimmickTrigger] targetPanel が設定されていません。");
            }
            return;
        }

        string message = wrongItemMessage;
        if (targetPanel != null && targetPanel.IsCompleted && !string.IsNullOrEmpty(wrongItemMessageAfterCompleted))
        {
            message = wrongItemMessageAfterCompleted;
        }

        if (!string.IsNullOrEmpty(message) && MessageUI.Instance != null)
        {
            MessageUI.Instance.ShowMessage(message);
        }
    }
}
