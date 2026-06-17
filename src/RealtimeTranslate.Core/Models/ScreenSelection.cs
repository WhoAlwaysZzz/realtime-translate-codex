namespace RealtimeTranslate.Core.Models;

public sealed record ScreenSelection(int X, int Y, int Width, int Height)
{
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
