using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SequencePuzzle : MonoBehaviour
{
    [Header("■ 正解の順番 (例: 0 -> 2 -> 1 -> 3)")]
    [SerializeField] private List<int> correctSequence = new List<int> { 0, 2, 1, 3 };

    [Header("■ 正解時の変化オブジェクト設定")]
    [Tooltip("正解時に非表示にするオブジェクト A")]
    [SerializeField] private GameObject objectA;

    [Tooltip("正解時に表示するオブジェクト B")]
    [SerializeField] private GameObject objectB;

    [Tooltip("正解時に表示するオブジェクト C")]
    [SerializeField] private GameObject objectC;

    [Header("■ 対象の4つのオブジェクト")]
    [Tooltip("SequenceButton と ClickableObject が付いている4つのオブジェクト")]
    [SerializeField] private GameObject[] puzzleObjects = new GameObject[4];

    [Header("■ 不正解メッセージ設定")]
    [Tooltip("不正解時に表示するメッセージ")]
    [SerializeField] private string wrongMessage = "ちがうで";

    private int currentInputIndex = 0;
    private bool isCleared = false;
    private bool isProcessingWrong = false; // 不正解処理中の重複実行防止

    private void Start()
    {
        // 初期状態：4つのオブジェクトの ClickableObject（調べる機能）のみをOFFにする
        foreach (var obj in puzzleObjects)
        {
            if (obj == null) continue;
            SetClickableObjectEnabled(obj, false);
        }
    }

    public void OnButtonPressed(int pressedID)
    {
        // クリア済み、または不正解処理中の連打は受け付けない
        if (isCleared || isProcessingWrong) return;

        // 押されたIDが「現在の順番の正解ID」と一致するか判定
        if (pressedID == correctSequence[currentInputIndex])
        {
            Debug.Log($"正解! ({currentInputIndex + 1}/{correctSequence.Count})");
            currentInputIndex++;

            // 最後まで順番通りに押せたらクリア！
            if (currentInputIndex >= correctSequence.Count)
            {
                OnClear();
            }
        }
        else
        {
            // 間違えた場合
            OnWrong();
        }
    }

    /// <summary>
    /// 不正解の処理（リセット＆メッセージ）
    /// </summary>
    private void OnWrong()
    {
        isProcessingWrong = true;
        currentInputIndex = 0; // 入力順を完全に最初（0）にリセット

        Debug.Log($"不正解: {wrongMessage}");

        // メッセージを表示
        if (MessageUI.Instance != null)
        {
            MessageUI.Instance.ShowMessage(wrongMessage);
        }

        // 短いインターバルを置いて入力受付を再開（連打による誤判定防止）
        Invoke(nameof(ResetWrongState), 0.3f);
    }

    private void ResetWrongState()
    {
        isProcessingWrong = false;
    }

    /// <summary>
    /// 正解（クリア）時の処理
    /// </summary>
    private void OnClear()
    {
        isCleared = true;
        Debug.Log("パズルクリア！");

        // ① Aを非表示、BとCを表示
        if (objectA != null) objectA.SetActive(false);
        if (objectB != null) objectB.SetActive(true);
        if (objectC != null) objectC.SetActive(true);

        // ② 4つのオブジェクトの挙動を切り替え
        foreach (var obj in puzzleObjects)
        {
            if (obj == null) continue;

            // SequenceButton（ギミック判定）をOFFにする
            SequenceButton seqBtn = obj.GetComponent<SequenceButton>();
            if (seqBtn != null) seqBtn.enabled = false;

            // ClickableObject（Examine機能）のみをONにする！
            SetClickableObjectEnabled(obj, true);
        }
    }

    /// <summary>
    /// ClickableObject のみを安全に ON / OFF 切り替え（Image等の描画コンポーネントは絶対にいじらない）
    /// </summary>
    private void SetClickableObjectEnabled(GameObject obj, bool state)
    {
        ClickableObject clickable = obj.GetComponent<ClickableObject>();
        if (clickable != null)
        {
            clickable.enabled = state;
        }
    }
}