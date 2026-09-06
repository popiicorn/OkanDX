using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // UIのイベント検知に必要
using DG.Tweening;

// IPointerClickHandler を継承することで UI のクリックを直接受け取る
public class ClickableObject : MonoBehaviour, IPointerClickHandler
{
    public enum ObjectType
    {
        Examine, // 調べるだけ（メッセージ表示）
        Door,    // ドア（クリックで開く画像へ変更）
        Okan     // おかん（クリックでクリア）
    }

    [Header("オブジェクトの種類")]
    [SerializeField] private ObjectType objectType = ObjectType.Examine;

    [Header("【調べる】タイプ用の設定")]
    [TextArea(2, 5)]
    [SerializeField] private string examineMessage = "特に変わったところはないようだ。";

    [Header("【ドア】タイプ用の設定")]
    [Tooltip("画像差し替えで表現する場合（従来通り）")]
    [SerializeField] private Sprite openDoorSprite;
    [SerializeField] private Image targetUIImage;

    [Header("【ドア】オブジェクト切り替え用の設定")]
    [Tooltip("閉じたドアのオブジェクト（自分自身、または非表示にしたいオブジェクト）")]
    [SerializeField] private GameObject closedDoorObject;

    [Tooltip("開いたドアのオブジェクト（表示させたいオブジェクト）")]
    [SerializeField] private GameObject openDoorObject;

    private bool isDoorOpen = false;

    [Header("【おかん】タイプ用の設定")]
    [SerializeField] private EpisodeManager episodeManager;

    // --- UI（Canvas）上でクリックされた時に自動で呼ばれる関数 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        ExecuteClickAction();
    }

    // --- 2D Collider（Sprite）上でクリックされた時に呼ばれる関数 ---
    private void OnMouseDown()
    {
        ExecuteClickAction();
    }

    /// <summary>
    /// クリック時の共通処理
    /// </summary>
    private void ExecuteClickAction()
    {
        // 1. 拡縮アニメーション
        AnimateClick();

        // 2. 種類ごとの処理
        switch (objectType)
        {
            case ObjectType.Examine:
                OnExamine();
                break;

            case ObjectType.Door:
                OnDoorClick();
                break;

            case ObjectType.Okan:
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

    private void OnExamine()
    {
        if (MessageUI.Instance != null)
        {
            MessageUI.Instance.ShowMessage(examineMessage);
        }
        else
        {
            Debug.LogWarning("MessageUIのInstanceが見つかりません。");
        }
    }

    private void OnDoorClick()
    {
        if (isDoorOpen) return;

        isDoorOpen = true;

        // --- パターンA: オブジェクトの表示・非表示切り替え ---
        if (closedDoorObject != null && openDoorObject != null)
        {
            closedDoorObject.SetActive(false); // 閉じた扉を非表示
            openDoorObject.SetActive(true);    // 開いた扉を表示
        }
        // --- パターンB: 画像の差し替え（従来の方法） ---
        else if (targetUIImage != null && openDoorSprite != null)
        {
            targetUIImage.sprite = openDoorSprite;
        }

        Debug.Log("ドアが開いた！");
    }

    private void OnOkanClick()
    {
        Debug.Log("おかんを発見！クリア！");

        if (episodeManager != null)
        {
            episodeManager.OnClearEpisode();
        }
        else
        {
            var manager = FindObjectOfType<EpisodeManager>();
            if (manager != null)
            {
                manager.OnClearEpisode();
            }
        }
    }
}