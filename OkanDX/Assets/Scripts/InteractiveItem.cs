using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class InteractiveItem : MonoBehaviour, IPointerClickHandler
{
    public enum ActionType
    {
        Hide,                   // ① 消える
        RemainAndShowText,      // ② 残って説明テキスト
        ChangeSpriteAndShowText,// ③ 画像が切り替わって説明テキスト
        SwapObject              // ④ 消えて別オブジェクトを表示
    }

    [Header("アイテム設定")]
    [SerializeField] private Item itemData; // 格納するアイテムデータ

    [Header("クリック後の挙動タイプ")]
    [SerializeField] private ActionType actionType = ActionType.Hide;

    [Header("③用：切り替え後の画像")]
    [SerializeField] private Sprite changedSprite;

    [Header("④用：代わりに出現させるオブジェクト")]
    [SerializeField] private GameObject newObject;

    [Header("②・③用：2回目以降に出す説明メッセージ")]
    [TextArea(2, 5)]
    [SerializeField] private string inspectMessage;

    private bool hasGottenItem = false;
    private Image imageComponent;

    private void Awake()
    {
        imageComponent = GetComponent<Image>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ExecuteClickAction();
    }

    // 2D Collider (Sprite) 用のクリック検知
    private void OnMouseDown()
    {
        ExecuteClickAction();
    }

    /// <summary>
    /// クリック時のメイン処理
    /// </summary>
    private void ExecuteClickAction()
    {
        // --- 未取得の場合：アイテムゲット処理 ＋ アニメーション ---
        if (!hasGottenItem)
        {
            if (InventoryManager.Instance == null || itemData == null) return;

            transform.DOKill();
            transform.localScale = Vector3.one;

            // Sequenceで「0.85倍に縮む ➔ そのまま0倍へ吸い込まれる」滑らかな流れを作成
            Sequence getSequence = DOTween.Sequence();

            getSequence
                // 1. キュッと一瞬沈み込む (0.06秒)
                .Append(transform.DOScale(0.8f, 0.06f).SetEase(Ease.OutQuad))
                // 2. 1に戻らず、そのまま0に向かってスーッと小さくなる (0.12秒)
                .Append(transform.DOScale(0f, 0f).SetEase(Ease.InQuad))
                // 3. 完了後にインベントリ追加と見た目の切り替え
                .OnComplete(() =>
                {
                    bool isAdded = InventoryManager.Instance.AddItem(itemData);

                    if (isAdded)
                    {
                        hasGottenItem = true;
                        ApplyPostGetAction();
                    }
                    else
                    {
                        // 満タンで入れなかった場合はサイズを元(1.0)に戻す
                        transform.localScale = Vector3.one;
                    }
                });
        }
        // --- 取得済みの場合：2回目以降のクリック処理 ---
        else
        {
            AnimateClick();
            ShowInspectMessage();
        }
    }

    /// <summary>
    /// 2回目以降の調べるクリック時のぽよんアニメーション（ClickableObjectと同等）
    /// </summary>
    private void AnimateClick()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        transform.DOScale(0.85f, 0.08f).OnComplete(() =>
        {
            transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
        });
    }

    /// <summary>
    /// アイテム取得後の見た目・状態の変化
    /// </summary>
    private void ApplyPostGetAction()
    {
        switch (actionType)
        {
            case ActionType.Hide:
                // ① 画像がそのまま消える
                gameObject.SetActive(false);
                break;

            case ActionType.RemainAndShowText:
                // ② サイズを1.0に戻して画面に残す
                transform.localScale = Vector3.one;
                break;

            case ActionType.ChangeSpriteAndShowText:
                // ③ 画像を切り替えてサイズを1.0に戻す
                if (imageComponent != null && changedSprite != null)
                {
                    imageComponent.sprite = changedSprite;
                }
                transform.localScale = Vector3.one;
                break;

            case ActionType.SwapObject:
                // ④ 自分は非表示になり、新しいオブジェクトを出現させる
                if (newObject != null)
                {
                    newObject.SetActive(true);
                }
                gameObject.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// ②・③でアイテム取得後にクリックした時のテキスト表示（MessageUIを使用）
    /// </summary>
    private void ShowInspectMessage()
    {
        if (string.IsNullOrEmpty(inspectMessage)) return;

        if (MessageUI.Instance != null)
        {
            MessageUI.Instance.ShowMessage(inspectMessage);
        }
        else
        {
            Debug.LogWarning("MessageUIのInstanceが見つかりません。");
        }
    }
}