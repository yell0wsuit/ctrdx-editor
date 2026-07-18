using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests that property fields refresh live when the selected object is mutated outside the panel (e.g. a canvas drag).</summary>
    public class EditorFieldRefreshTests
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

        private const string Level = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="640" height="480" />
            </layer>
            <layer name="Objects">
                <candy x="100" y="100" />
            </layer>
        </map>
        """;

        /// <summary>Verifies that mutating the object and calling RefreshFieldValues raises change notification so bound fields re-read.</summary>
        [Fact]
        public void RefreshFieldValuesReReadsMutatedPosition()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject candy = vm.Document!.Objects[0];
            vm.SelectedObject = candy;

            AttributeFieldViewModel xField = vm.Fields.Single(f => f.Name == "x");
            List<string?> changes = [];
            xField.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AttributeFieldViewModel.Value))
                {
                    changes.Add(xField.Value);
                }
            };

            // Simulate a canvas drag: the canvas mutates the object directly, then signals a refresh.
            candy.X = 250;
            vm.RefreshFieldValues();

            Assert.Contains("250", changes);
            Assert.Equal("250", xField.Value);
        }

        /// <summary>A canvas split changes the hand's field structure, so refresh must add the new numbered section.</summary>
        [Fact]
        public void RefreshFieldValuesRebuildsHandSegmentsAfterCanvasSplit()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml("""
                <map>
                    <layer name="settings"><map gridSize="32" width="640" height="480" /></layer>
                    <layer name="Objects">
                        <hand x="100" y="200" segmentsCount="2"
                              segment1Angle="0" segment1Length="60" segment1Rotatable="true"
                              segment2Angle="90" segment2Length="40" segment2Rotatable="false" />
                    </layer>
                </map>
                """);
            LevelObject hand = vm.Document!.Objects[0];
            vm.SelectedObject = hand;

            _ = HandGeometry.SplitBone(hand, 1, new Vec2(125, 200));
            vm.RefreshFieldValues();

            Assert.Equal("3", vm.Fields.Single(field => field.Name == HandObject.CountAttr).Value);
            Assert.Contains(vm.Fields, field => field.Name == HandObject.AngleAttr(3));
            Assert.Equal([1, 2, 3], [.. vm.FieldGroups.Where(group => group.Index > 0).Select(group => group.Index)]);
        }
    }
}
