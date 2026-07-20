using System;
using System.IO;

using CtrDxEditor.Playtest;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for resolving a picked path to a runnable DX binary.</summary>
    public class DxExecutableResolverTests : IDisposable
    {
        private readonly string _root = Directory.CreateTempSubdirectory("ctrdx-resolve-").FullName;

        /// <inheritdoc />
        public void Dispose()
        {
            Directory.Delete(_root, recursive: true);
            GC.SuppressFinalize(this);
        }

        // Builds <root>/<name>.app/Contents/MacOS/<binaries...> and optionally an Info.plist naming
        // CFBundleExecutable. Returns the bundle path (what the user would pick in the file dialog).
        private string MakeBundle(string name, string? cfBundleExecutable, params string[] binaries)
        {
            string bundle = Path.Combine(_root, name + ".app");
            string macOs = Path.Combine(bundle, "Contents", "MacOS");
            _ = Directory.CreateDirectory(macOs);
            foreach (string binary in binaries)
            {
                File.WriteAllText(Path.Combine(macOs, binary), "#!/bin/sh\n");
            }
            if (cfBundleExecutable is not null)
            {
                File.WriteAllText(
                    Path.Combine(bundle, "Contents", "Info.plist"),
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
                    + "<plist version=\"1.0\"><dict>\n"
                    + "<key>CFBundleName</key><string>Decoy</string>\n"
                    + $"<key>CFBundleExecutable</key><string>{cfBundleExecutable}</string>\n"
                    + "</dict></plist>\n");
            }
            return bundle;
        }

        /// <summary>Verifies a plain (non-bundle) executable path is used unchanged.</summary>
        [Fact]
        public void NonBundlePathPassesThroughUnchanged()
        {
            string exe = Path.Combine(_root, "CutTheRopeDX");
            File.WriteAllText(exe, "#!/bin/sh\n");

            bool ok = DxExecutableResolver.TryResolve(exe, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(exe, resolved);
            Assert.Null(error);
        }

        /// <summary>Verifies a bundle resolves through CFBundleExecutable, not through its name.</summary>
        [Fact]
        public void BundleResolvesViaCfBundleExecutable()
        {
            string bundle = MakeBundle("CutTheRopeDX", "RealBinary", "RealBinary", "Other");

            bool ok = DxExecutableResolver.TryResolve(bundle, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(Path.Combine(bundle, "Contents", "MacOS", "RealBinary"), resolved);
            Assert.Null(error);
        }

        /// <summary>
        /// Verifies a bundle path ending in a separator still resolves. A bundle dragged from Finder
        /// arrives as a directory URI with a trailing slash, which would otherwise fail the .app
        /// suffix test and be treated as a plain folder to search.
        /// </summary>
        [Fact]
        public void BundlePathWithTrailingSeparatorResolves()
        {
            string bundle = MakeBundle("CutTheRope-DX", "CutTheRope-DX", "CutTheRope-DX");

            bool ok = DxExecutableResolver.TryResolve(
                bundle + Path.DirectorySeparatorChar, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(Path.Combine(bundle, "Contents", "MacOS", "CutTheRope-DX"), resolved);
            Assert.Null(error);
        }

        /// <summary>Verifies a bundle with no Info.plist falls back to the bundle's own name.</summary>
        [Fact]
        public void BundleWithoutPlistFallsBackToBundleName()
        {
            string bundle = MakeBundle("CutTheRopeDX", null, "CutTheRopeDX", "Other");

            bool ok = DxExecutableResolver.TryResolve(bundle, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(Path.Combine(bundle, "Contents", "MacOS", "CutTheRopeDX"), resolved);
            Assert.Null(error);
        }

        /// <summary>Verifies malformed plist XML falls back rather than throwing.</summary>
        [Fact]
        public void BundleWithUnparseablePlistFallsBackToBundleName()
        {
            string bundle = MakeBundle("CutTheRopeDX", null, "CutTheRopeDX");
            File.WriteAllText(Path.Combine(bundle, "Contents", "Info.plist"), "<plist><dict>truncated");

            bool ok = DxExecutableResolver.TryResolve(bundle, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(Path.Combine(bundle, "Contents", "MacOS", "CutTheRopeDX"), resolved);
            Assert.Null(error);
        }

        /// <summary>Verifies a plist naming a missing binary still falls back to a usable one.</summary>
        [Fact]
        public void BundleWithPlistNamingMissingBinaryFallsBack()
        {
            string bundle = MakeBundle("CutTheRopeDX", "DoesNotExist", "CutTheRopeDX");

            bool ok = DxExecutableResolver.TryResolve(bundle, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(Path.Combine(bundle, "Contents", "MacOS", "CutTheRopeDX"), resolved);
            Assert.Null(error);
        }

        /// <summary>Verifies a differently-named sole binary is used when the other rules miss.</summary>
        [Fact]
        public void BundleWithSoleBinaryResolvesToIt()
        {
            string bundle = MakeBundle("CutTheRopeDX", null, "Renamed");

            bool ok = DxExecutableResolver.TryResolve(bundle, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(Path.Combine(bundle, "Contents", "MacOS", "Renamed"), resolved);
            Assert.Null(error);
        }

        /// <summary>Verifies an empty MacOS directory is reported as an error, not a bad path.</summary>
        [Fact]
        public void BundleWithNoBinariesReportsError()
        {
            string bundle = MakeBundle("CutTheRopeDX", null);

            bool ok = DxExecutableResolver.TryResolve(bundle, out string resolved, out string? error);

            Assert.False(ok);
            Assert.Equal("", resolved);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        /// <summary>Verifies an ambiguous bundle (several candidates, no usable hint) is an error.</summary>
        [Fact]
        public void BundleWithAmbiguousBinariesReportsError()
        {
            string bundle = MakeBundle("CutTheRopeDX", null, "Alpha", "Beta");

            bool ok = DxExecutableResolver.TryResolve(bundle, out string resolved, out string? error);

            Assert.False(ok);
            Assert.Equal("", resolved);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        /// <summary>
        /// Verifies a plain directory holding one bundle resolves through it. macOS pickers cannot
        /// return a .app at all, so the user selects its containing folder instead.
        /// </summary>
        [Fact]
        public void DirectoryContainingSingleBundleResolvesToItsBinary()
        {
            string bundle = MakeBundle("CutTheRope-DX", "CutTheRope-DX", "CutTheRope-DX");

            bool ok = DxExecutableResolver.TryResolve(_root, out string resolved, out string? error);

            Assert.True(ok);
            Assert.Equal(Path.Combine(bundle, "Contents", "MacOS", "CutTheRope-DX"), resolved);
            Assert.Null(error);
        }

        /// <summary>Verifies a folder holding several bundles is an error rather than a guess.</summary>
        [Fact]
        public void DirectoryWithManyBundlesReportsError()
        {
            _ = MakeBundle("Safari", "Safari", "Safari");
            _ = MakeBundle("CutTheRope-DX", "CutTheRope-DX", "CutTheRope-DX");

            bool ok = DxExecutableResolver.TryResolve(_root, out string resolved, out string? error);

            Assert.False(ok);
            Assert.Equal("", resolved);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        /// <summary>Verifies a directory holding no bundle at all is reported as an error.</summary>
        [Fact]
        public void DirectoryWithNoBundleReportsError()
        {
            string empty = Path.Combine(_root, "empty");
            _ = Directory.CreateDirectory(empty);

            bool ok = DxExecutableResolver.TryResolve(empty, out string resolved, out string? error);

            Assert.False(ok);
            Assert.Equal("", resolved);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        /// <summary>Verifies a path that does not exist at all is reported as an error.</summary>
        [Fact]
        public void MissingPathReportsError()
        {
            bool ok = DxExecutableResolver.TryResolve(
                Path.Combine(_root, "nope"), out string resolved, out string? error);

            Assert.False(ok);
            Assert.Equal("", resolved);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }

        /// <summary>A bundle counts, with or without the trailing separator Finder adds to a drop.</summary>
        [Fact]
        public void BundleCountsAsABundle()
        {
            string bundle = MakeBundle("CutTheRope-DX", null, "CutTheRope-DX");

            Assert.True(DxExecutableResolver.IsBundleOrBundleContainer(bundle));
            Assert.True(DxExecutableResolver.IsBundleOrBundleContainer(bundle + Path.DirectorySeparatorChar));
        }

        /// <summary>The folder a bundle sits in counts too, which is the other thing users drag over.</summary>
        [Fact]
        public void FolderHoldingABundleCountsAsABundleContainer()
        {
            _ = MakeBundle("CutTheRope-DX", null, "CutTheRope-DX");

            Assert.True(DxExecutableResolver.IsBundleOrBundleContainer(_root));
        }

        /// <summary>
        /// The macOS drop is held to a bundle, so a stray document cannot be stored as the game path.
        /// A dev-build binary is refused here on purpose: it goes in through the typed path instead.
        /// </summary>
        [Fact]
        public void PlainFilesAndBundlelessFoldersAreNotBundles()
        {
            string level = Path.Combine(_root, "level.xml");
            File.WriteAllText(level, "<level />");
            string empty = Path.Combine(_root, "empty");
            _ = Directory.CreateDirectory(empty);
            string devBuild = Path.Combine(_root, "CutTheRopeDX");
            File.WriteAllText(devBuild, "#!/bin/sh\n");

            Assert.False(DxExecutableResolver.IsBundleOrBundleContainer(level));
            Assert.False(DxExecutableResolver.IsBundleOrBundleContainer(empty));
            Assert.False(DxExecutableResolver.IsBundleOrBundleContainer(devBuild));
            Assert.False(DxExecutableResolver.IsBundleOrBundleContainer(Path.Combine(_root, "nope")));
            Assert.False(DxExecutableResolver.IsBundleOrBundleContainer("  "));
        }
    }
}
