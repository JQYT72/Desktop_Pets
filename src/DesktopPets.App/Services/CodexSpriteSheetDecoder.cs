using System.IO;
using System.Windows.Media.Imaging;
using DesktopPets.App.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DesktopPets.App.Services;

public sealed class CodexSpriteSheetDecoder
{
    public const int CellWidth = 192;
    public const int CellHeight = 208;
    public const int Columns = 8;
    public const int Rows = 9;

    public IReadOnlyList<BitmapSource> DecodeState(string spritesheetPath, string stateName)
    {
        if (!CodexSpriteSheetState.States.TryGetValue(stateName, out var state))
        {
            state = CodexSpriteSheetState.States["idle"];
        }

        if (!File.Exists(spritesheetPath))
        {
            throw new FileNotFoundException("没有找到 spritesheet 文件。", spritesheetPath);
        }

        using var image = Image.Load<Rgba32>(spritesheetPath);
        if (image.Width < Columns * CellWidth || image.Height < Rows * CellHeight)
        {
            throw new NotSupportedException("Codex spritesheet 尺寸至少需要 1536x1872。");
        }

        var sourcePixels = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(sourcePixels);

        var frames = new List<BitmapSource>(state.FrameCount);
        for (var frame = 0; frame < state.FrameCount; frame++)
        {
            frames.Add(CreateFrame(sourcePixels, image.Width, state.Row, frame));
        }

        return frames;
    }

    private static BitmapSource CreateFrame(byte[] sourcePixels, int sourceWidth, int row, int column)
    {
        var framePixels = new byte[CellWidth * CellHeight * 4];
        var sourceX = column * CellWidth;
        var sourceY = row * CellHeight;
        var sourceStride = sourceWidth * 4;
        var targetStride = CellWidth * 4;

        for (var y = 0; y < CellHeight; y++)
        {
            var sourceOffset = ((sourceY + y) * sourceStride) + (sourceX * 4);
            var targetOffset = y * targetStride;
            Buffer.BlockCopy(sourcePixels, sourceOffset, framePixels, targetOffset, targetStride);
        }

        var bitmap = BitmapSource.Create(
            CellWidth,
            CellHeight,
            96,
            96,
            System.Windows.Media.PixelFormats.Bgra32,
            null,
            ConvertRgba32ToBgra32(framePixels),
            CellWidth * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private static byte[] ConvertRgba32ToBgra32(byte[] rgba32)
    {
        var bgra32 = new byte[rgba32.Length];
        for (var i = 0; i < rgba32.Length; i += 4)
        {
            bgra32[i] = rgba32[i + 2];
            bgra32[i + 1] = rgba32[i + 1];
            bgra32[i + 2] = rgba32[i];
            bgra32[i + 3] = rgba32[i + 3];
        }

        return bgra32;
    }
}
