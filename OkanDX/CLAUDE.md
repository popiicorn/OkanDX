# CLAUDE.md

このファイルは、このリポジトリで作業する Claude Code 向けのガイドです。

## プロジェクト概要

- **OkanDX**: 2D のポイント＆クリック型脱出ゲーム（エピソード制＋ミニゲーム）
- **Unity**: 6000.0.47f1（Unity 6）/ URP 17 / 2D
- **主要パッケージ・プラグイン**
  - uGUI + TextMesh Pro（UI はすべて Canvas ベース）
  - DOTween（`Assets/Plugins/Demigiant`）… UI 演出・アニメーション
  - Easy Save 3（`Assets/Plugins/Easy Save 3`）… セーブ/ロード（`ES3.Save` / `ES3.Load`）
  - Input System 1.14（`activeInputHandler: 2` = 旧 Input Manager と併用）
- Git リポジトリのルートは 1 つ上の `D:\Unity\OkanDX`。Unity プロジェクトはこの `OkanDX/` フォルダ。

## ディレクトリ構成

```
Assets/
  Scripts/            ゲームの C# スクリプト（asmdef なし = Assembly-CSharp）
    Animation/        DOTween を使った汎用アニメーション
    Gimmick/          謎解きギミック UI（絵合わせ・ラジオ・順番押し等）
    MiniGame/         ミニゲーム（TP キャッチ等）
  Scenes/             Boot / Title / EpisodeSelect / Episode_XXX / MiniGame_XXX / ResultScene
  Resources/
    EpisodeData/      EpisodeData_XXX.asset, MiniGameData_XXX.asset（ScriptableObject）
    HintData/         HintData_EpXX.asset（ScriptableObject）
    UI/               実行時に Resources.Load する Prefab（EpisodeTitlePanel 等）
  Prefabs/  Designs/  Settings/
```

## アーキテクチャ

- **起動フロー**: `Boot` シーン → `BootLoader` が `GameManager.GoToTitle()` → `Title` → `EpisodeSelect` → `Episode_XXX` / `MiniGame_XXX` → `ResultScene`
- **GameManager**（`DontDestroyOnLoad` のシングルトン）がシーン遷移・進行度・セーブを一元管理する。
  - シーン名は `Episode_{番号:D3}` / `MiniGame_{番号:D3}` の命名規則に依存している。`EpisodeData.nextSceneName` が指定されていればそちらが優先。
  - `sceneLoaded` で Episode/MiniGame シーンに `Resources/UI/EpisodeTitlePanel` を自動生成する。
  - 進行度は `stageOrder`（なければ `episodeIndex`）基準で `ClearedEpisodeIndex` として ES3 に保存。
- **シーン内マネージャ/UI** も `public static XXX Instance { get; private set; }` 形式のシングルトン（`InventoryManager`, `MessageUI`, `HintModalUI`, `ItemDetailUI`, 各ギミック UI など）。
  - 例外: `HesokuriManager`（へそくり）はシーンに無ければ `Instance` アクセス時に自動生成される遅延シングルトン。
- **ClickableObject** がシーン上のクリック対象の中心。`ObjectType` enum で振る舞いを切り替え、インスペクタで設定する設計。
  - 現在の値: `Examine` / `Item` / `StateChange` / `ItemUse` / `Okan` / `MultiStateChange` / `ItemUseMultiStateChange` / `ItemUseAndGetItem`（新規は末尾に追加）
  - `Item` 取得後の挙動は `ItemPostAction`（Hide / RemainAndShowText / ChangeSpriteAndShowText / SwapObject）。
- **エピソードシーンの主な構成要素**
  - `EpisodeManager` … シーン開始時に `HintData` を `HintModalUI` へ渡す
  - `RoomNavigationManager` … 左右（＋上下・拡大）の View 切り替えを DOTween でスライド
  - `ClearTrigger` … クリックでクリア → リザルト/次シーンへ遷移
  - `InventoryManager` / `ItemSlot` / `ItemDetailUI` … アイテム所持・選択・詳細表示
- データは ScriptableObject（`EpisodeData`, `HintData`, `Item`）でアセット化し、`Resources` から読み込む。

## 新しいエピソードを追加するとき

1. `Assets/Scenes/Episode_XXX.unity` を作成（既存エピソードを複製するのが早い）
2. `Resources/EpisodeData/EpisodeData_XXX.asset` を作成し、`stageOrder` / `episodeIndex` / `episodeTitle` 等を設定
3. `Resources/HintData/HintData_EpXX.asset` を作成し、シーンの `EpisodeManager` にセット
4. Build Settings にシーンを登録（Claude は手順を案内するだけで、アセットは直接編集しない）

## コーディング規約（既存コードに合わせること）

