using UnityEngine;
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

    [Tooltip("メッセージ表示テキスト")]
    [SerializeField] private TMP_Text messageText;

    [Header("アニメーション設定")]
    [SerializeField] private float popupDuration = 0.25f; // ポヨンにかかる時間

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

        // 編集時に表示オンになっていても、起動時に自動で非表示＆スケールゼロにする
        if (messagePanelTransform != null)
        {
            messagePanelTransform.localScale = Vector3.zero;
        }

        if (messageLayer != null)
        {
            messageLayer.SetActive(false);
        }
    }

    private void Start()
    {
        // 起動時は非表示
        if (messageLayer != null) messageLayer.SetActive(false);
    }

    /// <summary>
    /// メッセージをポヨンと表示する
    /// </summary>
    public void ShowMessage(string text)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }

        if (messageLayer != null)
        {
            messageLayer.SetActive(true);
        }

        if (messagePanelTransform != null)
        {
            // 進行中のアニメーションをキャンセルしてスケールを0にする
            messagePanelTransform.DOKill();
            messagePanelTransform.localScale = Vector3.zero;

            // 0 から 1.0 へ向かって反動をつけて拡大（Ease.OutBack）
            messagePanelTransform.DOScale(Vector3.one, popupDuration)
     .SetEase(Ease.OutBack)
     .SetUpdate(true); // ← SetUpdate(true) に変更
        }
    }

    /// <summary>
    /// 吹き出しを閉じる
    /// </summary>
    public void HideMessage()
    {
        if (messagePanelTransform != null)
        {
            messagePanelTransform.DOKill();
            // シュッと縮んでから非表示にする
            messagePanelTransform.DOScale(Vector3.zero, 0.15f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (messageLayer != null)
                    {
                        messageLayer.SetActive(false);
                    }
                });
        }
        else
        {
            if (messageLayer != null)
            {
                messageLayer.SetActive(false);
            }
        }
    }
}