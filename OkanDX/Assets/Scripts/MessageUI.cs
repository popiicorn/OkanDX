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
    [SerializeField] private RectTransform messagePanelTransform;

    [Tooltip("吹き出し本体のImage（カラー変更用）")]
    [SerializeField] private Image panelImage;

    [Tooltip("メッセージ表示テキスト")]
    [SerializeField] private TMP_Text messageText;

    [Header("表示位置設定 (Y座標)")]
    [Tooltip("通常調査（説明）時のY座標（例: 0 で中央）")]
    [SerializeField] private float examinePositionY = 0f;

    [Tooltip("アイテム獲得時のY座標（例: 200 で少し上側）")]
    [SerializeField] private float itemGetPositionY = 200f;

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

        HideImmediate();
    }

    private void Start()
    {
        HideImmediate();
    }

    /// <summary>
    /// ① 通常の調査メッセージ表示（デフォルト色 ＆ 説明用の位置）
    /// </summary>
    public void ShowMessage(string text)
    {
        ShowMessageInternal(text, defaultPanelColor, examinePositionY);
    }

    /// <summary>
    /// ② アイテム獲得時メッセージ表示（指定色 ＆ アイテム用の位置）
    /// </summary>
    public void ShowMessage(string text, Color panelColor)
    {
        ShowMessageInternal(text, panelColor, itemGetPositionY);
    }

    /// <summary>
    /// 内部的な表示共通処理
    /// </summary>
    private void ShowMessageInternal(string text, Color panelColor, float targetPosY)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }

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
            messagePanelTransform.DOKill();
            messagePanelTransform.gameObject.SetActive(true);

            // ★ 呼び出し種別に応じてY座標（位置）を変更
            Vector2 pos = messagePanelTransform.anchoredPosition;
            pos.y = targetPosY;
            messagePanelTransform.anchoredPosition = pos;

            // ポヨンと拡大
            messagePanelTransform.localScale = Vector3.one * baseStartScale;
            messagePanelTransform.DOScale(Vector3.one * targetScale, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 吹き出しを閉じる
    /// </summary>
    public void HideMessage()
    {
        if (messagePanelTransform != null && messagePanelTransform.gameObject.activeSelf)
        {
            messagePanelTransform.DOKill();

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
    /// アニメーションなしで即座に非表示にする
    /// </summary>
    public void HideImmediate()
    {
        if (messagePanelTransform != null)
        {
            messagePanelTransform.DOKill();
            messagePanelTransform.localScale = Vector3.zero;
            messagePanelTransform.gameObject.SetActive(false);
        }

        if (messageLayer != null)
        {
            messageLayer.SetActive(false);
        }
    }
}