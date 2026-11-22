using System.Reflection;

namespace Bferal.VentoryDesktop;

/// <summary>
/// Token opcional incrustado en el build de release (CI). No sustituye buenas prácticas:
/// cualquier secreto dentro del .exe puede extraerse con herramientas.
/// </summary>
internal static class VentoryBuildSecrets
{
    private const string MetadataKey = "VentoryEmbeddedGitHubToken";

    internal static string? EmbeddedGitHubToken
    {
        get
        {
            foreach (var attr in typeof(VentoryBuildSecrets).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
            {
                if (attr.Key == MetadataKey && !string.IsNullOrWhiteSpace(attr.Value))
                    return attr.Value.Trim();
            }

            return null;
        }
    }
}