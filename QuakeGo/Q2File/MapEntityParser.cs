using System.Globalization;
using System.Text.RegularExpressions;
using OpenTK.Mathematics;

namespace GoQuake2.Q2File;

public static partial class MapEntityParser
{
    [GeneratedRegex("\\\"(?<key>[^\\\"]*)\\\"\\s*\\\"(?<value>[^\\\"]*)\\\"")]
    private static partial Regex PropertyRegex();

    [GeneratedRegex("\\{(?<body>.*?)\\}", RegexOptions.Singleline)]
    private static partial Regex EntityRegex();

    public static MapEntity[] Parse(string text)
    {
        var result = new List<MapEntity>();

        foreach (Match entityMatch in EntityRegex().Matches(text))
        {
            var entity = new MapEntity();

            foreach (Match propertyMatch in PropertyRegex().Matches(entityMatch.Groups["body"].Value))
            {
                entity.Properties[propertyMatch.Groups["key"].Value] = propertyMatch.Groups["value"].Value;
            }

            result.Add(entity);
        }

        return result.ToArray();
    }

    public static bool TryGetOrigin(MapEntity entity, out Vector3 origin)
    {
        origin = Vector3.Zero;

        if (!entity.TryGet("origin", out var raw))
        {
            return false;
        }

        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            return false;
        }

        origin = new Vector3(x, y, z);
        return true;
    }

    public static float GetAngleRadians(MapEntity entity, float fallback = 0f)
    {
        if (!entity.TryGet("angle", out var raw) ||
            !float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees))
        {
            return fallback;
        }

        return MathHelper.DegreesToRadians(degrees);
    }
}
