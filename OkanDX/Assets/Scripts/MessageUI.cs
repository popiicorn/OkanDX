using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // DOTweenを使用

public class MessageUI : MonoBehaviour
{
    public static MessageUI Instance { get; private set; }

    [Header("UI参照")]
    [Tooltip("画面全体を覆うレイヤー (MessageLayer)")]
    [SerializeField] private GameObject messageLayer;

    [Tooltip("吹き出し本体のTransform (MessagePanel)")]
    [SerializeField] private Transform messagePanelTransform;

    [Tooltip("吹き出し本体のImage（カラー変更用）")]
    [SerializeField] private Image panelImage;

    [Tooltip("メッセージ表示テキスト")]
    [SerializeField] private TMP_Text messageText;

    [Header("カラー設定")]
    [Tooltip("通常のメッセージ調査時のデフォルトカラー")]
    [SerializeField] private Color defaultPanelColor = Color.white;

    [Header("アニメーション設定")]
    [Tooltip("ポヨン演出の開始スケール（例: 0.5）")]
    [SerializeField] private float baseStartScale = 0.5f;

    [Tooltip("表示完了時の目標スケール（通常: 1.0）")]
    [SerializeField] private float targetScale = 1.0f;

    [Tooltip("ポヨンにかかる時間（表示）")]
    [SerializeField] private float popupDuration = 0.25f;

    [Tooltip("シュッと閉じるにかかる時間（非表示）")]
    [SerializeField] private float hideDuration = 0.15f;

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

        // 起動時は完全に消去＆非アクティブ化（一瞬のチラつきを100%防止）
        HideImmediate();
    }

    private void Start()
    {
        HideImmediate();
    }

    /// <summary>
    /// メッセージをポヨンと表示する（デフォルトカラー）
    /// </summary>
    public void ShowMessage(string text)
    {
        ShowMessage(text, defaultPanelColor);
    }

    /// <summary>
    /// メッセージをポヨンと表示する（カラー指定付き）
    /// </summary>
    public void ShowMessage(string text, Color panelColor)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }

        // ★ウィンドウの色を変更
        if (panelImage != null)
        {
            panelImage.color = panelColor;
        }

        if (messageLayer != null)
        {
            messageLayer.SetActive(true);
        }

        if (messagePanelTransform != null)
        {
            messagePanelTransform.gameObject.SetActive(true); // 表示する時に初めてアクティブ化
            messagePanelTransform.DOKill();

            // 一度 0.5 の大きさからスタートさせて、そこから 1.0 へポヨンと拡大
            messagePanelTransform.localScale = Vector3.one * baseStartScale;

            messagePanelTransform.DOScale(Vector3.one * targetScale, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 吹き出しを閉じる（通常のアニメーション付き非表示）
    /// </summary>
    public void HideMessage()
    {
        if (messagePanelTransform != null && messagePanelTransform.gameObject.activeSelf)
        {
            messagePanelTransform.DOKill();

            // 1.0 から 0.5 へシュッと縮み、終わったら非表示にする
            messagePanelTransform.DOScale(Vector3.one * baseStartScale, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    HideImmediate();
                });
        }
        else
        {
            HideImmediate();
        }
    }

    /// <summary>
    /// アニメーションなしで即座に非表示にする（画面切り替え時・チラつき防止用）
    /// </summary>
    public void HideImmediate()
    {
        if (messagePanelTransform != null)
        {
            messagePanelTransform.DOKill();
            messagePanelTransform.localScale = Vector3.zero;
            messagePanelTransform.gameObject.SetActive(false); // ★パネル自体を非アクティブ化
        }

        if (messageLayer != null)
        {
            messageLayer.SetActive(false);
        }
    }
}