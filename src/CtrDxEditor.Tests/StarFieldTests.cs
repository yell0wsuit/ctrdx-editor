using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests for star-specific property panel fields.</summary>
    public class StarFieldTests
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
                <star x="100" y="100" timeout="-1" />
            </layer>
        </map>
        """;

        /// <summary>Checking spin on a star writes a default positive whole rotateSpeed and reveals controls.</summary>
        [Fact]
        public void CheckingStarSpinWritesDefaultRotateSpeed()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;

            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;

            Assert.Equal("70", star.GetAttr("rotateSpeed"));
            Assert.Equal("70", vm.Fields.Single(f => f.Name == "spinSpeed").Value);
            Assert.True(vm.Fields.Single(f => f.Name == "spinClockwise").BoolValue);
        }

        /// <summary>Spin's static mover path does not expose polyline nodes until Polyline is selected.</summary>
        [Fact]
        public void CheckingSpinDoesNotEnablePolylineEditing()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;

            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;

            Assert.Equal("0,0", star.GetAttr("path"));
            Assert.Equal("none", vm.Fields.Single(f => f.Name == "movementMode").Value);
            Assert.False(vm.CanEditPolyline);

            vm.Fields.Single(f => f.Name == "movementMode").Value = "polyline";

            Assert.Equal("100,0", star.GetAttr("path"));
            Assert.True(vm.CanEditPolyline);
        }

        /// <summary>Orbit can be enabled without rotateSpeed and exposes radius/direction separately.</summary>
        [Fact]
        public void CheckingStarOrbitWithoutSpinWritesCircularPathRadius()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;

            Assert.False(vm.Fields.Single(f => f.Name == "spin").BoolValue);
            vm.Fields.Single(f => f.Name == "movementMode").Value = "orbit";

            Assert.Null(star.GetAttr("rotateSpeed"));
            Assert.Equal("RC30", star.GetAttr("path"));
            Assert.Equal("70", star.GetAttr("moveSpeed"));
            Assert.False(vm.Fields.Single(f => f.Name == "spin").BoolValue);
            Assert.Equal("orbit", vm.Fields.Single(f => f.Name == "movementMode").Value);
            Assert.DoesNotContain(vm.Fields, f => f.Name == "spinSpeed");
            Assert.DoesNotContain(vm.Fields, f => f.Name == "spinClockwise");
            AttributeFieldViewModel radius = vm.Fields.Single(f => f.Name == "orbitRadius");
            Assert.Equal("30", radius.Value);
            Assert.Equal(1, radius.NumericMinimum);
            AttributeFieldViewModel speed = vm.Fields.Single(f => f.Name == "orbitSpeed");
            Assert.Equal("70", speed.Value);
            Assert.Equal(1, speed.NumericMinimum);
            Assert.True(vm.Fields.Single(f => f.Name == "orbitClockwise").BoolValue);

            radius.Value = "45";
            speed.Value = "120";
            vm.Fields.Single(f => f.Name == "orbitClockwise").BoolValue = false;

            Assert.Equal("RW45", star.GetAttr("path"));
            Assert.Equal("120", star.GetAttr("moveSpeed"));
        }

        /// <summary>Clearing orbit radius behaves like timed-star duration: it stays visible while editing.</summary>
        [Fact]
        public void ClearingStarOrbitRadiusKeepsOrbitFieldsVisibleLikeTimedDuration()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;
            vm.Fields.Single(f => f.Name == "movementMode").Value = "orbit";
            AttributeFieldViewModel radius = vm.Fields.Single(f => f.Name == "orbitRadius");

            radius.Value = string.Empty;

            Assert.Equal(string.Empty, radius.Value);
            Assert.Equal("RC", star.GetAttr("path"));
            Assert.Equal("70", star.GetAttr("moveSpeed"));
            Assert.Equal("orbit", vm.Fields.Single(f => f.Name == "movementMode").Value);
            Assert.Contains(vm.Fields, f => f.Name == "orbitRadius");
            Assert.Contains(vm.Fields, f => f.Name == "orbitSpeed");

            radius.Value = "45";

            Assert.Equal("RC45", star.GetAttr("path"));
            Assert.Equal("45", radius.Value);
            Assert.Equal("orbit", vm.Fields.Single(f => f.Name == "movementMode").Value);
        }

        /// <summary>Clearing orbit speed behaves like timed-star duration and keeps orbit fields visible.</summary>
        [Fact]
        public void ClearingStarOrbitSpeedKeepsOrbitFieldsVisibleLikeTimedDuration()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;
            vm.Fields.Single(f => f.Name == "movementMode").Value = "orbit";
            AttributeFieldViewModel speed = vm.Fields.Single(f => f.Name == "orbitSpeed");

            speed.Value = string.Empty;

            Assert.Equal(string.Empty, speed.Value);
            Assert.Equal(string.Empty, star.GetAttr("moveSpeed"));
            Assert.Equal("orbit", vm.Fields.Single(f => f.Name == "movementMode").Value);
            Assert.Contains(vm.Fields, f => f.Name == "orbitRadius");
            Assert.Contains(vm.Fields, f => f.Name == "orbitSpeed");

            speed.Value = "130";

            Assert.Equal("130", star.GetAttr("moveSpeed"));
            Assert.Equal("130", speed.Value);
            Assert.Equal("orbit", vm.Fields.Single(f => f.Name == "movementMode").Value);
        }

        /// <summary>Clearing spin speed behaves like timed-star duration: it stays visible while editing.</summary>
        [Fact]
        public void ClearingStarSpinSpeedKeepsSpinFieldsVisibleLikeTimedDuration()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;
            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;
            AttributeFieldViewModel speed = vm.Fields.Single(f => f.Name == "spinSpeed");

            speed.Value = string.Empty;

            Assert.Equal(string.Empty, speed.Value);
            Assert.Equal(string.Empty, star.GetAttr("rotateSpeed"));
            Assert.True(vm.Fields.Single(f => f.Name == "spin").BoolValue);
            Assert.Contains(vm.Fields, f => f.Name == "spinSpeed");

            speed.Value = "130";

            Assert.Equal("130", star.GetAttr("rotateSpeed"));
            Assert.Equal("130", speed.Value);
            Assert.True(vm.Fields.Single(f => f.Name == "spin").BoolValue);
        }

        /// <summary>Spin and orbit can coexist, matching DX's separate rotateSpeed and moveSpeed handling.</summary>
        [Fact]
        public void CheckingStarSpinAndOrbitKeepsBothMoverAttributes()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;

            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;
            vm.Fields.Single(f => f.Name == "movementMode").Value = "orbit";

            Assert.Equal("70", star.GetAttr("rotateSpeed"));
            Assert.Equal("RC30", star.GetAttr("path"));
            Assert.Equal("70", star.GetAttr("moveSpeed"));
            Assert.Equal("70", vm.Fields.Single(f => f.Name == "spinSpeed").Value);
            Assert.Equal("30", vm.Fields.Single(f => f.Name == "orbitRadius").Value);
            Assert.Equal("70", vm.Fields.Single(f => f.Name == "orbitSpeed").Value);
        }

        /// <summary>Changing spin refreshes object-list bindings without clearing the selected object.</summary>
        [Fact]
        public void CheckingStarSpinRefreshesObjectListBindingsWithoutClearingSelection()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;
            int version = vm.ObjectListVersion;

            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;

            Assert.True(vm.ObjectListVersion > version);
            Assert.Same(star, vm.SelectedObject);
            Assert.Contains(star, vm.ObjectList);
        }

        /// <summary>Polyline movement is mutually exclusive with orbit but preserves independent spin.</summary>
        [Fact]
        public void SwitchingStarMovementModeBetweenOrbitAndPolylinePreservesSpin()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(Level);
            LevelObject star = vm.Document!.AllObjects[0];
            vm.SelectedObject = star;

            vm.Fields.Single(f => f.Name == "spin").BoolValue = true;
            vm.Fields.Single(f => f.Name == "movementMode").Value = "orbit";
            vm.Fields.Single(f => f.Name == "movementMode").Value = "polyline";

            Assert.Equal("70", star.GetAttr("rotateSpeed"));
            Assert.Equal("100,0", star.GetAttr("path"));
            Assert.Equal("70", star.GetAttr("moveSpeed"));
            Assert.Equal("polyline", vm.Fields.Single(f => f.Name == "movementMode").Value);
            Assert.Contains(vm.Fields, f => f.Name == "polylineSpeed");
            Assert.Contains(vm.Fields, f => f.Name == "polylineRetrace");
            Assert.DoesNotContain(vm.Fields, f => f.Name == "orbitRadius");

            vm.Fields.Single(f => f.Name == "movementMode").Value = "none";

            Assert.Equal("70", star.GetAttr("rotateSpeed"));
            // The game only spins an object that has a path (its mover carries rotateSpeed), so clearing movement
            // keeps the static "0,0" spin-carrier rather than removing the path and silently breaking spin.
            Assert.Equal("0,0", star.GetAttr("path"));
            Assert.Null(star.GetAttr("moveSpeed"));
            Assert.Equal("none", vm.Fields.Single(f => f.Name == "movementMode").Value);
        }
    }
}
