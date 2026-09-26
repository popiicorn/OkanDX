using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TVRemoteTarget : MonoBehaviour
{
    [Header("■ 画面切り替え設定")]
    [Tooltip("テレビ画面を表示しているSpriteRenderer（またはUI Image）")]
    [SerializeField] private SpriteRenderer tvScreenSpriteRenderer;
    [SerializeField] private Image tvScreenUIImage;

    [Tooltip("切り替える画像（Sprite）のリスト（任意に枚数追加可能）")]
    [SerializeField] private List<Sprite> tvSprites = new List<Sprite>();

    [Tooltip("チェックを入れると最後の画像から最初に戻ります")]
    [SerializeField] private bool isLoop = true;

    private int currentSpriteIndex = 0;

    /// <summary>
    /// テレビをクリックした時の処理（ClickableObjectやButtonイベントから呼び出し）
    /// </summary>
    public void OnClickTV()
    {
        SwitchTVScreen();
    }

    /// <summary>
    /// 画像切り替え処理
    /// </summary>
    public void SwitchTVScreen()
    {
        if (tvSprites == null || tvSprites.Count == 0) return;

        // 次の画像インデックスを計算
        if (isLoop)
        {
            currentSpriteIndex = (currentSpriteIndex + 1) % tvSprites.Count;
        }
        else
        {
            if (currentSpriteIndex < tvSprites.Count - 1)
            {
                currentSpriteIndex++;
            }
        }

        // 画像の反映（SpriteRenderer または UI Image に対応）
        Sprite nextSprite = tvSprites[currentSpriteIndex];

        if (tvScreenSpriteRenderer != null)
        {
            tvScreenSpriteRenderer.sprite = nextSprite;
        }

        if (tvScreenUIImage != null)
        {
            tvScreenUIImage.sprite = nextSprite;
        }
    }
}