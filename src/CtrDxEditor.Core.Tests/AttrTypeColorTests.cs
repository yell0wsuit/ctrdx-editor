using System;

using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>The properties panel needs a color type; a hex string in a text box is not one.</summary>
    public class AttrTypeColorTests
    {
        [Fact]
        public void ColorIsAnAttributeType()
        {
            Assert.True(Enum.IsDefined(AttrType.Color));
        }
    }
}
