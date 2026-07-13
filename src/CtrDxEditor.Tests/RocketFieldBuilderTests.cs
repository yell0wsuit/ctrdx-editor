using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
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

        [Fact]
        public void ImpulseFieldsCarryHelpText()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build();
            Assert.True(fields.Single(f => f.Name == "impulse").HasHelp);
            Assert.True(fields.Single(f => f.Name == "impulseFactor").HasHelp);
        }

        [Fact]
        public void UntimedRocketHidesBurnTimeAndTogglesOff()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build(("time", "-1"));
            Assert.Equal("false", fields.Single(f => f.Name == "timed").Value);
            Assert.DoesNotContain(fields, f => f.Name == "time");
        }

        [Fact]
        public void TimedRocketShowsBurnTimeField()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, _) = Build(("time", "5"));
            Assert.Equal("true", fields.Single(f => f.Name == "timed").Value);
            Assert.Contains(fields, f => f.Name == "time");
        }

        [Fact]
        public void TogglingTimedOnWritesPositiveTime()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject rocket) = Build(("time", "-1"));
            fields.Single(f => f.Name == "timed").Value = "true";
            Assert.Equal("5", rocket.GetAttr("time"));
        }

        [Fact]
        public void TogglingTimedOffWritesMinusOne()
        {
            (ObservableCollection<AttributeFieldViewModel> fields, LevelObject rocket) = Build(("time", "5"));
            fields.Single(f => f.Name == "timed").Value = "false";
            Assert.Equal("-1", rocket.GetAttr("time"));
        }
    }
}
