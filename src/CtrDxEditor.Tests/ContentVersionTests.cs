using System.Threading.Tasks;

using CtrDxEditor.Content;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the asset-bundle revision check behind the startup re-download prompt.</summary>
    public class ContentVersionTests
    {
        private sealed class ManifestStore(string json) : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(relPath == ContentManifest.FileName);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult<byte[]>([]);
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return relPath == ContentManifest.FileName
                    ? Task.FromResult(json)
                    : Task.FromException<string>(new System.IO.FileNotFoundException(relPath));
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(true);
            }
        }

        /// <summary>Reads the revision a bundle declares.</summary>
        [Fact]
        public void ParsesADeclaredVersion()
        {
            Assert.Equal(3, ContentManifest.ParseVersion("""{"version": 3, "files": {}}"""));
        }

        /// <summary>
        /// A manifest without the field is revision 0, which is what every bundle published before the
        /// field existed must read as.
        /// </summary>
        [Fact]
        public void TreatsAnAbsentVersionAsTheOriginalRevision()
        {
            Assert.Equal(0, ContentManifest.ParseVersion("""{"files": {}}"""));
        }

        /// <summary>
        /// A manifest the editor cannot make sense of is "unknown", distinct from revision 0, so it
        /// cannot be mistaken for a bundle that merely predates the field.
        /// </summary>
        [Theory]
        [InlineData("""{"version": "1", "files": {}}""")]
        [InlineData("""{"version": 1.5, "files": {}}""")]
        [InlineData("""{"version": null}""")]
        [InlineData("not json at all")]
        [InlineData("")]
        public void ReportsAnUnusableManifestAsUnknown(string json)
        {
            Assert.Null(ContentManifest.ParseVersion(json));
        }

        /// <summary>Adding the version field leaves the file map readable by the same parser.</summary>
        [Fact]
        public void KeepsParsingFilesAlongsideAVersion()
        {
            const string json = """{"version": 1, "files": {"images/a.png": "abc"}}""";

            Assert.Equal(1, ContentManifest.ParseVersion(json));
            Assert.Equal("abc", ContentManifest.ParseFiles(json)["images/a.png"]);
        }

        /// <summary>A bundle behind this build is offered as a re-download.</summary>
        [Fact]
        public void ReportsAnOlderBundleAsOutdated()
        {
            Assert.True(ContentVersion.IsOutdated(ContentVersion.CurrentAssetVersion - 1));
        }

        /// <summary>The current revision, and anything ahead of it, is left alone.</summary>
        [Fact]
        public void ReportsCurrentAndNewerBundlesAsUpToDate()
        {
            Assert.False(ContentVersion.IsOutdated(ContentVersion.CurrentAssetVersion));
            Assert.False(ContentVersion.IsOutdated(ContentVersion.CurrentAssetVersion + 1));
        }

        /// <summary>The store path reads the revision out of the installed manifest.</summary>
        [Fact]
        public async Task ReadsTheInstalledRevisionFromTheManifest()
        {
            ManifestStore current = new(
                $$$"""{"version": {{{ContentVersion.CurrentAssetVersion}}}, "files": {}}""");

            Assert.False(await ContentVersion.IsOutdatedAsync(current));
        }

        /// <summary>A bundle predating the version field is the case the prompt exists for.</summary>
        [Fact]
        public async Task ReportsAPreVersionBundleAsOutdated()
        {
            ManifestStore legacy = new("""{"files": {}}""");

            Assert.True(await ContentVersion.IsOutdatedAsync(legacy));
        }

        /// <summary>
        /// A content root with no readable manifest is left alone rather than treated as stale, so an
        /// unusual but working install is never nagged into a large download.
        /// </summary>
        [Fact]
        public async Task StaysSilentWhenTheManifestCannotBeRead()
        {
            Assert.False(await ContentVersion.IsOutdatedAsync(new EmptyContentStore()));
        }
    }
}
