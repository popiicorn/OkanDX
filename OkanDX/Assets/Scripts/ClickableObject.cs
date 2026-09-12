using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ClickableObject : MonoBehaviour, IPointerClickHandler
{
    public enum ObjectType
    {
        Examine,    // ① 調べるだけ（メッセージ表示）
        Item,       // ② アイテム取得（4つの後処理対応）
        StateChange,// ③ 状態変化（条件なしでのドア開閉・画像差し替えなど）
        ItemUse,    // ④ 指定アイテム使用による状態変化・ギミック解除
        Okan        // ⑤ おかん（クリックでクリア＆自動セーブ）
    }

    public enum ItemPostAction
    {
        Hide,                   // 消える
        RemainAndShowText,      // 残って2回目以降テキスト
        ChangeSpriteAndShowText,// 画像切り替わって2回目以降テキスト
        SwapObject              // 消えて別オブジェクト表示
    }

    [Header("■ 基本設定")]
    [SerializeField] private ObjectType objectType = ObjectType.Examine;

    // -------------------------------------------------------------
    // 【調べる】タイプ用の設定
    // -------------------------------------------------------------
    [Header("■ 【調べる】用の設定")]
    [TextArea(2, 5)]
    [SerializeField] private string examineMessage = "特に変わったところはないようだ。";

    // -------------------------------------------------------------
    // 【アイテム取得】タイプ用の設定
    // -------------------------------------------------------------
    [Header("■ 【アイテム取得】用の設定")]
    [SerializeField] private Item itemData;
    [SerializeField] private ItemPostAction itemPostAction = ItemPostAction.Hide;

    [Header(" └ 画像切り替え用 (ChangeSprite)")]
    [SerializeField] private Sprite changedSprite;

    [Header(" └ 別オブジェクト出現用 (SwapObject)")]
    [SerializeField] private GameObject newObject;

    [Header(" └ 2回目以降のメッセージ (Remain / ChangeSprite)")]
    [TextArea(2, 5)]
    [SerializeField] private string inspectAfterGetMessage;

    private bool hasGottenItem = false;

    // -------------------------------------------------------------
    // 【状態変化 / アイテム使用】タイプ用の設定
    // -------------------------------------------------------------
    [Header("■ 【状態変化・アイテム使用】共通設定")]
    [Tooltip("変化後の画像（画像差し替えで表現する場合）")]
    [SerializeField] private Sprite changedStateSprite;
    [SerializeField] private Image targetUIImage;

    [Tooltip("変化前のオブジェクト（非表示にするオブジェクト/自分自身など）")]
    [SerializeField] private GameObject beforeStateObject;

    [Tooltip("変化後のオブジェクト（出現させる新しいオブジェクト）")]
    [SerializeField] private GameObject afterStateObject;

    [Header("■ 【アイテム使用】専用設定")]
    [Tooltip("使用に必要なアイテムのID（例: key_01）")]
    [SerializeField] private string requiredItemID = "key_01";

    [Tooltip("使用時にインベントリからそのアイテムを削除するか")]
    [SerializeField] private bool consumeItemOnUse = true;

    [Header(" └ メッセージ設定")]
    [TextArea(2, 5)]
    [SerializeField] private string wrongItemMessage = "鍵がかかっている。";
    [TextArea(2, 5)]
    [SerializeField] private string successMessage = "鍵を使って開けた！";
    [TextArea(2, 5)]
    [SerializeField] private string afterUnlockedMessage = "すでに開いている。";

    private bool isStateChanged = false;

    // -------------------------------------------------------------
    // 【おかん】タイプ用の設定
    // -------------------------------------------------------------
    [Header("■ 【おかん】用の設定")]
    [SerializeField] private EpisodeData episodeData;
    [SerializeField] private string resultSceneName = "ResultScene";

    private Image imageComponent;

    private void Awake()
    {
        imageComponent = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ExecuteClickAction();
    }

    private void OnMouseDown()
    {
        ExecuteClickAction();
    }

    private void ExecuteClickAction()
    {
        switch (objectType)
        {
            case ObjectType.Examine:
                AnimateClick();
                ShowMessage(examineMessage);
                break;

            case ObjectType.Item:
                OnItemClick();
                break;

            case ObjectType.StateChange:
                AnimateClick();
                OnStateChangeClick();
                break;

            case ObjectType.ItemUse:
                AnimateClick();
                OnItemUseClick();
                break;

            case ObjectType.Okan:
                AnimateClick();
                OnOkanClick();
                break;
        }
    }

    private void AnimateClick()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        transform.DOScale(0.85f, 0.08f).OnComplete(() =>
        {
            transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
        });
    }

    private void OnItemClick()
    {
        if (hasGottenItem)
        {
            AnimateClick();
            ShowMessage(inspectAfterGetMessage);
            return;
        }

        if (InventoryManager.Instance == null || itemData == null) return;

        transform.DOKill();
        transform.localScale = Vector3.one;

        Sequence getSequence = DOTween.Sequence();
        getSequence
            .Append(transform.DOScale(0.85f, 0.06f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(0f, 0.0f).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                bool isAdded = InventoryManager.Instance.AddItem(itemData);

                if (isAdded)
                {
                    hasGottenItem = true;
                    ApplyItemPostAction();
                }
                else
                {
                    transform.localScale = Vector3.one;
                }
            });
    }

    private void ApplyItemPostAction()
    {
        switch (itemPostAction)
        {
            case ItemPostAction.Hide:
                gameObject.SetActive(false);
                break;

            case ItemPostAction.RemainAndShowText:
                transform.localScale = Vector3.one;
                break;

            case ItemPostAction.ChangeSpriteAndShowText:
                if (imageComponent != null && changedSprite != null)
                {
                    imageComponent.sprite = changedSprite;
                }
                transform.localScale = Vector3.one;
                break;

            case ItemPostAction.SwapObject:
                if (newObject != null)
                {
                    newObject.SetActive(true);
                }
                gameObject.SetActive(false);
                break;
        }
    }

    private void OnStateChangeClick()
    {
        if (isStateChanged) return;
        ChangeState();
    }

    private void OnItemUseClick()
    {
        if (isStateChanged)
        {
            ShowMessage(afterUnlockedMessage);
            return;
        }

        // 選択中のアイテムを取得
        Item selected = InventoryManager.Instance != null ? InventoryManager.Instance.SelectedItem : null;

        // 【成功】選択アイテムの id と requiredItemID が一致する場合
        if (selected != null && !string.IsNullOrEmpty(selected.id) && selected.id == requiredItemID)
        {
            if (consumeItemOnUse)
            {
                InventoryManager.Instance.RemoveSelectedItem();
            }

            ShowMessage(successMessage);
            ChangeState();
        }
        else
        {
            ShowMessage(wrongItemMessage);
        }
    }

    private void ChangeState()
    {
        isStateChanged = true;

        if (beforeStateObject != null || afterStateObject != null)
        {
            if (beforeStateObject != null)
            {
                beforeStateObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            if (afterStateObject != null)
            {
                afterStateObject.SetActive(true);
            }
        }
        else if (targetUIImage != null && changedStateSprite != null)
        {
            targetUIImage.sprite = changedStateSprite;
        }
    }

    private void OnOkanClick()
    {
        if (GameManager.Instance != null)
        {
            if (episodeData != null)
            {
                GameManager.Instance.CompleteCurrentEpisode(episodeData);
            }
            SceneManager.LoadScene(resultSceneName);
        }
    }

    private void ShowMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        if (MessageUI.Instance != null)
        {
            MessageUI.Instance.ShowMessage(msg);
        }
        else
        {
            Debug.LogWarning("MessageUIのInstanceが見つかりません。");
        }
    }
}