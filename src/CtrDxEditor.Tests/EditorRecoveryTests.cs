using System;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the rules deciding when unsaved work is snapshotted, cleared, and restored.</summary>
    public class EditorRecoveryTests
    {
        private const string Level = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="640" height="480" />
            </layer>
            <layer name="Objects">
                <candy x="100" y="100" />
            </layer>
        </map>
        """;

        private static EditorViewModel Editor(InMemoryRecoveryStore? store)
        {
            return new EditorViewModel(new SpriteCache(new EmptyContentStore()), recovery: store);
        }

        private static void Edit(EditorViewModel vm, string x = "200")
        {
            vm.Document!.AllObjects[0].SetAttr("x", x);
        }

        /// <summary>A clean level writes nothing.</summary>
        [Fact]
        public async Task UnmodifiedLevelWritesNothing()
        {
            InMemoryRecoveryStore store = new();
            EditorViewModel vm = Editor(store);
            vm.LoadLevelXml(Level);

            Assert.False(await vm.TryCaptureRecoveryAsync("level.xml"));

            Assert.Equal(0, store.SaveCount);
        }

        /// <summary>An edit writes the live XML, baseline, decoration and file name.</summary>
        [Fact]
        public async Task EditWritesFullSnapshot()
        {
            InMemoryRecoveryStore store = new();
            EditorViewModel vm = Editor(store);
            vm.LoadLevelXml(Level);
            vm.ActiveRopeSkin = 1;
            vm.ActiveBackground = 2;
            // Any skin above 0 loads candy art synchronously, which EmptyContentStore cannot serve.
            vm.ActiveCandySkin = -1;
            vm.ActiveOmNomSupport = 4;
            Edit(vm);

            Assert.True(await vm.TryCaptureRecoveryAsync("level.xml"));

            RecoverySnapshot snapshot = Assert.IsType<RecoverySnapshot>(store.Stored);
            Assert.Equal(vm.ToXml(), snapshot.Xml);
            Assert.Equal(vm.SavedBaselineXml, snapshot.BaselineXml);
            Assert.Equal("level.xml", snapshot.FileName);
            Assert.Equal((1, 2, -1, 4), (snapshot.RopeSkin, snapshot.Background, snapshot.CandySkin, snapshot.OmNomSupport));
        }

        /// <summary>A second tick with no further edits does not rewrite.</summary>
        [Fact]
        public async Task UnchangedTickDoesNotRewrite()
        {
            InMemoryRecoveryStore store = new();
            EditorViewModel vm = Editor(store);
            vm.LoadLevelXml(Level);
            Edit(vm);
            _ = await vm.TryCaptureRecoveryAsync(null);

            Assert.False(await vm.TryCaptureRecoveryAsync(null));

            Assert.Equal(1, store.SaveCount);
        }

        /// <summary>Returning to the saved state clears a snapshot this session wrote.</summary>
        [Fact]
        public async Task BackToSavedClearsOwnSnapshot()
        {
            InMemoryRecoveryStore store = new();
            EditorViewModel vm = Editor(store);
            vm.LoadLevelXml(Level);
            Edit(vm);
            _ = await vm.TryCaptureRecoveryAsync(null);

            Edit(vm, "100");
            _ = await vm.TryCaptureRecoveryAsync(null);

            Assert.Null(store.Stored);
        }

        /// <summary>A clean level never clears a snapshot left by another session.</summary>
        [Fact]
        public async Task CleanLevelKeepsForeignSnapshot()
        {
            RecoverySnapshot foreign = new() { Xml = "<map>old</map>", BaselineXml = "<map />" };
            InMemoryRecoveryStore store = new() { Stored = foreign };
            EditorViewModel vm = Editor(store);
            vm.LoadLevelXml(Level);

            _ = await vm.TryCaptureRecoveryAsync(null);

            Assert.Same(foreign, store.Stored);
        }

        /// <summary>Opening another level after a Discard keeps the discarded work recoverable.</summary>
        [Fact]
        public async Task LoadingAnotherLevelKeepsSnapshot()
        {
            InMemoryRecoveryStore store = new();
            EditorViewModel vm = Editor(store);
            vm.LoadLevelXml(Level);
            Edit(vm);
            _ = await vm.TryCaptureRecoveryAsync(null);

            vm.LoadLevelXml(Level);
            _ = await vm.TryCaptureRecoveryAsync(null);

            Assert.NotNull(store.Stored);
        }

        /// <summary>Clearing after a save empties the slot and lets the next edit write again.</summary>
        [Fact]
        public async Task ClearAfterSaveEmptiesSlot()
        {
            InMemoryRecoveryStore store = new();
            EditorViewModel vm = Editor(store);
            vm.LoadLevelXml(Level);
            Edit(vm);
            _ = await vm.TryCaptureRecoveryAsync(null);

            vm.MarkSaved();
            await vm.ClearRecoveryAsync();

            Assert.Null(store.Stored);
            Edit(vm, "300");
            Assert.True(await vm.TryCaptureRecoveryAsync(null));
        }

        /// <summary>A restored level is modified, diffs against the old baseline, and carries decoration and name.</summary>
        [Fact]
        public async Task RestoreRebuildsEditorState()
        {
            InMemoryRecoveryStore store = new();
            EditorViewModel source = Editor(store);
            source.LoadLevelXml(Level);
            Edit(source);
            source.ActiveBackground = 5;
            _ = await source.TryCaptureRecoveryAsync("level.xml");
            RecoverySnapshot snapshot = store.Stored!;

            EditorViewModel vm = Editor(store);
            vm.RestoreSnapshot(snapshot);

            Assert.True(vm.IsModified);
            Assert.Equal(snapshot.BaselineXml, vm.SavedBaselineXml);
            Assert.Equal(snapshot.Xml, vm.ToXml());
            Assert.Equal(5, vm.ActiveBackground);
            Assert.Equal("level.xml", vm.RecoveredFileName);
        }

        /// <summary>Unparseable snapshot XML throws and leaves the editor empty.</summary>
        [Fact]
        public void RestoreOfCorruptXmlThrowsWithoutLoading()
        {
            EditorViewModel vm = Editor(new InMemoryRecoveryStore());

            _ = Assert.ThrowsAny<Exception>(() =>
                vm.RestoreSnapshot(new RecoverySnapshot { Xml = "<map", BaselineXml = "<map />" }));

            Assert.False(vm.HasDocument);
        }

        /// <summary>Loading, creating or closing a level forgets the recovered file name.</summary>
        [Fact]
        public void LoadForgetsRecoveredFileName()
        {
            EditorViewModel vm = Editor(new InMemoryRecoveryStore());
            vm.RestoreSnapshot(new RecoverySnapshot { Xml = Level, BaselineXml = "<map />", FileName = "a.xml" });

            vm.LoadLevelXml(Level);

            Assert.Null(vm.RecoveredFileName);
        }

        /// <summary>Without a store every recovery call is a no-op.</summary>
        [Fact]
        public async Task NullStoreIsNoOp()
        {
            EditorViewModel vm = Editor(null);
            vm.LoadLevelXml(Level);
            Edit(vm);

            Assert.False(await vm.TryCaptureRecoveryAsync(null));
            await vm.ClearRecoveryAsync();
        }
    }
}
