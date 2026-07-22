using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// Drives palette-to-canvas object placement. With a mouse or pen this is an internal pointer-capture
    /// drag (no OS drag-drop, so no OS drag image): a click drops at the level center, a drag drops where it
    /// lands on the canvas, and a drag released off-canvas cancels. Touch has no drag path at all — a tap
    /// drops at the level center and a swipe scrolls the list. Pointer coordinates are measured against the
    /// supplied root visual.
    /// </summary>
    /// <remarks>
    /// Dragging is meaningless on touch: in the compact shell the palette sheet sits over the canvas, so
    /// there is nowhere to drag to, and a drop point under the sheet is one the finger never saw. Capturing
    /// the pointer would also take the gesture away from the sheet's <c>ScrollViewer</c> and stop the list
    /// scrolling, so touch presses deliberately leave the pointer uncaptured.
    /// </remarks>
    /// <param name="root">Visual the pointer coordinates and drag threshold are measured against.</param>
    /// <param name="canvas">Canvas the armed palette element is placed onto.</param>
    internal sealed class PaletteDragController(Visual root, LevelCanvas canvas)
    {
        private const double DragThreshold = 4;

        private string? _pendingElement;
        private Point _pressPos;
        private bool _dragging;
        private bool _touch;
        private PaletteItemViewModel? _draggingItem;

        /// <summary>How long a placed item shows its confirmation before reverting to its icon.</summary>
        private static readonly TimeSpan PlacedFeedbackDuration = TimeSpan.FromMilliseconds(900);

        /// <summary>The item under the current press, recorded for touch as well as mouse.</summary>
        private PaletteItemViewModel? _pendingItem;

        /// <summary>The item currently showing a placement confirmation, or null when none is.</summary>
        private PaletteItemViewModel? _placedItem;

        /// <summary>Clears <see cref="_placedItem"/> once its confirmation has been shown long enough.</summary>
        private DispatcherTimer? _placedTimer;

        /// <summary>Arms a placement on press; the gesture resolves on release.</summary>
        /// <remarks>
        /// A mouse or pen press captures the pointer so the drag survives leaving the palette. A touch press
        /// does not, leaving the sheet's scroll gesture intact.
        /// </remarks>
        public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            Cancel();
            bool touch = e.Pointer.Type == PointerType.Touch;
            if (!touch && !e.GetCurrentPoint(root).Properties.IsLeftButtonPressed)
            {
                return;
            }

            Button? button = (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true);
            if (button is { Tag: string element, IsEnabled: true })
            {
                _pendingElement = element;
                // Recorded before the touch return: touch is the case the confirmation exists for.
                _pendingItem = button.DataContext as PaletteItemViewModel;
                _touch = touch;
                _pressPos = e.GetPosition(root);

                if (touch)
                {
                    return;
                }

                if (_pendingItem is { } pressed)
                {
                    _draggingItem = pressed;
                    pressed.IsDragging = true;
                }
                e.Pointer.Capture(sender as IInputElement);
            }
        }

        /// <summary>Promotes a press to a drag past the threshold and previews the ghost while over the canvas.</summary>
        public void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_pendingElement is not string element)
            {
                return;
            }

            if (!_dragging)
            {
                Point now = e.GetPosition(root);
                if (Math.Abs(now.X - _pressPos.X) < DragThreshold
                    && Math.Abs(now.Y - _pressPos.Y) < DragThreshold)
                {
                    return;
                }

                if (_touch)
                {
                    // The finger is scrolling the list, not placing. Drop the pending placement so the
                    // release cannot mistake the end of a swipe for a tap.
                    Cancel();
                    return;
                }

                _dragging = true;
            }

            // Show the drag preview only while over the canvas; hide it when the cursor leaves.
            Point onCanvas = e.GetPosition(canvas);
            if (new Rect(canvas.Bounds.Size).Contains(onCanvas))
            {
                canvas.ShowGhost(element, onCanvas);
            }
            else
            {
                canvas.HideGhost();
            }
        }

        /// <summary>
        /// Resolves the gesture: a click or tap drops at center, a drag drops where it lands, off-canvas
        /// cancels. Touch never reaches the drag branch, so a tap that survived the move handler places.
        /// </summary>
        public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_pendingElement is string element)
            {
                canvas.HideGhost();
                if (!_dragging)
                {
                    if (canvas.AddAtCenter(element)) // a click or tap: drop at the level center
                    {
                        ConfirmPlacement();
                        _ = canvas.Focus();
                    }
                }
                else
                {
                    Point onCanvas = e.GetPosition(canvas);
                    if (new Rect(canvas.Bounds.Size).Contains(onCanvas))
                    {
                        if (canvas.DropElement(element, onCanvas)) // dragged onto the canvas
                        {
                            ConfirmPlacement();
                            _ = canvas.Focus();
                        }
                    }
                    // dragged but released off-canvas: cancel
                }
            }

            e.Pointer.Capture(null);
            Cancel();
        }

        /// <summary>Cancels an in-flight drag when the pointer capture is lost.</summary>
        public void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            Cancel();
        }

        /// <summary>Flags the pressed item as just placed and starts the timer that clears it.</summary>
        /// <remarks>
        /// One item is confirmed at a time: a second placement clears the first immediately rather than
        /// leaving two rows lit, so the cue always points at the row that was last tapped. The timer is
        /// created once and restarted, not recreated, so a burst of taps cannot leave stray timers behind.
        /// </remarks>
        private void ConfirmPlacement()
        {
            if (_pendingItem is not { } placed)
            {
                return;
            }

            if (_placedItem is { } previous && !ReferenceEquals(previous, placed))
            {
                previous.JustPlaced = false;
            }

            _placedItem = placed;
            placed.JustPlaced = true;

            if (_placedTimer is null)
            {
                _placedTimer = new DispatcherTimer { Interval = PlacedFeedbackDuration };
                _placedTimer.Tick += (_, _) => ClearPlacementFeedback();
            }

            _placedTimer.Stop();
            _placedTimer.Start();
        }

        /// <summary>Reverts the confirmed row to its icon and stops the timer.</summary>
        private void ClearPlacementFeedback()
        {
            _placedTimer?.Stop();
            if (_placedItem is { } placed)
            {
                placed.JustPlaced = false;
            }
            _placedItem = null;
        }

        /// <summary>Clears drag state, hides the ghost, and unsets the dragged item's flag.</summary>
        /// <remarks>
        /// Deliberately leaves <see cref="_placedItem"/> alone: <c>Cancel</c> runs at the end of every
        /// release, immediately after a successful placement has confirmed, so clearing the confirmation
        /// here would blank it in the frame it was set.
        /// </remarks>
        public void Cancel()
        {
            canvas.HideGhost();
            _pendingElement = null;
            _pendingItem = null;
            _dragging = false;
            _touch = false;
            if (_draggingItem is { } draggingItem)
            {
                draggingItem.IsDragging = false;
            }
            _draggingItem = null;
        }
    }
}
