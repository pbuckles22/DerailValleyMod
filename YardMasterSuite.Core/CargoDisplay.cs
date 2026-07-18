using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure cargo formatting for the local-car HUD bar. Locos omit the segment.
/// </summary>
public static class CargoDisplay
{
    public static string? Format(bool isLoco, string? cargoTypeName)
    {
        if (isLoco)
        {
            return null;
        }

        var name = cargoTypeName?.Trim();
        if (string.IsNullOrEmpty(name)
            || string.Equals(name, "None", System.StringComparison.OrdinalIgnoreCase)
            || name!.StartsWith("Empty", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Empty Cargo";
        }

        return $"Cargo {HumanizePascalCase(name)}";
    }

    internal static string HumanizePascalCase(string value)
    {
        if (value.Length <= 1)
        {
            return value;
        }

        var sb = new StringBuilder(value.Length + 8);
        sb.Append(value[0]);
        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && !char.IsUpper(value[i - 1]))
            {
                sb.Append(' ');
            }
            else if (char.IsUpper(c)
                && i + 1 < value.Length
                && char.IsLower(value[i + 1])
                && char.IsUpper(value[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
