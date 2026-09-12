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
        StateChange,// ③ 状態変化（ドア開閉・箱開け・スイッチ・画像差し替えなど）
        Okan        // ④ おかん（クリックでクリア＆自動セーブ）
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
    // 【アイテム】タイプ用の設定
    // -------------------------------------------------------------
    [Header("■ 【アイテム】用の設定")]
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
    // 【状態変化】タイプ用の設定（ドア、箱、スイッチ、ギミック等）
    // -------------------------------------------------------------
    [Header("■ 【状態変化】用の設定")]
    [Tooltip("変化後の画像（画像差し替えで表現する場合）")]
    [SerializeField] private Sprite changedStateSprite;
    [SerializeField] private Image targetUIImage;

    [Tooltip("変化前のオブジェクト（非表示にするオブジェクト）")]
    [SerializeField] private GameObject beforeStateObject;

    [Tooltip("変化後のオブジェクト（表示するオブジェクト）")]
    [SerializeField] private GameObject afterStateObject;

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

    // Canvas UI 用クリック検知
    public void OnPointerClick(PointerEventData eventData)
    {
        ExecuteClickAction();
    }

    // 2D Collider (Sprite) 用クリック検知
    private void OnMouseDown()
    {
        ExecuteClickAction();
    }

    /// <summary>
    /// クリック時のメイン分岐処理
    /// </summary>
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

            case ObjectType.Okan:
                AnimateClick();
                OnOkanClick();
                break;
        }
    }

    // -------------------------------------------------------------
    //  アニメーション
    // -------------------------------------------------------------
    private void AnimateClick()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        transform.DOScale(0.85f, 0.08f).OnComplete(() =>
        {
            transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
        });
    }

    // -------------------------------------------------------------
    //  ObjectType.Item（アイテム取得）
    // -------------------------------------------------------------
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

    // -------------------------------------------------------------
    //  ObjectType.StateChange（状態変化：ドア、箱、スイッチ等）
    // -------------------------------------------------------------
    private void OnStateChangeClick()
    {
        if (isStateChanged) return;
        isStateChanged = true;

        // パターン1: オブジェクト自体の切り替え
        if (beforeStateObject != null && afterStateObject != null)
        {
            beforeStateObject.SetActive(false);
            afterStateObject.SetActive(true);
        }
        // パターン2: 画像（Sprite）の差し替え
        else if (targetUIImage != null && changedStateSprite != null)
        {
            targetUIImage.sprite = changedStateSprite;
        }
    }

    // -------------------------------------------------------------
    //  ObjectType.Okan（クリア＆自動セーブ）
    // -------------------------------------------------------------
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

    // -------------------------------------------------------------
    //  共通メッセージ表示
    // -------------------------------------------------------------
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