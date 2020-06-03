
using Microsoft.Extensions.FileProviders;

namespace Octokit.Test
{
    public static class EmbeddedFiles
    {
        public static EmbeddedFileProvider Provider { get; } =
            new EmbeddedFileProvider(typeof(EmbeddedFiles).Assembly, "");
    }
}
