using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>
    /// Builds the grab properties: the "Attach to" binding control, the rope-geometry group with
    /// progressive Auto-catch / Movable-rail disclosure, and the hook-variant toggles. Structural
    /// changes trigger a rebuild so disclosure and gating re-evaluate.
    /// </summary>
    public static class GrabFieldBuilder
    {
        /// <summary>Appends the grab's fields, in panel order, to <paramref name="fields"/>.</summary>
        public static void Build(
            IList<AttributeFieldViewModel> fields,
            LevelObject grab,
            LevelDocument document,
            Action onChanged,
            Action onChanging,
            Action rebuild)
        {
            bool gun = Bool(grab, "gun");
            bool wheel = Bool(grab, "wheel");
            bool spider = Bool(grab, "spider");
            bool kickable = Bool(grab, "kickable");
            bool autoCatch = Int(grab, "radius") > 0;
            bool movable = Int(grab, "moveLength") > 0;
            bool twoParts = document.TwoParts;

            void Structural()
            {
                onChanged();
                rebuild();
            }

            // Attach to stays in place but greys out when the grab has no authored rope - a gun grab, or
            // an auto-catch grab (which binds candy at runtime) - rather than disappearing and shifting
            // every field below it. Mirrors LoadGrabs, which skips the binding block unless radius == -1.
            IReadOnlyList<GrabBindOption> options = GrabBinding.Options(document.Objects, twoParts);
            if (options.Count >= 2)
            {
                AttributeOptionViewModel[] vmOptions =
                    [.. options.Select(o => new AttributeOptionViewModel(o.Token, o.Label))];
                fields.Add(new AttributeFieldViewModel(
                    "attachTo",
                    vmOptions,
                    () => GrabBinding.CurrentToken(grab, document.Objects, document.TwoParts),
                    token => GrabBinding.Apply(grab, token ?? "primary"),
                    onChanged,
                    onChanging)
                { IsEnabled = !gun && !autoCatch });
            }

            bool geomEnabled = !gun;
            bool railEnabled = !gun && !wheel && !kickable;

            // The Auto-catch toggle comes first, then length XOR radius fill the same slot beneath
            // it. Because exactly one of the two is always shown, toggling swaps the row in place
            // without shifting the toggle or the fields below it.
            fields.Add(Synthetic(
                "autoCatch",
                () => autoCatch,
                on => grab.SetAttr("radius", on ? "100" : "-1"),
                Structural,
                onChanging,
                geomEnabled));
            fields.Add(autoCatch
                ? Attr(grab, "radius", AttrType.Whole, onChanged, onChanging, geomEnabled)
                : Attr(grab, "length", AttrType.Whole, onChanged, onChanging, geomEnabled));

            fields.Add(Synthetic(
                "movable",
                () => movable,
                on => grab.SetAttr("moveLength", on ? "100" : "-1"),
                Structural,
                onChanging,
                railEnabled));
            if (movable)
            {
                fields.Add(Attr(grab, "moveVertical", AttrType.Bool, onChanged, onChanging, railEnabled));
                fields.Add(Attr(grab, "moveLength", AttrType.Whole, onChanged, onChanging, railEnabled));
                fields.Add(Attr(grab, "moveOffset", AttrType.Whole, onChanged, onChanging, railEnabled));
            }

            fields.Add(BoolAttr(grab, "wheel", Structural, onChanging, !(gun || movable), ClearMoveRail));
            fields.Add(BoolAttr(grab, "gun", Structural, onChanging, !(wheel || spider || kickable || movable), ClearMoveRail));

            fields.Add(Attr(grab, "spider", AttrType.Bool, Structural, onChanging, !gun));
            fields.Add(BoolAttr(grab, "kickable", Structural, onChanging, !(gun || movable), ClearMoveRail));
            if (kickable)
            {
                fields.Add(Attr(grab, "kicked", AttrType.Bool, onChanged, onChanging, !gun));
            }

            // The game hides an invisible grab (and its rope) entirely; the editor keeps it visible but
            // pale so it stays selectable. Works for every grab type, so it is never gated.
            fields.Add(Attr(grab, "invisible", AttrType.Bool, onChanged, onChanging, true));
        }

        private static AttributeFieldViewModel Attr(
            LevelObject grab, string name, AttrType type, Action onChanged, Action onChanging, bool enabled)
        {
            return new AttributeFieldViewModel(grab, name, type, null, onChanged, onChanging) { IsEnabled = enabled };
        }

        private static AttributeFieldViewModel Synthetic(
            string name, Func<bool> get, Action<bool> set, Action onChanged, Action onChanging, bool enabled)
        {
            return new AttributeFieldViewModel(
                name,
                AttrType.Bool,
                () => get() ? "true" : "false",
                v => set(v == "true"),
                onChanged,
                onChanging)
            { IsEnabled = enabled };
        }

        private static AttributeFieldViewModel BoolAttr(
            LevelObject grab, string name, Action onChanged, Action onChanging, bool enabled, Action<LevelObject> whenEnabled)
        {
            return new AttributeFieldViewModel(
                name,
                AttrType.Bool,
                () => Bool(grab, name) ? "true" : "false",
                v =>
                {
                    bool on = v == "true";
                    grab.SetAttr(name, on ? "true" : "false");
                    if (on)
                    {
                        whenEnabled(grab);
                    }
                },
                onChanged,
                onChanging)
            { IsEnabled = enabled };
        }

        private static void ClearMoveRail(LevelObject grab)
        {
            grab.SetAttr("moveLength", "-1");
        }

        private static bool Bool(LevelObject grab, string name)
        {
            return bool.TryParse(grab.GetAttr(name), out bool b) && b;
        }

        private static int Int(LevelObject grab, string name)
        {
            return int.TryParse(grab.GetAttr(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
                ? v
                : 0;
        }
    }
}
