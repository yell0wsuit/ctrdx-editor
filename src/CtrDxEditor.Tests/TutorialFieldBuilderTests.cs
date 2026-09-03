using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>The tutorial properties panel exposes icon and text editing controls.</summary>
    public class TutorialFieldBuilderTests
    {
        private sealed class EmptyStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(false);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult(Array.Empty<byte>());
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult("");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(false);
            }
        }

        private static ObservableCollection<AttributeFieldViewModel> Build(LevelObject obj)
        {
            ObservableCollection<AttributeFieldViewModel> fields = [];
            TutorialFieldBuilder.Build(fields, obj, new SpriteCache(new EmptyStore()), () => { }, () => { }, () => { });
            return fields;
        }

        /// <summary>Rebuilds the field list in place, the way EditorViewModel's structural callback does.</summary>
        private sealed class Harness
        {
            private readonly LevelObject _obj;
            private readonly SpriteCache _sprites = new(new EmptyStore());

            public Harness(LevelObject obj)
            {
                _obj = obj;
                Populate();
            }

            public ObservableCollection<AttributeFieldViewModel> Fields { get; } = [];

            public int RebuildCount { get; private set; }

            private void Populate()
            {
                Fields.Clear();
                TutorialFieldBuilder.Build(Fields, _obj, _sprites, () => { }, () => { }, Rebuild);
            }

            private void Rebuild()
            {
                RebuildCount++;
                Populate();
            }
        }

        /// <summary>Builds an eleven-choice icon picker and an angle field for tutorial icons.</summary>
        [Fact]
        public void IconPanelHasIconPickerAndAngle()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("angle", "0")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel icon = fields.Single(f => f.Name == "icon");
            Assert.Equal(11, icon.EnumOptions!.Length);
            Assert.Contains(fields, f => f.Name == "angle");
        }

        /// <summary>Renames the underlying element when a different tutorial icon is selected.</summary>
        [Fact]
        public void SelectingIconRenamesElement()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel icon = fields.Single(f => f.Name == "icon");
            icon.SelectedOption = icon.EnumOptions!.First(option => option.Value == "tutorial07");
            Assert.Equal("tutorial07", obj.Type);
        }

        /// <summary>A fixed-width (manual) tutorial text shows text, the auto-width toggle, and width.</summary>
        [Fact]
        public void ManualTextPanelHasTextAutoAndWidth()
        {
            LevelObject obj = new(new XElement(
                "tutorialText",
                new XAttribute("text", "hi"),
                new XAttribute("width", "140")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            Assert.Contains(fields, f => f.Name == "text");
            Assert.Contains(fields, f => f.Name == "autoWidth");
            Assert.Contains(fields, f => f.Name == "width");
        }

        /// <summary>An auto-width tutorial text hides the manual width field.</summary>
        [Fact]
        public void AutoTextPanelHidesWidth()
        {
            LevelObject obj = new(new XElement(
                "tutorialText",
                new XAttribute("text", "hi")));
            TutorialObject.SetAutoWidth(obj, true);
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            Assert.Contains(fields, f => f.Name == "text");
            Assert.Contains(fields, f => f.Name == "autoWidth");
            Assert.DoesNotContain(fields, f => f.Name == "width");
        }

        /// <summary>Uses the property field and F2 overlay without exposing a redundant panel command.</summary>
        [Fact]
        public void ViewModelHasNoTutorialEditButtonCommand()
        {
            Assert.Null(typeof(EditorViewModel).GetProperty("CanEditTutorialText"));
            Assert.Null(typeof(EditorViewModel).GetProperty("EditTutorialTextCommand"));
            Assert.Null(typeof(EditorViewModel).GetEvent("TutorialTextEditRequested"));
        }

        /// <summary>Shows manual width after a canvas resize disables auto-width.</summary>
        [Fact]
        public void RefreshFieldsShowsWidthAfterCanvasResize()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            LevelObject text = vm.PlaceObject("tutorialText", 20, 30)!;
            Assert.DoesNotContain(vm.Fields, f => f.Name == "width");

            TutorialTextResize.ApplyDrag(text, 120);
            vm.RefreshFieldValues();

            Assert.Contains(vm.Fields, f => f.Name == "width");
        }

        /// <summary>Groups appear content, Trigger, Timing, Look, Motion, in that order.</summary>
        [Fact]
        public void GroupsAppearInBriefOrder()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            PropertyGroupViewModel[] groups = [.. EditorViewModel.GroupFields(fields)];

            Assert.False(groups[0].HasHeader);
            Assert.Equal(["Trigger", "Timing", "Look", "Motion"], groups.Skip(1).Select(g => g.Header));
        }

        /// <summary>A freshly placed icon has every attribute at its default, so every titled section starts collapsed.</summary>
        [Fact]
        public void DefaultObjectCollapsesEveryTitledSection()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            PropertyGroupViewModel[] groups = [.. EditorViewModel.GroupFields(fields)];

            foreach (PropertyGroupViewModel group in groups.Where(g => g.HasHeader))
            {
                Assert.False(group.IsExpanded);
            }
        }

        /// <summary>An authored non-default Timing attribute starts that section expanded, not the others.</summary>
        [Fact]
        public void NonDefaultAttributeExpandsOnlyItsOwnSection()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("fadeIn", "2")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            PropertyGroupViewModel[] groups = [.. EditorViewModel.GroupFields(fields)];

            Assert.True(groups.Single(g => g.Header == "Timing").IsExpanded);
            Assert.False(groups.Single(g => g.Header == "Trigger").IsExpanded);
            Assert.False(groups.Single(g => g.Header == "Look").IsExpanded);
            Assert.False(groups.Single(g => g.Header == "Motion").IsExpanded);
        }

        /// <summary>showOn lists all 31 tutorial events, edge events before the state conditions.</summary>
        [Fact]
        public void ShowOnListsAllThirtyOneEventsEdgeFirst()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel showOn = fields.Single(f => f.Name == "showOn");

            Assert.Equal(31, showOn.EnumOptions!.Length);
            Assert.Equal("start", showOn.EnumOptions![0].Value);
            Assert.Equal("bubbled", showOn.EnumOptions![24].Value);
            Assert.Equal("candyMoved", showOn.EnumOptions![^1].Value);
        }

        /// <summary>subject offers exactly any/primary/left/right.</summary>
        [Fact]
        public void SubjectOffersFourOptions()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel subject = fields.Single(f => f.Name == "subject");

            Assert.Equal(["any", "primary", "left", "right"], subject.EnumOptions!.Select(o => o.Value));
        }

        /// <summary>Reading a color field never rewrites the raw attribute, whatever spelling or casing it carries.</summary>
        [Fact]
        public void ColorFieldDisplaysRawTextWithoutRewriting()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("color", "#ff0000")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel color = fields.Single(f => f.Name == "color");

            Assert.Equal("#ff0000", color.Value);
            Assert.Equal("#ff0000", obj.GetAttr("color"));
        }

        /// <summary>Building the panel alone, with no edit, never calls SetAttr on an authored color.</summary>
        [Fact]
        public void UnrelatedEditDoesNotTouchAnUntouchedColor()
        {
            LevelObject obj = new(new XElement(
                "tutorial01", new XAttribute("color", "1, 2, 3"), new XAttribute("angle", "0")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);

            fields.Single(f => f.Name == "angle").Value = "45";

            Assert.Equal("1, 2, 3", obj.GetAttr("color"));
        }

        /// <summary>An actual edit always writes hex, even when the prior spelling was a triplet.</summary>
        [Fact]
        public void EditingATripletColorWritesHex()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("color", "1,2,3")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel color = fields.Single(f => f.Name == "color");

            color.Value = "10,20,30";

            Assert.Equal("#0A141E", obj.GetAttr("color"));
        }

        /// <summary>An edit that parses as hex is re-emitted uppercase, per FormatHex.</summary>
        [Fact]
        public void EditingToLowercaseHexNormalizesToUppercase()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("color", "#000000")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel color = fields.Single(f => f.Name == "color");

            color.Value = "#ff9900";

            Assert.Equal("#FF9900", obj.GetAttr("color"));
        }

        /// <summary>An edit that doesn't parse at all is written verbatim, so the field stays typeable.</summary>
        [Fact]
        public void UnparseableColorEditStaysTypeable()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel color = fields.Single(f => f.Name == "color");

            color.Value = "purple";

            Assert.Equal("purple", obj.GetAttr("color"));
        }

        /// <summary>color is offered even on the two full-color quads the validator flags it on.</summary>
        [Theory]
        [InlineData("tutorial10")]
        [InlineData("tutorial11")]
        public void ColorIsOfferedOnFullColorQuads(string element)
        {
            LevelObject obj = new(new XElement(element));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);

            Assert.Contains(fields, f => f.Name == "color");
        }

        /// <summary>size and lineHeight are text-only, never offered on a sign icon.</summary>
        [Fact]
        public void SizeAndLineHeightAreTextOnly()
        {
            LevelObject icon = new(new XElement("tutorial01"));
            LevelObject text = new(new XElement("tutorialText", new XAttribute("text", "hi")));

            Assert.DoesNotContain(Build(icon), f => f.Name == "size");
            Assert.DoesNotContain(Build(icon), f => f.Name == "lineHeight");
            Assert.Contains(Build(text), f => f.Name == "size");
            Assert.Contains(Build(text), f => f.Name == "lineHeight");
        }

        /// <summary>A fresh icon starts with no motion fields beyond the mode picker itself, reading None.</summary>
        [Fact]
        public void FreshIconStartsAtMotionNone()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel motion = fields.Single(f => f.Name == "motion");

            Assert.Equal("none", motion.Value);
            Assert.DoesNotContain(fields, f => f.Name == "path");
        }

        /// <summary>Switching motion to Looping rebuilds the panel and reveals path and moveSpeed, not ease.</summary>
        [Fact]
        public void SwitchingToLoopingRevealsPathAndSpeedOnly()
        {
            LevelObject obj = new(new XElement("tutorial10"));
            Harness harness = new(obj);
            AttributeFieldViewModel motion = harness.Fields.Single(f => f.Name == "motion");

            motion.SelectedOption = motion.EnumOptions!.Single(o => o.Value == "looping");

            Assert.Equal(1, harness.RebuildCount);
            Assert.NotNull(obj.GetAttr("path"));
            Assert.Contains(harness.Fields, f => f.Name == "path");
            Assert.Contains(harness.Fields, f => f.Name == "moveSpeed");
            Assert.DoesNotContain(harness.Fields, f => f.Name == "moveDelay");
            Assert.DoesNotContain(harness.Fields, f => f.Name == "ease");
        }

        /// <summary>Switching motion to Timed reveals moveDelay and one ease dropdown per leg.</summary>
        [Fact]
        public void SwitchingToTimedRevealsMoveDelayAndEasePerLeg()
        {
            LevelObject obj = new(new XElement("tutorial10", new XAttribute("path", "100,0,200,0")));
            Harness harness = new(obj);
            AttributeFieldViewModel motion = harness.Fields.Single(f => f.Name == "motion");

            motion.SelectedOption = motion.EnumOptions!.Single(o => o.Value == "timed");

            Assert.Equal(1, harness.RebuildCount);
            Assert.Contains(harness.Fields, f => f.Name == "moveDelay");
            Assert.Equal(2, harness.Fields.Count(f => f.Name == "ease"));
        }

        /// <summary>Editing the path to add a leg rebuilds the panel with a matching extra ease dropdown.</summary>
        [Fact]
        public void EditingPathChangesEaseLegCount()
        {
            LevelObject obj = new(new XElement(
                "tutorial10", new XAttribute("path", "100,0"), new XAttribute("moveDelay", "0")));
            Harness harness = new(obj);
            Assert.Single(harness.Fields, f => f.Name == "ease");

            harness.Fields.Single(f => f.Name == "path").Value = "100,0,200,0";

            Assert.Equal(1, harness.RebuildCount);
            Assert.Equal(2, harness.Fields.Count(f => f.Name == "ease"));
        }

        /// <summary>Setting every leg to the same ease serializes the single-value shorthand.</summary>
        [Fact]
        public void AgreeingLegsSerializeToShorthand()
        {
            LevelObject obj = new(new XElement(
                "tutorial10", new XAttribute("path", "100,0,200,0"), new XAttribute("moveDelay", "0")));
            Harness harness = new(obj);
            AttributeFieldViewModel[] eases = [.. harness.Fields.Where(f => f.Name == "ease")];
            Assert.Equal(2, eases.Length);

            eases[0].SelectedOption = eases[0].EnumOptions!.Single(o => o.Value == "in");
            Assert.Equal("in,none", obj.GetAttr("ease"));

            eases[1].SelectedOption = eases[1].EnumOptions!.Single(o => o.Value == "in");
            Assert.Equal("in", obj.GetAttr("ease"));
        }

        /// <summary>Toggling motion to None clears path, ease and moveDelay in one rebuild.</summary>
        [Fact]
        public void SwitchingBackToNoneClearsMotionFields()
        {
            LevelObject obj = new(new XElement(
                "tutorial10", new XAttribute("path", "100,0"), new XAttribute("moveDelay", "0")));
            Harness harness = new(obj);
            AttributeFieldViewModel motion = harness.Fields.Single(f => f.Name == "motion");

            motion.SelectedOption = motion.EnumOptions!.Single(o => o.Value == "none");

            Assert.Null(obj.GetAttr("path"));
            Assert.Null(obj.GetAttr("moveDelay"));
            Assert.DoesNotContain(harness.Fields, f => f.Name == "path");
        }

        /// <summary>inArea is off by default; turning it on seeds a rectangle and reveals four number fields.</summary>
        [Fact]
        public void TurningOnInAreaSeedsRectAndRevealsFields()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            Harness harness = new(obj);
            Assert.DoesNotContain(harness.Fields, f => f.Name == "inAreaX");

            harness.Fields.Single(f => f.Name == "inArea").Value = "true";

            Assert.Equal(1, harness.RebuildCount);
            Assert.Equal("0,0,100,100", obj.GetAttr("inArea"));
            Assert.Contains(harness.Fields, f => f.Name == "inAreaX");
            Assert.Contains(harness.Fields, f => f.Name == "inAreaY");
            Assert.Contains(harness.Fields, f => f.Name == "inAreaWidth");
            Assert.Contains(harness.Fields, f => f.Name == "inAreaHeight");
        }

        /// <summary>Editing one inArea component preserves the other three.</summary>
        [Fact]
        public void EditingOneAreaComponentPreservesTheOthers()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("inArea", "10,20,100,100")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);

            fields.Single(f => f.Name == "inAreaWidth").Value = "50";

            Assert.Equal("10,20,50,100", obj.GetAttr("inArea"));
        }

        /// <summary>Turning inArea off removes the attribute and hides the four fields.</summary>
        [Fact]
        public void TurningOffInAreaRemovesAttribute()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("inArea", "10,20,100,100")));
            Harness harness = new(obj);

            harness.Fields.Single(f => f.Name == "inArea").Value = "false";

            Assert.Null(obj.GetAttr("inArea"));
            Assert.DoesNotContain(harness.Fields, f => f.Name == "inAreaX");
        }

        /// <summary>duration's forever toggle writes -1 and hides the numeric field; unchecking restores a real value.</summary>
        [Fact]
        public void HoldForeverTogglesDurationSentinelAndFieldVisibility()
        {
            LevelObject obj = new(new XElement("tutorialText", new XAttribute("text", "hi")));
            Harness harness = new(obj);
            Assert.Contains(harness.Fields, f => f.Name == "duration");

            harness.Fields.Single(f => f.Name == "holdsForever").Value = "true";

            Assert.Equal("-1", obj.GetAttr("duration"));
            Assert.DoesNotContain(harness.Fields, f => f.Name == "duration");

            harness.Fields.Single(f => f.Name == "holdsForever").Value = "false";

            Assert.Equal("5", obj.GetAttr("duration"));
            Assert.Contains(harness.Fields, f => f.Name == "duration");
        }

        /// <summary>
        /// repeat's forever toggle writes -1 and hides the numeric field the same way duration does;
        /// unchecking it restores the schema's real default of one pass, not an arbitrary "2".
        /// </summary>
        [Fact]
        public void RepeatForeverTogglesRepeatSentinelAndFieldVisibility()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            Harness harness = new(obj);
            Assert.Contains(harness.Fields, f => f.Name == "repeat");

            harness.Fields.Single(f => f.Name == "repeatsForever").Value = "true";

            Assert.Equal("-1", obj.GetAttr("repeat"));
            Assert.DoesNotContain(harness.Fields, f => f.Name == "repeat");

            harness.Fields.Single(f => f.Name == "repeatsForever").Value = "false";

            Assert.Equal("1", obj.GetAttr("repeat"));
            Assert.Contains(harness.Fields, f => f.Name == "repeat");
        }

        /// <summary>group is a sequencing tag, not a trigger condition, so it belongs in Timing.</summary>
        [Fact]
        public void GroupFieldBelongsToTiming()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            AttributeFieldViewModel group = fields.Single(f => f.Name == "group");

            Assert.Equal("Timing", group.GroupHeader);
        }

        /// <summary>An authored group tag alone starts Timing expanded, and leaves Trigger collapsed.</summary>
        [Fact]
        public void AuthoredGroupExpandsTimingNotTrigger()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("group", "intro")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            PropertyGroupViewModel[] groups = [.. EditorViewModel.GroupFields(fields)];

            Assert.True(groups.Single(g => g.Header == "Timing").IsExpanded);
            Assert.False(groups.Single(g => g.Header == "Trigger").IsExpanded);
        }

        /// <summary>rotateSpeed is offered only in Looping - the shared mover reads it, the timeline can't.</summary>
        [Fact]
        public void RotateSpeedIsLoopingOnly()
        {
            LevelObject looping = new(new XElement("tutorial10", new XAttribute("path", "100,0")));
            LevelObject timed = new(new XElement(
                "tutorial10", new XAttribute("path", "100,0"), new XAttribute("moveDelay", "0")));
            LevelObject none = new(new XElement("tutorial10"));

            Assert.Equal(TutorialMotionMode.Looping, TutorialMotion.ModeOf(looping));
            Assert.Contains(Build(looping), f => f.Name == "rotateSpeed");
            Assert.DoesNotContain(Build(timed), f => f.Name == "rotateSpeed");
            Assert.DoesNotContain(Build(none), f => f.Name == "rotateSpeed");
        }

        /// <summary>
        /// Typing a repeat count into Timing on a currently-Looping prompt reclassifies its motion:
        /// ModeOf treats any authored repeat as a Timed marker on a path-bearing prompt, so the edit
        /// must rebuild the panel or the Motion group is left showing a stale "Looping".
        /// </summary>
        [Fact]
        public void EditingTimingRepeatOnALoopingPromptRebuildsAsTimed()
        {
            LevelObject obj = new(new XElement("tutorial10", new XAttribute("path", "100,0")));
            Harness harness = new(obj);
            Assert.Equal("looping", harness.Fields.Single(f => f.Name == "motion").Value);

            harness.Fields.Single(f => f.Name == "repeat").Value = "3";

            Assert.Equal(1, harness.RebuildCount);
            Assert.Equal(TutorialMotionMode.Timed, TutorialMotion.ModeOf(obj));
            Assert.Equal("timed", harness.Fields.Single(f => f.Name == "motion").Value);
            Assert.Equal("3", obj.GetAttr("repeat"));
            // ease was never authored; TutorialMotion.Timed treats that the same as "none" per leg,
            // so no seeding is needed for the panel to read and display Timed motion correctly.
            Assert.NotNull(TutorialMotion.Timed(obj));
        }
    }
}
