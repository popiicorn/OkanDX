/// <summary>
/// ClickableObject と同じオブジェクトに付けたコンポーネントが、クリックを先に受け取るための仕組み。
/// TryInterceptClick が true を返すと、ClickableObject の通常の処理（メッセージ表示など）は行われない。
/// </summary>
public interface IClickInterceptor
{
    /// <summary>
    /// クリックを処理する。処理した場合は true を返す
    /// </summary>
    bool TryInterceptClick();
}
