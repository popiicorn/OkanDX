﻿using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// このオブジェクトが表示（アクティブ化）／非表示にされたときに、インスペクタで指定した処理を呼ぶ。
/// 例: ギミックの結果オブジェクトが表示されたら、別のギミックの演出を開始する。
/// 例: アイテムを拾ってオブジェクトが消えたら、別のオブジェクトをクリック可能にする。
/// </summary>
public class ActivateEventTrigger : MonoBehaviour
{
    // ================================================
    // インスペクタ設定
    // ================================================
    [Header("■ 表示されたときに呼ぶ処理")]
    [SerializeField] private UnityEvent onActivated;

    [Header("■ 非表示にされたときに呼ぶ処理")]
    [Tooltip("このオブジェクト自身が非表示にされたときだけ呼ぶ（親の非表示やシーン終了では呼ばない）")]
    [SerializeField] private UnityEvent onDeactivated;

    [Header("■ 呼び出し回数")]
    [Tooltip("ON: それぞれ最初の 1 回だけ呼ぶ / OFF: 表示・非表示のたびに呼ぶ")]
    [SerializeField] private bool invokeOnce = true;

    private bool hasInvoked = false;
    private bool hasInvokedDeactivated = false;

    private void OnEnable()
    {
        if (invokeOnce && hasInvoked) return;

        hasInvoked = true;
        onActivated.Invoke();
    }

    private void OnDisable()
    {
        // 親が非表示になった場合やシーン終了時は activeSelf が true のままなので呼ばない
        if (gameObject.activeSelf) return;
        if (invokeOnce && hasInvokedDeactivated) return;

        hasInvokedDeactivated = true;
        onDeactivated.Invoke();
    }
}
