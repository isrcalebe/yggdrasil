using System.Globalization;
using Microsoft.Net.Http.Headers;

namespace yggdrasil.Modules.Profiles.Http;

internal static class DataVersionETag
{
    public static string Format(int dataVersion)
        => $"\"{dataVersion.ToString(CultureInfo.InvariantCulture)}\"";

    public static bool TryParse(IList<EntityTagHeaderValue> ifMatch, out int dataVersion)
    {
        dataVersion = 0;

        return ifMatch is [{ IsWeak: false } tag]
            && int.TryParse(tag.Tag.AsSpan().Trim('"'), NumberStyles.None, CultureInfo.InvariantCulture, out dataVersion);
    }
}
