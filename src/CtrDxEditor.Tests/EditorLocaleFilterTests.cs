using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests locale discovery and session-only localized-object filtering.</summary>
    public class EditorLocaleFilterTests
    {
        private const string Localized = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
            <layer name="text">
                <tutorialText x="1" y="2" locale="en" text="Hello" width="100" />
                <tutorialText x="1" y="2" locale="es" text="Hola" width="100" />
                <tutorialText x="1" y="2" locale="ru" text="Privet" width="100" />
            </layer>
            <layer name="objects"><candy x="9" y="9" /></layer>
        </map>
        """;

        private const string LocalizedWithoutEnglish = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
            <layer name="text">
                <tutorialText x="1" y="2" locale="fr" text="Bonjour" width="100" />
                <tutorialText x="1" y="2" locale="es" text="Hola" width="100" />
            </layer>
        </map>
        """;

        /// <summary>Verifies that document locales are discovered with English first.</summary>
        [Fact]
        public void AvailableLocalesUnionsDocumentLocalesWithEnFirst()
        {
            EditorViewModel vm = Create();

            Assert.Equal("en", vm.AvailableLocales[0]);
            Assert.Equal(0, vm.DisplayLocaleIndex);
            Assert.Contains("es", vm.AvailableLocales);
            Assert.Contains("ru", vm.AvailableLocales);
        }

        /// <summary>Without English, the first locale encountered in the document is selected.</summary>
        [Fact]
        public void MultipleLocalesWithoutEnglishSelectFirstDocumentLocale()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));

            vm.LoadLevelXml(LocalizedWithoutEnglish);

            Assert.Equal(["fr", "es"], vm.AvailableLocales);
            Assert.Equal("fr", vm.DisplayLocale);
            Assert.Equal(0, vm.DisplayLocaleIndex);
            Assert.True(vm.HasLocalizedText);
        }

        /// <summary>The picker index and display locale remain synchronized in both directions.</summary>
        [Fact]
        public void DisplayLocaleIndexSynchronizesPickerSelection()
        {
            EditorViewModel vm = Create();

            vm.DisplayLocaleIndex = 1;
            Assert.Equal("es", vm.DisplayLocale);

            vm.DisplayLocale = "ru";
            Assert.Equal(2, vm.DisplayLocaleIndex);
        }

        /// <summary>A single localized language is selected but does not need a visible picker.</summary>
        [Fact]
        public void SingleLocaleSelectsAvailableLanguageAndHidesPicker()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml("""
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
                <layer name="text"><tutorialText x="1" y="2" locale="es" text="Hola" width="100" /></layer>
            </map>
            """);

            Assert.Equal(["es"], vm.AvailableLocales);
            Assert.Equal("es", vm.DisplayLocale);
            Assert.False(vm.HasLocalizedText);
        }

        /// <summary>Verifies that localized objects outside the selected locale are effectively hidden.</summary>
        [Fact]
        public void NonSelectedLocaleTextIsEffectivelyHidden()
        {
            EditorViewModel vm = Create();

            ObservableCollection<LevelObject> text = vm.Layers.First(layer => layer.Name == "text").Objects;
            LevelObject es = text.First(obj => obj.GetAttr("locale") == "es");
            LevelObject en = text.First(obj => obj.GetAttr("locale") == "en");
            Assert.Contains(es, vm.EffectivelyHiddenObjects);
            Assert.DoesNotContain(en, vm.EffectivelyHiddenObjects);
        }

        /// <summary>Verifies that objects without a locale remain visible under locale filtering.</summary>
        [Fact]
        public void LocaleLessObjectsAreNeverLocaleHidden()
        {
            EditorViewModel vm = Create();

            LevelObject candy = vm.Layers.First(layer => layer.Name == "objects").Objects[0];
            Assert.DoesNotContain(candy, vm.EffectivelyHiddenObjects);
        }

        /// <summary>Verifies that changing the selected locale immediately recomputes visibility.</summary>
        [Fact]
        public void SwitchingLocaleUpdatesHiddenSet()
        {
            EditorViewModel vm = Create();

            vm.DisplayLocale = "es";

            ObservableCollection<LevelObject> text = vm.Layers.First(layer => layer.Name == "text").Objects;
            LevelObject es = text.First(obj => obj.GetAttr("locale") == "es");
            LevelObject en = text.First(obj => obj.GetAttr("locale") == "en");
            Assert.Contains(en, vm.EffectivelyHiddenObjects);
            Assert.DoesNotContain(es, vm.EffectivelyHiddenObjects);
        }

        /// <summary>The eye toggle is inert for an off-locale object; the language picker governs it instead.</summary>
        [Fact]
        public void TogglingOffLocaleObjectVisibilityIsNoOp()
        {
            EditorViewModel vm = Create();
            ObservableCollection<LevelObject> text = vm.Layers.First(layer => layer.Name == "text").Objects;
            LevelObject es = text.First(obj => obj.GetAttr("locale") == "es");

            vm.ToggleObjectVisibility(es);

            Assert.Equal("en", vm.DisplayLocale);
            Assert.Contains(es, vm.EffectivelyHiddenObjects);
        }

        /// <summary>Toggling a current-locale object hides it individually without changing the language.</summary>
        [Fact]
        public void TogglingCurrentLocaleObjectHidesItWithoutSwitchingLocale()
        {
            EditorViewModel vm = Create();
            ObservableCollection<LevelObject> text = vm.Layers.First(layer => layer.Name == "text").Objects;
            LevelObject en = text.First(obj => obj.GetAttr("locale") == "en");

            vm.ToggleObjectVisibility(en);

            Assert.Equal("en", vm.DisplayLocale);
            Assert.Contains(en, vm.EffectivelyHiddenObjects);
        }

        /// <summary>Opening another document resets locale and visibility choices from the previous document.</summary>
        [Fact]
        public void LoadingAnotherLevelResetsDocumentVisibilityState()
        {
            EditorViewModel vm = Create();
            vm.DisplayLocale = "es";
            vm.SetLayerHidden(vm.Layers.First(layer => layer.Name == "objects").Layer, true);

            vm.LoadLevelXml(Localized);

            Assert.Equal("en", vm.DisplayLocale);
            LayerViewModel objects = vm.Layers.First(layer => layer.Name == "objects");
            Assert.True(objects.IsVisible);
            Assert.DoesNotContain(objects.Objects[0], vm.EffectivelyHiddenObjects);
        }

        /// <summary>A level with multiple locales has switchable localized text.</summary>
        [Fact]
        public void HasLocalizedTextTrueWithMultipleLocales()
        {
            EditorViewModel vm = Create();

            Assert.True(vm.HasLocalizedText);
        }

        /// <summary>A level with no locale attributes has nothing to switch, so the picker is not useful.</summary>
        [Fact]
        public void HasLocalizedTextFalseWithoutLocales()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml("""
            <?xml version='1.0' encoding='utf-8'?>
            <map>
                <layer name="settings"><map width="320" height="480" /><gameDesign ropePhysicsSpeed="1" /></layer>
                <layer name="a"><candy x="1" y="2" /></layer>
            </map>
            """);

            Assert.False(vm.HasLocalizedText);
        }

        /// <summary>Closing a multilingual level notifies the picker that localized text is gone.</summary>
        [Fact]
        public void ClosingLevelNotifiesHasLocalizedTextChanged()
        {
            EditorViewModel vm = Create();
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.CloseLevel();

            Assert.False(vm.HasLocalizedText);
            Assert.Contains(nameof(EditorViewModel.HasLocalizedText), changed);
        }

        private static EditorViewModel Create()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(Localized);
            return vm;
        }
    }
}
