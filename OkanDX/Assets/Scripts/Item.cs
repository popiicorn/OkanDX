using UnityEngine;

[System.Serializable]
public class Item
{
    public string id;           // アイテム識別ID (例: "key_01")
    public string itemName;     // アイテム名 (例: "小さな鍵")
    [TextArea(2, 4)]
    public string description;  // アイテムの説明文
    public Sprite icon;         // アイテムのアイコン画像
}