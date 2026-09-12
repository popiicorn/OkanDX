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

        // 起動時は完全にスケール0にする（画面に残らないように）
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
            messagePanelTransform.DOKill();

            // ★一度 0.5 の大きさからスタートさせて、そこから 1.0 へポヨンと拡大
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
        if (messagePanelTransform != null)
        {
            messagePanelTransform.DOKill();

            // ★ 1.0 から 0.5 へシュッと縮み、終わったら一瞬で 0 に落として非表示にする
            messagePanelTransform.DOScale(Vector3.one * baseStartScale, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    messagePanelTransform.localScale = Vector3.zero; // 完全に消す

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