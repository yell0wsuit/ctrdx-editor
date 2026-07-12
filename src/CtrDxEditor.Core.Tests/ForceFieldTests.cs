using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests exact directional-emitter geometry ported from the game.</summary>
    public class ForceFieldTests
    {
        /// <summary>Steam uses its maximum height and all three game valve levels.</summary>
        [Fact]
        public void SteamTubeUsesExactMaximumReachAndLevelMarks()
        {
            ForceFieldSpec field = ForceFieldTable.For("steamTube")!;

            Assert.Equal(141, field.Reach);
            Assert.Equal(-90, field.DirectionOffset);
            Assert.Equal([32.9, 94, 141], field.LevelMarks);
            Assert.Equal(141, field.LevelReach(spriteScale: 1));
            Assert.Equal([32.9, 94, 141], field.LevelMarkDistances(spriteScale: 1));
        }

        /// <summary>Pump remains an unsegmented 624-unit force field.</summary>
        [Fact]
        public void PumpRemainsUnsegmented()
        {
            ForceFieldSpec field = ForceFieldTable.For("pump")!;

            Assert.Equal(624, field.Reach);
            Assert.Empty(field.LevelMarks);
            Assert.Equal(208, field.LevelReach(spriteScale: 1));
        }
    }
}
