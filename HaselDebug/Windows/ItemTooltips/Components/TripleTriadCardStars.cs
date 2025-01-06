using System.Numerics;
using Dalamud.Interface.Utility;
using HaselCommon.Gui.Yoga;
using HaselCommon.Services;
using ImGuiNET;

namespace HaselDebug.Windows.ItemTooltips.Components;

public class TripleTriadCardStars(TextureService textureService) : Node()
{
    public int Stars { get; set; }

    public override void DrawContent()
    {
        var starSize = 32 * 0.75f * ImGuiHelpers.GlobalScale;
        var starRadius = starSize / 1.666f;
        var starCenter = AbsolutePosition + new Vector2(ComputedMarginLeft, ComputedMarginTop) + new Vector2(starSize) / 2f;

        void DrawStar(StarPosition pos)
        {
            var angleIncrement = 2 * MathF.PI / 5; // 5 = amount of stars
            var angle = (int)pos * angleIncrement - MathF.PI / 2;

            ImGui.SetCursorPos(starCenter + new Vector2(starRadius * MathF.Cos(angle), starRadius * MathF.Sin(angle)));
            textureService.DrawPart("CardTripleTriad", 1, 1, starSize);
        }

        if (Stars >= 1)
        {
            DrawStar(StarPosition.Top);

            if (Stars >= 2)
                DrawStar(StarPosition.Left);
            if (Stars >= 3)
                DrawStar(StarPosition.Right);
            if (Stars >= 4)
                DrawStar(StarPosition.BottomLeft);
            if (Stars >= 5)
                DrawStar(StarPosition.BottomRight);
        }
    }

    private enum StarPosition
    {
        Top = 0,
        Right = 1,
        Left = 4,
        BottomLeft = 3,
        BottomRight = 2
    }
}
