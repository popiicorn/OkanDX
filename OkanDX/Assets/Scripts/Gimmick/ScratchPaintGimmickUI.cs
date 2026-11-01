using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// スクラッチ風の塗りギミック用パネル。
/// 「塗る前」の画像をこすって消し、下の「塗った後」の画像を見せていく。
/// 指定割合まで塗れたら完成メッセージを出し、シーン上のオブジェクトを切り替える。
/// 途中で閉じても塗った状態は保持され、次に開いたときは続きから塗れる。
/// </summary>
public class ScratchPaintGimmickUI : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 画面全体オーバーレイ")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float overlayAlpha = 0.6f;

    [Header("■ メインパネル（白い枠）")]
    [SerializeField] private RectTransform mainPanelTransform;

    [Header("■ 閉じるボタン")]
    [SerializeField] private Button closeButton;

    [Header("■ 開閉アニメーション")]
    [SerializeField] private float popupDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.15f;

    [Header("■ 画像")]
    [Tooltip("塗る前の画像（こすると消えていく側）")]
    [SerializeField] private Sprite beforeSprite;
    [Tooltip("塗った後の画像（下から見えてくる側）")]
    [SerializeField] private Sprite afterSprite;
    [Tooltip("塗った後の画像を表示する Image")]
    [SerializeField] private Image afterImage;
    [Tooltip("塗る前の画像を表示する RawImage（afterImage の上に重ねる）")]
    [SerializeField] private RawImage scratchImage;

    [Header("■ 塗り設定")]
    [Tooltip("ペンの太さ（半径。パネル上の UI 座標の単位）")]
    [SerializeField] private float brushRadius = 40f;
    [Tooltip("完成とみなす塗った割合（0〜1）。0.9 = 90%")]
    [Range(0.1f, 1f)]
    [SerializeField] private float completeRatio = 0.9f;
    [Tooltip("この値より不透明なピクセルだけを「塗る対象」として数える（0〜1）")]
    [Range(0f, 1f)]
    [SerializeField] private float paintableAlphaThreshold = 0.5f;
    [Tooltip("完成時に残った「塗る前」の画像を消すフェード時間（秒）")]
    [SerializeField] private float completeFadeDuration = 0.2f;

    [Header("■ ペンカーソル")]
    [Tooltip("マウスに追従させるペンの Image（Pivot の位置がペン先になる）")]
    [SerializeField] private RectTransform penCursor;
    [Tooltip("ペンカーソルに使う画像（未指定なら Image に設定済みの画像のまま）")]
    [SerializeField] private Sprite penCursorSprite;
    [Tooltip("ペンカーソルを表示する範囲（白い枠に対する割合）。0.9 = 内側 90%")]
    [Range(0.1f, 1f)]
    [SerializeField] private float cursorAreaRatio = 0.9f;

    [Header("■ 完成時のメッセージ")]
    [SerializeField] private GameObject messageWindowObject;
    [SerializeField] private TMP_Text messageText;
    [TextArea]
    [SerializeField] private string completeMessage = "なんかできたで！";
    [SerializeField] private float messageStartScale = 0.5f;
    [SerializeField] private float messagePopupDuration = 0.25f;

    [Header("■ 完成時のシーン切り替え")]
    [Tooltip("完成時に非表示にするオブジェクト")]
    [SerializeField] private GameObject[] hideObjectsOnComplete;
    [Tooltip("完成時に表示するオブジェクト")]
    [SerializeField] private GameObject[] showObjectsOnComplete;

    // ================================================
    // 内部状態
    // ================================================
    private Canvas rootCanvas;
    private Texture2D scratchTexture;
    private Color32[] scratchPixels;
    private byte paintableAlphaByte;
    private int paintablePixelCount = 0;
    private int paintedPixelCount = 0;

    private bool isInitialized = false;
    private bool isOpenRequested = false;
    private bool isCompleted = false;
    private bool isPainting = false;
    private bool isCursorShown = false;
    private Vector2 lastPaintPixel;

    /// <summary>
    /// 完成済みかどうか
    /// </summary>
    public bool IsCompleted => isCompleted;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        if (penCursor != null)
        {
            if (penCursorSprite != null)
            {
                Image penImage = penCursor.GetComponent<Image>();
                if (penImage != null) penImage.sprite = penCursorSprite;
            }
            penCursor.gameObject.SetActive(false);
        }

        // OpenPanel から初めてアクティブ化された場合は閉じない
        if (!isOpenRequested) HideImmediate();
    }

    private void OnDisable()
    {
        isPainting = false;
        SetPenCursorVisible(false);
    }

    private void OnDestroy()
    {
        if (overlayCanvasGroup != null) overlayCanvasGroup.DOKill();
        if (mainPanelTransform != null) mainPanelTransform.DOKill();
        if (messageWindowObject != null) messageWindowObject.transform.DOKill();
        if (scratchImage != null) scratchImage.DOKill();

        if (scratchTexture != null) Destroy(scratchTexture);
    }

    // ----
    // 塗る前の画像から書き換え可能なテクスチャを作る（初回オープン時のみ）
    // ----
    private void InitializeScratch()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (afterImage != null)
        {
            afterImage.sprite = afterSprite;
            afterImage.preserveAspect = true;
        }

        if (scratchImage == null || beforeSprite == null)
        {
            Debug.LogWarning("[ScratchPaintGimmickUI] scratchImage または beforeSprite が設定されていません。");
            return;
        }

        // テクスチャが Read/Write 無効でも読めるよう、RenderTexture 経由でピクセルを取得する
        Rect texRect = beforeSprite.textureRect;
        int width = Mathf.RoundToInt(texRect.width);
        int height = Mathf.RoundToInt(texRect.height);
        Texture2D source = beforeSprite.texture;

        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture.active = rt;

        scratchTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        scratchTexture.wrapMode = TextureWrapMode.Clamp;
        scratchTexture.ReadPixels(new Rect(texRect.x, texRect.y, width, height), 0, 0);
        scratchTexture.Apply(false);

        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);

        scratchPixels = scratchTexture.GetPixels32();

        // 塗る対象（不透明なピクセル）の数を数える
        paintableAlphaByte = (byte)Mathf.RoundToInt(paintableAlphaThreshold * 255f);
        paintablePixelCount = 0;
        foreach (var px in scratchPixels)
        {
            if (px.a > paintableAlphaByte) paintablePixelCount++;
        }

        scratchImage.texture = scratchTexture;
        scratchImage.raycastTarget = false;
        FitScratchImageToAfterImage(width, height);
    }

    // ----
    // 塗る前の画像を、塗った後の画像（preserveAspect）と同じ位置・サイズに合わせる
    // ----
    private void FitScratchImageToAfterImage(int texWidth, int texHeight)
    {
        RectTransform scratchRect = scratchImage.rectTransform;
        RectTransform baseRect = afterImage != null ? afterImage.rectTransform : scratchRect.parent as RectTransform;
        if (baseRect == null) return;

        Vector2 areaSize = baseRect.rect.size;
        float texAspect = (float)texWidth / texHeight;
        Vector2 fitSize = areaSize;
        if (areaSize.x / areaSize.y > texAspect)
        {
            fitSize.x = areaSize.y * texAspect;
        }
        else
        {
            fitSize.y = areaSize.x / texAspect;
        }

        if (scratchRect.parent == baseRect)
        {
            scratchRect.anchorMin = scratchRect.anchorMax = new Vector2(0.5f, 0.5f);
            scratchRect.pivot = new Vector2(0.5f, 0.5f);
            scratchRect.anchoredPosition = baseRect.rect.center;
        }
        scratchRect.sizeDelta = fitSize;
    }

    // ================================================
    // 開閉
    // ================================================
    /// <summary>
    /// パネルを開く（2 回目以降は前回の塗り状態から再開）
    /// </summary>
    public void OpenPanel()
    {
        isOpenRequested = true;
        gameObject.SetActive(true);

        InitializeScratch();

        if (messageWindowObject != null)
        {
            messageWindowObject.transform.DOKill();
            messageWindowObject.transform.localScale = Vector3.one;
            messageWindowObject.SetActive(isCompleted);
        }

        // ① オーバーレイのフェードイン
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.DOFade(overlayAlpha, popupDuration).SetUpdate(true);
        }

        // ② メインパネルのポヨン拡大表示 (0.5倍 -> 1.0倍, Ease.OutBack)
        if (mainPanelTransform != null)
        {
            mainPanelTransform.DOKill();
            mainPanelTransform.gameObject.SetActive(true);
            mainPanelTransform.localScale = Vector3.one * 0.5f;
            mainPanelTransform.DOScale(Vector3.one, popupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// パネルを閉じる（縮小＋フェードアウト）
    /// </summary>
    public void ClosePanel()
    {
        isOpenRequested = false;
        isPainting = false;
        SetPenCursorVisible(false);

        // ① オーバーレイのフェードアウト
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.DOKill();
            overlayCanvasGroup.DOFade(0f, hideDuration).SetUpdate(true);
        }

        // ② メインパネルのポヨン縮小非表示 (1.0倍 -> 0.5倍, Ease.InBack)
        if (mainPanelTransform != null && mainPanelTransform.gameObject.activeSelf)
        {
            mainPanelTransform.DOKill();
            mainPanelTransform.DOScale(Vector3.one * 0.5f, hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(HideImmediate);
        }
        else
        {
            HideImmediate();
        }
    }

    /// <summary>
    /// 即時非表示
    /// </summary>
    public void HideImmediate()
    {
        isOpenRequested = false;
        isPainting = false;
        SetPenCursorVisible(false);

        if (overlayCanvasGroup != null) overlayCanvasGroup.alpha = 0f;

        if (mainPanelTransform != null)
        {
            mainPanelTransform.localScale = Vector3.zero;
            mainPanelTransform.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    // ================================================
    // カーソル追従と塗り（パネル表示中のみ動く）
    // ================================================
    private void Update()
    {
        if (!isOpenRequested || isCompleted || mainPanelTransform == null || !mainPanelTransform.gameObject.activeSelf) return;

        Vector2 screenPos = Input.mousePosition;
        bool inArea = IsInCursorArea(screenPos);

        SetPenCursorVisible(inArea);
        if (inArea) MovePenCursor(screenPos);

        if (inArea && Input.GetMouseButton(0))
        {
            PaintAt(screenPos);
        }
        else
        {
            isPainting = false;
        }
    }

    // ----
    // 白い枠の内側（cursorAreaRatio の範囲）にいるか
    // ----
    private bool IsInCursorArea(Vector2 screenPos)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mainPanelTransform, screenPos, GetEventCamera(), out Vector2 local))
        {
            return false;
        }

        Rect rect = mainPanelTransform.rect;
        Vector2 half = rect.size * 0.5f * cursorAreaRatio;
        Vector2 offset = local - rect.center;
        return Mathf.Abs(offset.x) <= half.x && Mathf.Abs(offset.y) <= half.y;
    }

    private void MovePenCursor(Vector2 screenPos)
    {
        if (penCursor == null) return;

        RectTransform parentRect = penCursor.parent as RectTransform;
        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, GetEventCamera(), out Vector2 local))
        {
            penCursor.localPosition = local;
        }
    }

    // ----
    // ペンカーソルの表示切り替え（OS のカーソルは逆に隠す）
    // ----
    private void SetPenCursorVisible(bool visible)
    {
        if (penCursor == null) return;
        if (isCursorShown == visible) return;

        isCursorShown = visible;
        penCursor.gameObject.SetActive(visible);
        Cursor.visible = !visible;
    }

    // ----
    // 指定したスクリーン座標を塗る（前回位置から線でつなぐ）
    // ----
    private void PaintAt(Vector2 screenPos)
    {
        if (scratchTexture == null || scratchImage == null) return;

        RectTransform scratchRect = scratchImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(scratchRect, screenPos, GetEventCamera(), out Vector2 local))
        {
            return;
        }

        Rect rect = scratchRect.rect;
        Vector2 pixel = new Vector2(
            (local.x - rect.x) / rect.width * scratchTexture.width,
            (local.y - rect.y) / rect.height * scratchTexture.height);
        float radiusPx = brushRadius / rect.width * scratchTexture.width;

        Vector2 from = isPainting ? lastPaintPixel : pixel;
        float distance = Vector2.Distance(from, pixel);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / Mathf.Max(1f, radiusPx * 0.5f)));

        bool changed = false;
        for (int i = 1; i <= steps; i++)
        {
            changed |= EraseCircle(Vector2.Lerp(from, pixel, (float)i / steps), radiusPx);
        }

        isPainting = true;
        lastPaintPixel = pixel;

        if (!changed) return;

        scratchTexture.SetPixels32(scratchPixels);
        scratchTexture.Apply(false);

        if (paintablePixelCount > 0 && (float)paintedPixelCount / paintablePixelCount >= completeRatio)
        {
            OnCompleted();
        }
    }

    // ----
    // 円の範囲の「塗る前」の画像を透明にする。1 ピクセルでも変化したら true
    // ----
    private bool EraseCircle(Vector2 center, float radius)
    {
        int width = scratchTexture.width;
        int height = scratchTexture.height;
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + radius));
        float sqrRadius = radius * radius;
        bool changed = false;

        for (int y = minY; y <= maxY; y++)
        {
            float dy = y - center.y;
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - center.x;
                if (dx * dx + dy * dy > sqrRadius) continue;

                int index = y * width + x;
                byte alpha = scratchPixels[index].a;
                if (alpha == 0) continue;

                if (alpha > paintableAlphaByte) paintedPixelCount++;
                scratchPixels[index].a = 0;
                changed = true;
            }
        }

        return changed;
    }

    private Camera GetEventCamera()
    {
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return rootCanvas.worldCamera;
    }

    // ================================================
    // 完成
    // ================================================
    private void OnCompleted()
    {
        if (isCompleted) return;
        isCompleted = true;
        isPainting = false;

        // カーソルを通常に戻す
        SetPenCursorVisible(false);

        // 残った「塗る前」の画像を消して、塗った後の画像を全部見せる
        if (scratchImage != null)
        {
            scratchImage.DOKill();
            scratchImage.DOFade(0f, completeFadeDuration).SetUpdate(true).SetLink(scratchImage.gameObject);
        }

        // パネルの後ろでシーン上のオブジェクトを切り替える
        if (hideObjectsOnComplete != null)
        {
            foreach (var obj in hideObjectsOnComplete)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (showObjectsOnComplete != null)
        {
            foreach (var obj in showObjectsOnComplete)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // 完成メッセージをポヨンと表示（閉じるまで出したまま）
        if (messageText != null) messageText.text = completeMessage;

        if (messageWindowObject != null)
        {
            Transform messageTransform = messageWindowObject.transform;
            messageWindowObject.SetActive(true);
            messageTransform.DOKill();
            messageTransform.localScale = Vector3.one * messageStartScale;
            messageTransform.DOScale(Vector3.one, messagePopupDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(messageWindowObject);
        }
    }
}
