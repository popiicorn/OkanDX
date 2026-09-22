using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ClickableObject : MonoBehaviour, IPointerClickHandler
{
    public enum ObjectType
    {
        Examine,          // ① 調べるだけ（メッセージ表示）
        Item,             // ② アイテム取得（4つの後処理対応）
        StateChange,      // ③ 状態変化（条件なしでのドア開閉・画像差し替えなど）
        ItemUse,          // ④ 指定アイテム使用による状態変化・ギミック解除
        Okan,             // ⑤ おかん（クリックでクリア＆自動セーブ）
        MultiStateChange  // ⑥ 複数段階の画像切り替え（順番に画像を差し替える）
    }

    public enum ItemPostAction
    {
        Hide,                    // 消える
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

    [Header(" └ アイテム獲得時のウィンドウカラー")]
    [SerializeField] private Color itemGetPanelColor = new Color(1.0f, 0.9f, 0.4f);

    [Header(" └ 画像切り替え用 (ChangeSprite)")]
    [SerializeField] private Sprite changedSprite;

    [Header(" └ 非表示用オブジェクト (Hide / SwapObject)")]
    [Tooltip("アイテム取得時に非表示（消去）にしたいオブジェクトのリスト（※空欄の場合は自分自身のみ非表示）")]
    [SerializeField] private GameObject[] hideObjects; // ★ 複数非表示用

    [Header(" └ 別オブジェクト出現用 (SwapObject)")]
    [Tooltip("出現させたい新しいオブジェクト（複数指定可能）")]
    [SerializeField] private GameObject[] newObjects;

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
    [SerializeField] private GameObject[] beforeStateObjects;

    [Tooltip("変化後のオブジェクト（出現させる新しいオブジェクト）")]
    [SerializeField] private GameObject[] afterStateObjects;

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
    // 【複数段階の画像切り替え】タイプ用の設定
    // -------------------------------------------------------------
    [Header("■ 【複数段階画像切り替え】用の設定")]
    [Tooltip("切り替えていく画像のリスト（要素数をインスペクターで自由に設定）")]
    [SerializeField] private Sprite[] multiSprites;

    [Tooltip("各画像（ステップ）に対応して出現させたいオブジェクトのリスト\n※画像と同じ順番・要素数で設定。出したいものがないステップは『None』のままでOK")]
    [SerializeField] private GameObject[] stepObjects;

    [Tooltip("次のステップに進んだ際、前のステップで出現させたオブジェクトを自動で消す（非表示にする）か")]
    [SerializeField] private bool hidePreviousStepObject = true;

    [Tooltip("最後の画像に達した後にクリックした際に出すメッセージ")]
    [TextArea(2, 5)]
    [SerializeField] private string multiStateEndMessage = "これ以上は動かないようだ。";

    [Tooltip("最後の画像まで切り替わった後、最初（0番目）に戻るか")]
    [SerializeField] private bool loopSprites = false;

    private int currentSpriteIndex = 0; // 現在の画像インデックス

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

            case ObjectType.MultiStateChange:
                AnimateClick();
                OnMultiStateChangeClick();
                break;
        }
    }

    private void AnimateClick()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        transform.DOScale(0.9f, 0.06f).OnComplete(() =>
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

        bool isAdded = InventoryManager.Instance.AddItem(itemData);

        if (isAdded)
        {
            hasGottenItem = true;

            string itemNameText = !string.IsNullOrEmpty(itemData.itemName) ? itemData.itemName : "アイテム";
            string getMsg = $"「{itemNameText}」を手に入れた！";

            transform.DOKill();
            transform.localScale = Vector3.one;

            Sequence getSequence = DOTween.Sequence();
            getSequence
                .Append(transform.DOScale(0.85f, 0.06f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(0f, 0.0f).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    ShowMessage(getMsg, itemGetPanelColor);
                    ApplyItemPostAction();
                });
        }
        else
        {
            AnimateClick();
            ShowMessage("これ以上持てないようだ。");
        }
    }

    private void ApplyItemPostAction()
    {
        // ★ 1. HideObjects（非表示リスト）に登録されているオブジェクトを一括非表示
        if (hideObjects != null && hideObjects.Length > 0)
        {
            foreach (var obj in hideObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        // ★ 2. 各後処理の実行
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
                // 登録された複数のオブジェクトを一括アクティブ化
                if (newObjects != null && newObjects.Length > 0)
                {
                    foreach (var obj in newObjects)
                    {
                        if (obj != null)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
                gameObject.SetActive(false);
                break;
        }
    }

    private void OnStateChangeClick()
    {
        if (isStateChanged)
        {
            ShowMessage(inspectAfterGetMessage);
            return;
        }

        ChangeState();
    }

    private void OnMultiStateChangeClick()
    {
        if (multiSprites == null || multiSprites.Length == 0) return;

        Image targetImg = targetUIImage != null ? targetUIImage : imageComponent;
        if (targetImg == null) return;

        if (currentSpriteIndex < multiSprites.Length)
        {
            if (multiSprites[currentSpriteIndex] != null)
            {
                targetImg.sprite = multiSprites[currentSpriteIndex];
            }

            UpdateStepObjects(currentSpriteIndex);
            currentSpriteIndex++;
        }
        else
        {
            if (loopSprites)
            {
                currentSpriteIndex = 0;
                if (multiSprites[currentSpriteIndex] != null)
                {
                    targetImg.sprite = multiSprites[currentSpriteIndex];
                }

                UpdateStepObjects(currentSpriteIndex);
                currentSpriteIndex++;
            }
            else
            {
                ShowMessage(multiStateEndMessage);
            }
        }
    }

    private void UpdateStepObjects(int index)
    {
        if (stepObjects == null || stepObjects.Length == 0) return;

        if (hidePreviousStepObject)
        {
            foreach (var obj in stepObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (index < stepObjects.Length && stepObjects[index] != null)
        {
            stepObjects[index].SetActive(true);
        }
    }

    private void OnItemUseClick()
    {
        if (isStateChanged)
        {
            ShowMessage(afterUnlockedMessage);
            return;
        }

        Item selected = InventoryManager.Instance != null ? InventoryManager.Instance.SelectedItem : null;

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

        bool hasBefore = beforeStateObjects != null && beforeStateObjects.Length > 0;
        bool hasAfter = afterStateObjects != null && afterStateObjects.Length > 0;

        if (hasBefore || hasAfter)
        {
            if (hasBefore)
            {
                foreach (var obj in beforeStateObjects)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }

            if (hasAfter)
            {
                foreach (var obj in afterStateObjects)
                {
                    if (obj != null) obj.SetActive(true);
                }
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
            Debug.Log($"[MessageUI] メッセージ呼び出し成功: {msg}");
            MessageUI.Instance.ShowMessage(msg);
        }
        else
        {
            Debug.LogError($"[MessageUI] エラー: Instance が null です！ (メッセージ: {msg})");
        }
    }

    private void ShowMessage(string msg, Color customColor)
    {
        if (string.IsNullOrEmpty(msg)) return;

        if (MessageUI.Instance != null)
        {
            Debug.Log($"[MessageUI] カラー付きメッセージ呼び出し成功: {msg}");
            MessageUI.Instance.ShowMessage(msg, customColor);
        }
        else
        {
            Debug.LogError($"[MessageUI] エラー: Instance が null です！ (メッセージ: {msg})");
        }
    }
}