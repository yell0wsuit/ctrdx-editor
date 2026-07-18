using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Serializes tests that instantiate Avalonia's canvas property registry.</summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class LevelCanvasTestGroup
    {
        /// <summary>The shared xUnit collection name for tests that construct <c>LevelCanvas</c>.</summary>
        public const string Name = "LevelCanvas";
    }
}
