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

        /// <summary>DX keeps the finger artwork full-color, so its picker only offers clearing an imported tint.</summary>
        [Fact]
        public void FullColorIconDoesNotOfferCustomColor()
        {
            LevelObject obj = new(XElement.Parse("""<tutorial10 color="#FF0000" />"""));

            AttributeFieldViewModel color = Assert.Single(Build(obj), field => field.Name == "color");

            Assert.False(color.CanApplyCustomColor);
            color.Value = "";
            Assert.Null(obj.GetAttr("color"));
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

        /// <summary>An auto-width tutorial text keeps the manual width discoverable but disabled.</summary>
        [Fact]
        public void AutoTextPanelDisablesWidth()
        {
            LevelObject obj = new(new XElement(
                "tutorialText",
                new XAttribute("text", "hi")));
            TutorialObject.SetAutoWidth(obj, true);
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);
            Assert.Contains(fields, f => f.Name == "text");
            Assert.Contains(fields, f => f.Name == "autoWidth");
            Assert.False(fields.Single(f => f.Name == "width").IsEnabled);
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
            Assert.False(vm.Fields.Single(f => f.Name == "width").IsEnabled);

            TutorialTextResize.ApplyDrag(text, 120);
            vm.RefreshFieldValues();

            Assert.True(vm.Fields.Single(f => f.Name == "width").IsEnabled);
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

        /// <summary>A structural checkbox rebuild keeps the section the user opened expanded.</summary>
        [Fact]
        public void TriggerAreaTogglePreservesExpandedTriggerSection()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("tutorial01", 20, 30);
            vm.FieldGroups.Single(group => group.Header == "Trigger").IsExpanded = true;

            vm.Fields.Single(field => field.Name == "inArea").BoolValue = true;

            Assert.True(vm.FieldGroups.Single(group => group.Header == "Trigger").IsExpanded);

            vm.Fields.Single(field => field.Name == "inArea").BoolValue = false;

            Assert.True(vm.FieldGroups.Single(group => group.Header == "Trigger").IsExpanded);
        }

        /// <summary>A checkbox that changes dependent-field state does not collapse another open section.</summary>
        [Fact]
        public void AutoWidthTogglePreservesExpandedTutorialSections()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("tutorialText", 20, 30);
            vm.FieldGroups.Single(group => group.Header == "Look").IsExpanded = true;

            vm.Fields.Single(field => field.Name == "autoWidth").BoolValue = false;
            Assert.True(vm.FieldGroups.Single(group => group.Header == "Look").IsExpanded);

            vm.Fields.Single(field => field.Name == "autoWidth").BoolValue = true;
            Assert.True(vm.FieldGroups.Single(group => group.Header == "Look").IsExpanded);
        }

        /// <summary>Refreshing auto-width text updates disabled fields in place instead of rebuilding the panel.</summary>
        [Fact]
        public void AutoWidthRefreshKeepsExistingFieldInstances()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("tutorialText", 20, 30);
            AttributeFieldViewModel width = vm.Fields.Single(field => field.Name == "width");

            vm.RefreshFieldValues();

            Assert.Same(width, vm.Fields.Single(field => field.Name == "width"));
            Assert.False(width.IsEnabled);
        }

        /// <summary>Expansion state belongs to the selected object and never leaks to a new selection.</summary>
        [Fact]
        public void SelectingAnotherTutorialUsesItsOwnInitialCollapseState()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml("""
                <map>
                    <layer name="settings"><map gridSize="32" width="640" height="480" /></layer>
                    <layer name="Objects">
                        <tutorial01 x="20" y="30" />
                        <tutorial02 x="40" y="50" />
                    </layer>
                </map>
                """);
            LevelObject first = vm.Document!.AllObjects[0];
            LevelObject second = vm.Document.AllObjects[1];
            vm.SelectedObject = first;
            vm.FieldGroups.Single(group => group.Header == "Trigger").IsExpanded = true;

            vm.SelectedObject = second;

            Assert.False(vm.FieldGroups.Single(group => group.Header == "Trigger").IsExpanded);
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

        /// <summary>Changing showOn updates both XML and the selection the field reports back to the UI.</summary>
        [Fact]
        public void ShowOnSelectionRereadsTheChangedValue()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            AttributeFieldViewModel showOn = Build(obj).Single(f => f.Name == "showOn");

            showOn.SelectedOption = showOn.EnumOptions!.Single(o => o.Value == "ropeCut");

            Assert.Equal("ropeCut", obj.GetAttr("showOn"));
            Assert.Equal("ropeCut", showOn.SelectedOption!.Value);
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

        /// <summary>Changing subject updates both XML and the selection the field reports back to the UI.</summary>
        [Fact]
        public void SubjectSelectionRereadsTheChangedValue()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            AttributeFieldViewModel subject = Build(obj).Single(f => f.Name == "subject");

            subject.SelectedOption = subject.EnumOptions!.Single(o => o.Value == "primary");

            Assert.Equal("primary", obj.GetAttr("subject"));
            Assert.Equal("primary", subject.SelectedOption!.Value);
        }

        /// <summary>Effective DX defaults are visible without eagerly authoring redundant attributes.</summary>
        [Fact]
        public void DefaultValuesAreVisibleWithoutMutatingXml()
        {
            LevelObject obj = new(new XElement("tutorialText", new XAttribute("text", "hi")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);

            Assert.Equal("0", fields.Single(f => f.Name == "delay").Value);
            Assert.Equal("1", fields.Single(f => f.Name == "fadeIn").Value);
            Assert.Equal("5", fields.Single(f => f.Name == "duration").Value);
            Assert.Equal("0.5", fields.Single(f => f.Name == "fadeOut").Value);
            Assert.Equal("1", fields.Single(f => f.Name == "repeat").Value);
            Assert.Equal("1", fields.Single(f => f.Name == "opacity").Value);
            Assert.Equal("0", fields.Single(f => f.Name == "angle").Value);
            Assert.Equal("1", fields.Single(f => f.Name == "size").Value);
            Assert.Equal("1", fields.Single(f => f.Name == "lineHeight").Value);
            Assert.Null(obj.GetAttr("delay"));
            Assert.Null(obj.GetAttr("fadeIn"));
            Assert.Null(obj.GetAttr("duration"));
            Assert.Null(obj.GetAttr("fadeOut"));
            Assert.Null(obj.GetAttr("repeat"));
            Assert.Null(obj.GetAttr("opacity"));
            Assert.Null(obj.GetAttr("angle"));
            Assert.Null(obj.GetAttr("size"));
            Assert.Null(obj.GetAttr("lineHeight"));
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

        /// <summary>Clearing an optional color restores the game's default instead of authoring color="".</summary>
        [Fact]
        public void ClearingColorRemovesTheAttribute()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("color", "#FF0000")));
            AttributeFieldViewModel color = Build(obj).Single(f => f.Name == "color");

            color.Value = "";

            Assert.Null(obj.GetAttr("color"));
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
            _ = Assert.Single(harness.Fields, f => f.Name == "ease");

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

        /// <summary>inArea is off by default; its default rectangle remains visible but disabled until enabled.</summary>
        [Fact]
        public void TurningOnInAreaSeedsRectAndRevealsFields()
        {
            LevelObject obj = new(new XElement("tutorial01"));
            Harness harness = new(obj);
            Assert.Equal("0", harness.Fields.Single(f => f.Name == "inAreaX").Value);
            Assert.Equal("0", harness.Fields.Single(f => f.Name == "inAreaY").Value);
            Assert.Equal("100", harness.Fields.Single(f => f.Name == "inAreaWidth").Value);
            Assert.Equal("100", harness.Fields.Single(f => f.Name == "inAreaHeight").Value);
            Assert.All(
                harness.Fields.Where(f => f.Name.StartsWith("inArea", StringComparison.Ordinal) && f.Name != "inArea"),
                field => Assert.False(field.IsEnabled));

            harness.Fields.Single(f => f.Name == "inArea").Value = "true";

            Assert.Equal(1, harness.RebuildCount);
            Assert.Equal("0,0,100,100", obj.GetAttr("inArea"));
            Assert.Contains(harness.Fields, f => f.Name == "inAreaX");
            Assert.Contains(harness.Fields, f => f.Name == "inAreaY");
            Assert.Contains(harness.Fields, f => f.Name == "inAreaWidth");
            Assert.Contains(harness.Fields, f => f.Name == "inAreaHeight");
            Assert.All(
                harness.Fields.Where(f => f.Name.StartsWith("inArea", StringComparison.Ordinal) && f.Name != "inArea"),
                field => Assert.True(field.IsEnabled));
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

        /// <summary>Area controls show the whole coordinates DX actually uses, not fractional source values.</summary>
        [Fact]
        public void AreaFieldsDisplayDxRuntimeCoordinatesAsWholeNumbers()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("inArea", "1.9,2.9,3.9,4.9")));
            ObservableCollection<AttributeFieldViewModel> fields = Build(obj);

            Assert.Equal("1", fields.Single(f => f.Name == "inAreaX").Value);
            Assert.Equal("2", fields.Single(f => f.Name == "inAreaY").Value);
            Assert.Equal("3", fields.Single(f => f.Name == "inAreaWidth").Value);
            Assert.Equal("4", fields.Single(f => f.Name == "inAreaHeight").Value);
            Assert.All(fields.Where(f => f.Name.StartsWith("inArea", StringComparison.Ordinal) && f.Name != "inArea"),
                field => Assert.False(field.AllowsDecimal));
            Assert.Equal("1.9,2.9,3.9,4.9", obj.GetAttr("inArea"));
        }

        /// <summary>Turning inArea off removes the attribute while retaining disabled default coordinates.</summary>
        [Fact]
        public void TurningOffInAreaRemovesAttribute()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("inArea", "10,20,100,100")));
            Harness harness = new(obj);

            harness.Fields.Single(f => f.Name == "inArea").Value = "false";

            Assert.Null(obj.GetAttr("inArea"));
            Assert.False(harness.Fields.Single(f => f.Name == "inAreaX").IsEnabled);
            Assert.Equal("100", harness.Fields.Single(f => f.Name == "inAreaWidth").Value);
        }

        /// <summary>duration's forever toggle writes -1 but leaves its normal value visible and disabled.</summary>
        [Fact]
        public void HoldForeverTogglesDurationSentinelAndFieldVisibility()
        {
            LevelObject obj = new(new XElement("tutorialText", new XAttribute("text", "hi")));
            Harness harness = new(obj);
            Assert.Contains(harness.Fields, f => f.Name == "duration");

            harness.Fields.Single(f => f.Name == "holdsForever").Value = "true";

            Assert.Equal("-1", obj.GetAttr("duration"));
            Assert.Equal("5", harness.Fields.Single(f => f.Name == "duration").Value);
            Assert.False(harness.Fields.Single(f => f.Name == "duration").IsEnabled);

            harness.Fields.Single(f => f.Name == "holdsForever").Value = "false";

            Assert.Equal("5", obj.GetAttr("duration"));
            Assert.True(harness.Fields.Single(f => f.Name == "duration").IsEnabled);
        }

        /// <summary>
        /// repeat's forever toggle writes -1 and disables the normal count instead of hiding it;
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
            Assert.Equal("1", harness.Fields.Single(f => f.Name == "repeat").Value);
            Assert.False(harness.Fields.Single(f => f.Name == "repeat").IsEnabled);

            harness.Fields.Single(f => f.Name == "repeatsForever").Value = "false";

            Assert.Equal("1", obj.GetAttr("repeat"));
            Assert.True(harness.Fields.Single(f => f.Name == "repeat").IsEnabled);
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

        /// <summary>Clearing a group removes it so DX does not index every cleared prompt under group="".</summary>
        [Fact]
        public void ClearingGroupRemovesTheAttribute()
        {
            LevelObject obj = new(new XElement("tutorial01", new XAttribute("group", "intro")));
            AttributeFieldViewModel group = Build(obj).Single(f => f.Name == "group");

            group.Value = "";

            Assert.Null(obj.GetAttr("group"));
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
