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

        /// <summary>Verifies that document locales are discovered with English first.</summary>
        [Fact]
        public void AvailableLocalesUnionsDocumentLocalesWithEnFirst()
        {
            EditorViewModel vm = Create();

            Assert.Equal("en", vm.AvailableLocales[0]);
            Assert.Contains("es", vm.AvailableLocales);
            Assert.Contains("ru", vm.AvailableLocales);
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

        private static EditorViewModel Create()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(Localized);
            return vm;
        }
    }
}
