using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Warning severity and the ordering the validation dialog relies on.</summary>
    public class LevelWarningSeverityTests
    {
        /// <summary>Existing call sites construct warnings, so Warning has to be the default.</summary>
        [Fact]
        public void SeverityDefaultsToWarning()
        {
            Assert.Equal(LevelWarningSeverity.Warning, new LevelWarning("Validation.NoCandy").Severity);
        }

        [Fact]
        public void ErrorSeverityIsSetByInitializer()
        {
            LevelWarning error = new("Validation.Tutorial.UnknownEvent", "en", "tutorial01")
            {
                Severity = LevelWarningSeverity.Error,
            };

            Assert.Equal(LevelWarningSeverity.Error, error.Severity);
            Assert.Equal(["en", "tutorial01"], error.Args);
        }
    }
}
