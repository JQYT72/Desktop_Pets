namespace DesktopPets.App.Models;

public sealed record CodexSpriteSheetState(int Row, int FrameCount, int FrameDurationMs)
{
    public static readonly IReadOnlyList<PetAnimationOption> Options =
    [
        new("idle", "待机"),
        new("running-right", "向右跑"),
        new("running-left", "向左跑"),
        new("waving", "挥手"),
        new("jumping", "跳跃"),
        new("failed", "失败"),
        new("waiting", "等待"),
        new("running", "工作"),
        new("review", "检查")
    ];

    public static readonly IReadOnlyDictionary<string, CodexSpriteSheetState> States =
        new Dictionary<string, CodexSpriteSheetState>(StringComparer.OrdinalIgnoreCase)
        {
            ["idle"] = new(0, 6, 160),
            ["running-right"] = new(1, 8, 90),
            ["running-left"] = new(2, 8, 90),
            ["waving"] = new(3, 4, 130),
            ["jumping"] = new(4, 5, 110),
            ["failed"] = new(5, 8, 150),
            ["waiting"] = new(6, 6, 170),
            ["running"] = new(7, 6, 120),
            ["review"] = new(8, 6, 170)
        };
}
