using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CtrDxEditor.Content
{
    /// <summary>Acquires the content asset bundle and persists it for an <see cref="IContentStore"/> to read.</summary>
    public interface IContentInstaller
    {
        /// <summary>Downloads the bundle from the release, reporting fractional progress in [0, 1].</summary>
        Task InstallFromDownloadAsync(IProgress<double>? progress, CancellationToken ct);

        /// <summary>Installs the bundle from an already-obtained zip stream (upload / local file).</summary>
        Task InstallFromZipAsync(Stream zipStream, CancellationToken ct);
    }
}
