using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SequenceButton : MonoBehaviour, IPointerClickHandler
{
    [Header("このボタンのID (0 ~ 3)")]
    [SerializeField] private int buttonID;

    [Header("親のパズル管理者")]
    [SerializeField] private SequencePuzzle puzzleManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enabled) return;

        // ClickableObject と同じクリック時のポンッとするアニメーション演出
        AnimateClick();

        if (puzzleManager != null)
        {
            puzzleManager.OnButtonPressed(buttonID);
        }
    }

    private void AnimateClick()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        transform.DOScale(0.9f, 0.06f).OnComplete(() =>
        {
            transform.DOScale(1f, 0.15f).SetEase(Ease.OutBack);
        });
    }
}