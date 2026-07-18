using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests moving-grab fields and incompatible mode normalization.</summary>
    public class GrabMovementFieldTests
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

        /// <summary>Grabs reuse mover controls without inheriting self-spin.</summary>
        [Fact]
        public void GrabExposesMovementButNotSelfSpin()
        {
            EditorViewModel vm = Vm("<grab x='100' y='100' length='100' moveLength='-1' kickable='false' />");

            Assert.Contains(vm.Fields, f => f.Name == "movementMode");
            Assert.DoesNotContain(vm.Fields, f => f.Name is "spin" or "spinSpeed" or "spinClockwise");
        }

        /// <summary>Choosing a mover path normalizes incompatible grab variants.</summary>
        /// <param name="mode">Orbit or Polyline.</param>
        [Theory]
        [InlineData("orbit")]
        [InlineData("polyline")]
        public void EnablingMovementClearsRailAndSuctionCup(string mode)
        {
            EditorViewModel vm = Vm("<grab x='100' y='100' length='100' moveLength='80' kickable='true' />");
            LevelObject grab = vm.SelectedObject!;
            AttributeFieldViewModel movement = vm.Fields.Single(f => f.Name == "movementMode");

            movement.SelectedOption = movement.EnumOptions!.Single(o => o.Value == mode);

            Assert.Equal("-1", grab.GetAttr("moveLength"));
            Assert.Equal("false", grab.GetAttr("kickable"));
            Assert.True(Core.Editing.MoverPath.HasActiveMovement(grab));
        }

        /// <summary>Rail and suction controls clear movement when explicitly enabled.</summary>
        /// <param name="fieldName">Synthetic rail or suction field.</param>
        [Theory]
        [InlineData("movable")]
        [InlineData("kickable")]
        public void EnablingIncompatibleGrabModeClearsMovement(string fieldName)
        {
            EditorViewModel vm = Vm("<grab x='100' y='100' length='100' moveLength='-1' kickable='false' path='RC40' moveSpeed='50' />");
            AttributeFieldViewModel field = vm.Fields.Single(f => f.Name == fieldName);

            Assert.False(field.IsEnabled);
            // Legacy conflicts are preserved until a compatibility control is explicitly edited.
            field.IsEnabled = true;
            field.BoolValue = true;

            Assert.Null(vm.SelectedObject!.GetAttr("path"));
            Assert.Null(vm.SelectedObject.GetAttr("moveSpeed"));
        }

        /// <summary>The pollen visibility setting is meaningful only for active movement.</summary>
        [Theory]
        [InlineData("", false)]
        [InlineData(" path='RC40' moveSpeed='50'", true)]
        public void HidePathAppearsOnlyForMovingGrab(string moverAttributes, bool expected)
        {
            EditorViewModel vm = Vm($"<grab x='100' y='100' length='100'{moverAttributes} />");

            Assert.Equal(expected, vm.Fields.Any(f => f.Name == "hidePath"));
        }

        private static EditorViewModel Vm(string grabXml)
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml($"<map><layer name='settings'><map gridSize='32' width='640' height='480' /></layer><layer name='Objects'>{grabXml}</layer></map>");
            vm.SelectedObject = vm.Document!.AllObjects[0];
            return vm;
        }
    }
}
