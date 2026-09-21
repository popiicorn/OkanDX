using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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
    [SerializeField] private float examinePositionY = 0f;
    [SerializeField] private float itemGetPositionY = 200f;

    [Header("カラー設定")]
    [SerializeField] private Color defaultPanelColor = Color.white;

    [Header("アニメーション設定")]
    [SerializeField] private float baseStartScale = 0.5f;
    [SerializeField] private float targetScale = 1.0f;
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    private bool canClose = false;
    private CanvasGroup messageCanvasGroup; // ★透明度制御用

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

        // ★ CanvasGroup を取得（無ければ自動追加）
        messageCanvasGroup = GetComponent<CanvasGroup>();
        if (messageCanvasGroup == null)
        {
            messageCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 初期状態は画面上に配置しつつ透明（Alpha=0）＆クリック透過（BlocksRaycasts=false）にしておく
        HideImmediate();
    }

    private void Update()
    {
        if (canClose && messageCanvasGroup != null && messageCanvasGroup.alpha > 0.9f)
        {
            if (Input.GetMouseButtonDown(0))
            {
                HideMessage();
            }
        }
    }

    public void ShowMessage(string text)
    {
        ShowMessageInternal(text, defaultPanelColor, examinePositionY);
    }

    public void ShowMessage(string text, Color panelColor)
    {
        ShowMessageInternal(text, panelColor, itemGetPositionY);
    }

    private void ShowMessageInternal(string text, Color panelColor, float targetPosY)
    {
        if (messageText != null) messageText.text = text;
        if (panelImage != null) panelImage.color = panelColor;

        canClose = false;

        // ★ オブジェクト自体は常にアクティブにしておき、CanvasGroupで可視化する
        gameObject.SetActive(true);
        if (messageLayer != null) messageLayer.SetActive(true);
        if (messagePanelTransform != null) messagePanelTransform.gameObject.SetActive(true);

        if (messageCanvasGroup != null)
        {
            messageCanvasGroup.alpha = 1f;
            messageCanvasGroup.blocksRaycasts = true; // タップ受付ON
        }

        if (messagePanelTransform != null)
        {
            messagePanelTransform.DOKill();

            // Y位置設定
            Vector2 pos = messagePanelTransform.anchoredPosition;
            pos.y = targetPosY;
            messagePanelTransform.anchoredPosition = pos;

            // ポヨンと拡大アニメーション
            messagePanelTransform.localScale = Vector3.one * baseStartScale;
            messagePanelTransform.DOScale(Vector3.one * targetScale, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    StartCoroutine(EnableCloseNextFrame());
                });
        }
        else
        {
            StartCoroutine(EnableCloseNextFrame());
        }
    }

    private IEnumerator EnableCloseNextFrame()
    {
        yield return null;
        canClose = true;
    }

    public void HideMessage()
    {
        canClose = false;

        if (messagePanelTransform != null && messageCanvasGroup != null && messageCanvasGroup.alpha > 0f)
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

    public void HideImmediate()
    {
        canClose = false;

        // ★ 非アクティブ(SetActive(false))にせず、透明(alpha=0) ＆ タップ無効(blocksRaycasts=false)にする
        if (messageCanvasGroup != null)
        {
            messageCanvasGroup.alpha = 0f;
            messageCanvasGroup.blocksRaycasts = false;
        }

        if (messagePanelTransform != null)
        {
            messagePanelTransform.DOKill();
            messagePanelTransform.localScale = Vector3.one * baseStartScale;
        }
    }
}