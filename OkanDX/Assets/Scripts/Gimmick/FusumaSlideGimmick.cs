using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 戸（ふすま等）をクリックするたびに段階的にスライドして開くギミック。
/// すべての戸が開き切ったら、指定した Graphic の色を変えてクリック可能にする。
/// </summary>
public class FusumaSlideGimmick : MonoBehaviour
{
    /// <summary>
    /// 全開時の色の変え方
    /// </summary>
    public enum ColorChangeMode
    {
        Fade,    // ふわっとフェード
        Instant  // パッと切り替え
    }

    [System.Serializable]
    public class SlideDoor
    {
        [Tooltip("クリックを受け付ける戸の Button")]
        public Button doorButton;

        [Tooltip("動かす戸の RectTransform（未指定の場合は doorButton の RectTransform）")]
        public RectTransform doorTransform;

        [Tooltip("クリックごとの移動先 X 座標（1回目, 2回目, 3回目...）。最後の値が全開位置")]
        public List<float> stepPositionsX = new List<float>();

        [HideInInspector] public int currentStep = 0;

        public bool IsFullyOpened => currentStep >= stepPositionsX.Count;
    }

    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 戸の設定")]
    [SerializeField] private List<SlideDoor> doors = new List<SlideDoor>();

    [Header("■ 移動アニメーション")]
    [Tooltip("1回のクリックで移動にかかる時間（秒）")]
    [SerializeField] private float moveDuration = 0.6f;
    [Tooltip("移動の動き方（OutCubic: 止まる直前にゆっくりになる）")]
    [SerializeField] private Ease moveEase = Ease.OutCubic;

    [Header("■ 全開時の色変更")]
    [Tooltip("Fade: ふわっと変える / Instant: パッと切り替える")]
    [SerializeField] private ColorChangeMode colorChangeMode = ColorChangeMode.Fade;
    [Tooltip("フェードにかかる時間（秒）。Instant のときは使わない")]
    [SerializeField] private float colorFadeDuration = 0.5f;
    [Tooltip("全開時に変更する色（RGB のみ反映し、アルファは元の値を保つ）")]
    [SerializeField] private Color openedColor = Color.white;
    [Tooltip("全開時に色を変更する Graphic（Image 等）")]
    [SerializeField] private Graphic[] colorChangeTargets;

    [Header("■ 全開時にクリック可能にするもの")]
    [Tooltip("全開時に RayCast Target を ON にする Graphic")]
    [SerializeField] private Graphic[] raycastEnableTargets;

    private bool isOpenedEventDone = false;

    // ================================================
    // 初期化
    // ================================================
    private void Awake()
    {
        for (int i = 0; i < doors.Count; i++)
        {
            int index = i;
            SlideDoor door = doors[i];

            if (door.doorTransform == null && door.doorButton != null)
            {
                door.doorTransform = door.doorButton.GetComponent<RectTransform>();
            }

            if (door.doorButton != null)
            {
                door.doorButton.onClick.AddListener(() => OnClickDoor(index));
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var door in doors)
        {
            if (door.doorTransform != null) door.doorTransform.DOKill();
        }
    }

    // ================================================
    // クリック処理
    // ================================================
    /// <summary>
    /// 戸をクリックしたとき、次の段階の位置までスライドさせる
    /// </summary>
    private void OnClickDoor(int doorIndex)
    {
        SlideDoor door = doors[doorIndex];
        if (door.doorTransform == null || door.IsFullyOpened) return;

        float targetX = door.stepPositionsX[door.currentStep];
        door.currentStep++;

        // 移動中に再クリックされた場合は、前の移動を止めて次の位置へ向かう
        door.doorTransform.DOKill();
        door.doorTransform.DOAnchorPosX(targetX, moveDuration)
            .SetEase(moveEase)
            .SetLink(door.doorTransform.gameObject)
            .OnComplete(CheckAllOpened);
    }

    // ----
    // すべての戸が開き切ったか判定する
    // ----
    private void CheckAllOpened()
    {
        if (isOpenedEventDone) return;

        foreach (var door in doors)
        {
            if (!door.IsFullyOpened) return;
        }

        isOpenedEventDone = true;
        OnAllOpened();
    }

    // ----
    // 全開時の演出：色を変えてクリック可能にする
    // ----
    private void OnAllOpened()
    {
        if (colorChangeTargets != null)
        {
            foreach (var target in colorChangeTargets)
            {
                if (target == null) continue;

                Color newColor = new Color(openedColor.r, openedColor.g, openedColor.b, target.color.a);

                if (colorChangeMode == ColorChangeMode.Fade)
                {
                    target.DOKill();
                    target.DOColor(newColor, colorFadeDuration).SetLink(target.gameObject);
                }
                else
                {
                    target.color = newColor;
                }
            }
        }

        if (raycastEnableTargets != null)
        {
            foreach (var target in raycastEnableTargets)
            {
                if (target != null) target.raycastTarget = true;
            }
        }
    }
}
