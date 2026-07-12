using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

using CtrDxEditor.Rendering;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// Drives palette-to-canvas object placement as an internal pointer-capture drag (no OS drag-drop, so
    /// no OS drag image): a click drops at the level center, a drag drops where it lands on the canvas, and
    /// a drag released off-canvas cancels. Pointer coordinates are measured against the supplied root visual.
    /// </summary>
    /// <param name="root">Visual the pointer coordinates and drag threshold are measured against.</param>
    /// <param name="canvas">Canvas the armed palette element is placed onto.</param>
    internal sealed class PaletteDragController(Visual root, LevelCanvas canvas)
    {
        private const double DragThreshold = 4;

        private string? _pendingElement;
        private Point _pressPos;
        private bool _dragging;
        private PaletteItemViewModel? _draggingItem;

        /// <summary>Arms a placement on left-press and captures the pointer; the gesture resolves on release.</summary>
        public void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            Cancel();
            if (!e.GetCurrentPoint(root).Properties.IsLeftButtonPressed)
            {
                return;
            }

            Button? button = (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true);
            if (button is { Tag: string element, IsEnabled: true })
            {
                _pendingElement = element;
                if (button.DataContext is PaletteItemViewModel pressed)
                {
                    _draggingItem = pressed;
                    pressed.IsDragging = true;
                }
                _pressPos = e.GetPosition(root);
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

        /// <summary>Resolves the gesture: a click drops at center, a drag drops where it lands, off-canvas cancels.</summary>
        public void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_pendingElement is string element)
            {
                canvas.HideGhost();
                if (!_dragging)
                {
                    if (canvas.AddAtCenter(element)) // a click: drop at the level center
                    {
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

        /// <summary>Clears drag state, hides the ghost, and unsets the dragged item's flag.</summary>
        public void Cancel()
        {
            canvas.HideGhost();
            _pendingElement = null;
            _dragging = false;
            if (_draggingItem is { } draggingItem)
            {
                draggingItem.IsDragging = false;
            }
            _draggingItem = null;
        }
    }
}
