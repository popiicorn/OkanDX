using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailUI : MonoBehaviour
{
    public static ItemDetailUI Instance { get; private set; }

    [Header("UI要素")]
    [SerializeField] private GameObject detailPanel;      // 拡大画面の親パネル
    [SerializeField] private Image detailImage;          // 拡大画像用 Image
    [SerializeField] private TextMeshProUGUI detailText; // 説明用 Text（使わない場合は未割当でOK）
    [SerializeField] private Button closeButton;         // 閉じるボタン（画面全体クリック用でも可）

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

        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDetail);
        }
    }

    /// <summary>
    /// アイテムの詳細画面を表示
    /// </summary>
    public void OpenDetail(Item item)
    {
        if (item == null || !item.canInspect) return;

        if (detailImage != null && item.detailSprite != null)
        {
            detailImage.sprite = item.detailSprite;
        }

        if (detailText != null)
        {
            detailText.text = item.detailMessage;
            detailText.gameObject.SetActive(!string.IsNullOrEmpty(item.detailMessage));
        }

        if (detailPanel != null)
        {
            detailPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 詳細画面を閉じる
    /// </summary>
    public void CloseDetail()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }
}