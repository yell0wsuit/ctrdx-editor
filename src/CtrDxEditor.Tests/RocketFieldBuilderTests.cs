using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the rocket properties-panel fields.</summary>
    public class RocketFieldBuilderTests
    {
        private static (ObservableCollection<AttributeFieldViewModel>, LevelObject) Build(params (string, string)[] attrs)
        {
            XElement e = new("rocket");
            foreach ((string k, string v) in attrs)
            {
                e.SetAttributeValue(k, v);
            }
            LevelObject rocket = new(e);
            ObservableCollection<AttributeFieldViewModel> fields = [];
            RocketFieldBuilder.Build(fields, rocket, () => { }, () => { }, () => { });
            return (fields, rocket);
        }

        /// <summary>Both impulse fields carry help text, since neither name explains its units on its own.</summary>
        [Fact]
        public void ImpulseFieldsCarryHelpText()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build();
            Assert.True(fields.Single(f => f.Name == "impulse").HasHelp);
            Assert.True(fields.Single(f => f.Name == "impulseFactor").HasHelp);
        }

        /// <summary>A negative <c>time</c> means the rocket burns forever, so the toggle reads false and the burn-time field stays hidden.</summary>
        [Fact]
        public void UntimedRocketHidesBurnTimeAndTogglesOff()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build(("time", "-1"));
            Assert.Equal("false", fields.Single(f => f.Name == "timed").Value);
            Assert.DoesNotContain(fields, f => f.Name == "time");
        }

        /// <summary>A positive <c>time</c> flips the toggle on and reveals the burn-time field for editing.</summary>
        [Fact]
        public void TimedRocketShowsBurnTimeField()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build(("time", "5"));
            Assert.Equal("true", fields.Single(f => f.Name == "timed").Value);
            Assert.Contains(fields, f => f.Name == "time");
        }

        /// <summary>Turning the toggle on replaces the sentinel with a positive default rather than leaving -1 in the document.</summary>
        [Fact]
        public void TogglingTimedOnWritesPositiveTime()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject rocket) = Build(("time", "-1"));
            fields.Single(f => f.Name == "timed").Value = "true";
            Assert.Equal("5", rocket.GetAttr("time"));
        }

        /// <summary>Turning the toggle off writes the -1 sentinel back, which is how the game encodes an untimed rocket.</summary>
        [Fact]
        public void TogglingTimedOffWritesMinusOne()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject rocket) = Build(("time", "5"));
            fields.Single(f => f.Name == "timed").Value = "false";
            Assert.Equal("-1", rocket.GetAttr("time"));
        }

        /// <summary>
        /// The game calls <c>ParseMover</c> on rockets, so they take mover paths; a rocket's own rotation is
        /// the player's to aim, so it gets movement without the self-spin controls.
        /// </summary>
        [Fact]
        public void RocketExposesMovementButNotSelfSpin()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build();
            Assert.Equal("none", fields.Single(f => f.Name == "movementMode").Value);
            Assert.DoesNotContain(fields, f => f.Name is "spin" or "spinSpeed" or "spinClockwise");
        }

        /// <summary>Choosing Orbit seeds a circular DX path and the speed that drives it.</summary>
        [Fact]
        public void ChoosingOrbitWritesCircularPath()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject rocket) = Build();
            fields.Single(f => f.Name == "movementMode").Value = "orbit";

            Assert.Equal("RC30", rocket.GetAttr("path"));
            Assert.True(MoverPath.HasActiveMovement(rocket));
        }

        /// <summary>Choosing Polyline seeds a real segment, so the canvas has something to draw and edit.</summary>
        [Fact]
        public void ChoosingPolylineWritesMovingSegment()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject rocket) = Build();
            fields.Single(f => f.Name == "movementMode").Value = "polyline";

            Assert.True(MoverPath.IsPolylineMovement(rocket.GetAttr("path")));
            Assert.True(MoverPath.HasActiveMovement(rocket));
        }
    }
}
