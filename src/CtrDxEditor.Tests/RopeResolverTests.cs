using System.Collections.Generic;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests that rope resolution mirrors the game's LoadGrabs bungee-creation gate.</summary>
    public class RopeResolverTests
    {
        private static LevelObject Obj(string name, params (string, string)[] attrs)
        {
            XElement e = new(name);
            foreach ((string k, string val) in attrs)
            {
                e.SetAttributeValue(k, val);
            }
            return new LevelObject(e);
        }

        /// <summary>A normal grab (radius -1) resolves to the candy, matching the game building a bungee.</summary>
        [Fact]
        public void NormalGrabResolvesToCandy()
        {
            LevelObject grab = Obj("grab", ("radius", "-1"));
            LevelObject candy = Obj("candy");
            List<LevelObject> objects = [grab, candy];

            RopeTarget target = RopeResolver.Resolve(grab, objects, twoParts: false);

            Assert.Equal(RopeTargetKind.Candy, target.Kind);
            Assert.Equal(candy, target.Target);
        }

        /// <summary>An auto-catch grab (positive radius) resolves to no rope: the game skips the binding block.</summary>
        [Fact]
        public void AutoCatchGrabResolvesToNoRope()
        {
            LevelObject grab = Obj("grab", ("radius", "100"));
            LevelObject candy = Obj("candy");
            List<LevelObject> objects = [grab, candy];

            RopeTarget target = RopeResolver.Resolve(grab, objects, twoParts: false);

            Assert.Equal(RopeTargetKind.None, target.Kind);
            Assert.Null(target.Target);
        }
    }
}
