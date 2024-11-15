using System.Text;
using Dalamud.Utility;
using Lumina.Text.ReadOnly;

namespace BlueMageHelper;

public static class Helper
{
    // From Ottermandias
    public static string ToTitleCaseExtended(ReadOnlySeString s, sbyte article = 0)
    {
        if (article == 1)
            return s.ToDalamudString().ToString();

        var sb        = new StringBuilder(s.ToDalamudString().ToString());
        var lastSpace = true;
        for (var i = 0; i < sb.Length; ++i)
        {
            if (sb[i] == ' ')
            {
                lastSpace = true;
            }
            else if (lastSpace)
            {
                lastSpace = false;
                sb[i]     = char.ToUpperInvariant(sb[i]);
            }
        }

        return sb.ToString();
    }
}