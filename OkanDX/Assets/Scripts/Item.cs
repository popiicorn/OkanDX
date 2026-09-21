using UnityEngine;

[System.Serializable]
public class Item
{
    public string id;           // アイテム識別ID (例: "key_01")
    public string itemName;     // アイテム名 (例: "小さな鍵")
    [TextArea(2, 4)]
    public string description;  // アイテムの説明文
    public Sprite icon;         // アイテムのアイコン画像

    [Header("■ 詳細（拡大）表示設定")]
    public bool canInspect = false;   // 虫眼鏡を表示して拡大可能にするか
    public Sprite detailSprite;       // 拡大表示する画像（本の表紙や手紙など）
    [TextArea(2, 4)]
    public string detailMessage;      // （任意）拡大画面で出す補足テキスト
}