- **コメント・ログ・UI 文言は日本語**。XML ドキュメントコメント `/// <summary>` を public メソッドに付ける。
- 区切りコメント `// ================================================` や `// ----` でセクションを分ける既存スタイルを踏襲する。
- インスペクタ公開は `[SerializeField] private` を基本とし、`[Header("■ ...")]` / `[Tooltip]` / `[TextArea]` で整理する。
- 命名: クラス・メソッド・プロパティは PascalCase、private フィールドは camelCase（`_` プレフィックスなし）、定数は `UPPER_SNAKE_CASE`。
- ログには `[クラス名]` プレフィックスを付ける（例: `Debug.LogWarning("[GameManager] ...")`）。
- シングルトン参照時は `if (XXX.Instance != null)` で null チェックする。
- イベント購読は `OnEnable` で登録、`OnDisable` で解除する。
- DOTween の Tween/Sequence はオブジェクト破棄時に `Kill()` するか `SetLink(gameObject)` でリークを防ぐ。
- オブジェクト検索は Unity 6 の API（`FindFirstObjectByType` / `FindObjectsByType`）を使う。`FindObjectOfType` は非推奨。
- namespace は現状使っていない。新規スクリプトも既存に合わせてグローバル名前空間でよい。
- **ファイルは UTF-8（BOM 付き）・CRLF**。クラス名とファイル名を必ず一致させる（MonoBehaviour / ScriptableObject は 1 ファイル 1 クラス）。
- 新しい ScriptableObject には `[CreateAssetMenu]` を付け、Project ビューから作成できるようにする。
- セーブキーは `GameManager` のように `const string SAVE_KEY_XXX` で定数化し、文字列を直書きしない。

## Unity C# の実装指針

- **`UnityEngine.Object` に `?.` / `??` を使わない**（破棄済みオブジェクトの「偽 null」を判定できない）。`!= null` で比較する。
- `GetComponent` / `Find` 系は `Awake` / `Start` でキャッシュし、`Update` 内で毎フレーム呼ばない。基本は `[SerializeField]` でインスペクタ参照を渡す。
- クリック駆動のゲームなので、ポーリング用の `Update` は極力書かず、`Button.onClick` / `IPointerClickHandler` / イベントで処理する。
- 初期化順: 自身の初期化と `Instance` 設定は `Awake`、他オブジェクト参照は `Start` 以降で行う。
- 時間待ちはコルーチンか DOTween（`DOVirtual.DelayedCall` 等）を使う。ポーズ中も動かす UI 演出は `SetUpdate(true)`。
- UI 入力は EventSystem 経由（uGUI）。2D オブジェクトのクリックは `ClickableObject` 経由に揃え、独自の Raycast 処理を増やさない。
- 既存の `ClickableObject` に機能を足すと肥大化しやすい。独立したギミックは `Gimmick/` に新しいコンポーネントとして分ける。
- テスト用・デバッグ用のコード（例: `EpisodeManager.Start` のへそくり +1000 円）には `// ★テスト用` コメントを付け、リリース前に除去できるようにする。

## Unity 特有の注意事項

- **`.meta` ファイルを削除・手動生成しない**。スクリプトやアセットを移動・リネームする場合は `.meta` も一緒に移動し、GUID を保つ（GUID が変わるとシーン/Prefab の参照が切れる）。
- **`[SerializeField]` フィールド名を変更すると、シーン・Prefab に保存された値が失われる**。変更が必要なら `[FormerlySerializedAs("旧名")]` を付ける。
- enum（特に `ClickableObject.ObjectType` 等）はシリアライズ時に整数で保存されるため、**既存要素の途中に追加・並べ替えをしない**。新しい値は末尾に追加する。
- `.unity` / `.prefab` / `.asset` は YAML だが、**原則として直接編集しない**。必要な場合は変更内容をユーザーに説明し、Unity エディタ上での作業手順を提示する。
- `Resources.Load` のパス文字列（`"UI/EpisodeTitlePanel"`, `"EpisodeData"` 等）を変更・アセットを移動する際は、参照しているコードも合わせて修正する。
- 新しいシーンを追加したら Build Settings（`ProjectSettings/EditorBuildSettings.asset`）への登録が必要な旨をユーザーに伝える。（現状 `Episode_000` は未登録）
- `Library/`, `Temp/`, `obj/`, `Logs/`, `*.csproj`, `*.sln` は自動生成物なので編集しない。
- `Assets/Plugins/`（DOTween, Easy Save 3）と `Assets/TextMesh Pro/` はサードパーティ製なので変更しない。
- `UnityEditor` 名前空間を使うコードは `Editor/` フォルダに置くか `#if UNITY_EDITOR` で囲む（ビルドが通らなくなるため）。

## ビルド・確認

- Claude 側でコンパイルやプレイ確認はできない。コード変更後は **Unity エディタでのコンパイルエラー確認と、該当シーンでの動作確認をユーザーに依頼する**。
- 構文レベルの確認が必要な場合は `dotnet build Assembly-CSharp.csproj` が参考になるが、csproj は Unity が再生成するため結果は目安とする。
- デバッグ用のセーブ初期化は `GameManager` のコンテキストメニュー「Reset Save Data」。

## Git 運用

- コミットメッセージは日本語で、何を実装したかを短く書く（例: 「ほうきでハサミ発見」）。
- 「戻るならこれをRevert」というコミットは、作業の合間に入った小さな変更（`Boot.unity` / `UserSettings/`）を分けたもの。
- コミットやプッシュはユーザーに頼まれたときだけ行う。
