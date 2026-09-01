using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests the reminder that fires when a level drops Time Travel's rocket physics. Rockets already
    /// placed keep the impulse they were authored with, so the editor points the author at them rather
    /// than rewriting values it did not choose.
    /// </summary>
    public class RocketImpulseReviewTests
    {
        /// <summary>Dropping the flag on a level that holds rockets is worth interrupting for.</summary>
        [Fact]
        public void TurningTheFlagOffWithRocketsAsksForAReview()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"", "<rocket x=\"10\" y=\"20\" impulse=\"5\" />");

            Assert.True(RocketObject.ImpulseNeedsReview(doc.Settings, Off(doc.Settings), doc));
        }

        /// <summary>With no rocket in the level there is nothing to review, so the reminder stays quiet.</summary>
        [Fact]
        public void TurningTheFlagOffWithoutRocketsStaysQuiet()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"", "<star x=\"10\" y=\"20\" />");

            Assert.False(RocketObject.ImpulseNeedsReview(doc.Settings, Off(doc.Settings), doc));
        }

        /// <summary>Leaving the flag on changes nothing about the authored impulses.</summary>
        [Fact]
        public void KeepingTheFlagOnStaysQuiet()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"", "<rocket x=\"10\" y=\"20\" impulse=\"5\" />");

            Assert.False(RocketObject.ImpulseNeedsReview(doc.Settings, doc.Settings, doc));
        }

        /// <summary>A level that never used the flag has nothing to be reminded about.</summary>
        [Fact]
        public void ALevelWithoutTheFlagStaysQuiet()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\"", "<rocket x=\"10\" y=\"20\" impulse=\"20\" />");

            Assert.False(RocketObject.ImpulseNeedsReview(doc.Settings, Off(doc.Settings), doc));
        }

        /// <summary>
        /// Turning off mobile physics clears the Time Travel flag with it, and that route to losing the
        /// tuning deserves the same reminder as unticking the flag directly.
        /// </summary>
        [Fact]
        public void TurningOffMobilePhysicsAlsoAsksForAReview()
        {
            LevelDocument doc = Doc("useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"", "<rocket x=\"10\" y=\"20\" impulse=\"5\" />");
            LevelSettings after = doc.Settings with { UseMobilePhysics = false, UseTimeTravelRocketPhysics = false };

            Assert.True(RocketObject.ImpulseNeedsReview(doc.Settings, after, doc));
        }

        /// <summary>The reminder counts every rocket it is asking the author to revisit.</summary>
        [Fact]
        public void EveryRocketIsCounted()
        {
            LevelDocument doc = Doc(
                "useMobilePhysics=\"true\" useTimeTravelRocketPhysics=\"true\"",
                "<rocket x=\"10\" y=\"20\" impulse=\"5\" /><star x=\"1\" y=\"2\" /><rocket x=\"30\" y=\"40\" impulse=\"7\" />");

            Assert.Equal(2, RocketObject.RocketsIn(doc).Count());
        }

        private static LevelSettings Off(LevelSettings settings)
        {
            return settings with { UseTimeTravelRocketPhysics = false };
        }

        private static LevelDocument Doc(string designAttributes, string objects)
        {
            return LevelDocument.Parse($"""
                <map>
                    <layer name="settings">
                        <map gridSize="32" width="1024" height="576" />
                        <gameDesign {designAttributes} />
                    </layer>
                    <layer name="Objects">{objects}</layer>
                </map>
                """);
        }
    }
}
