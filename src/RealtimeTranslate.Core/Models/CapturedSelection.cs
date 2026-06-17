namespace RealtimeTranslate.Core.Models;

public sealed record CapturedSelection(
    ScreenSelection Selection,
    string ScreenshotPath,
    byte[] CroppedImagePng);
