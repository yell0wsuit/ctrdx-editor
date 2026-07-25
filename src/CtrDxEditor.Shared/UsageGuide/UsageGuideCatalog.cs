using System.Collections.Generic;

namespace CtrDxEditor.UsageGuide
{
    /// <summary>The localized articles shipped with the editor.</summary>
    public static class UsageGuideCatalog
    {
        /// <summary>The stable identifier opened by the guide's Home button.</summary>
        public const string HomeArticleId = "welcome";

        /// <summary>All built-in articles in table-of-contents order.</summary>
        public static IReadOnlyList<GuideArticle> Articles { get; } =
        [
            A("welcome", "StartHere",
                [P("welcome", "Intro"), H("welcome", "Route"), Steps("welcome", "Route"), Shot("welcome", "Layout", "guide-welcome-editor-layout.png"), Tip("welcome", "Search")],
                ["first-level", "know-the-editor", "object-reference"]),
            A("first-level", "StartHere",
                [Steps("first-level", "Build"), Note("first-level", "Validation"), Shot("first-level", "Example", "guide-first-level.png")],
                ["add-place-objects", "level-settings", "save-export-playtest"]),
            A("know-the-editor", "StartHere",
                [H("know-the-editor", "Expanded"), P("know-the-editor", "Expanded"), H("know-the-editor", "Compact"), P("know-the-editor", "Compact"), Shot("know-the-editor", "Layouts", "guide-editor-layouts.png")],
                ["menus-commands", "layers", "edit-properties"]),

            A("add-place-objects", "EditLevel",
                [Steps("add-place-objects", "Place"), Note("add-place-objects", "Availability"), Shot("add-place-objects", "Drag", "guide-place-object.png")],
                ["select-transform", "object-reference", "snapping"]),
            A("select-transform", "EditLevel",
                [P("select-transform", "Selection"), P("select-transform", "Handles"), Tip("select-transform", "Duplicate"), Note("select-transform", "RotationSnap"), Shot("select-transform", "Handles", "guide-transform-handles.png")],
                ["clipboard-lock-hide", "modifier-keys", "snapping"]),
            A("clipboard-lock-hide", "EditLevel",
                [P("clipboard-lock-hide", "Clipboard"), P("clipboard-lock-hide", "Locking"), Warning("clipboard-lock-hide", "DeleteLayer")],
                ["layers", "keyboard-shortcuts", "select-transform"]),
            A("layers", "EditLevel",
                [P("layers", "Intro"), Steps("layers", "Organize"), Note("layers", "InheritedState"), Shot("layers", "Panel", "guide-layers-panel.png")],
                ["clipboard-lock-hide", "know-the-editor"]),
            A("edit-properties", "EditLevel",
                [P("edit-properties", "Fields"), P("edit-properties", "Help"), Tip("edit-properties", "Precision")],
                ["select-transform", "level-settings", "object-reference"]),
            A("level-settings", "EditLevel",
                [P("level-settings", "Mechanics"), P("level-settings", "Customization"), Note("level-settings", "Palette"), Shot("level-settings", "Dialog", "guide-level-settings.png")],
                ["object-reference", "validation-review", "first-level"]),

            A("rope-hooks", "ComplexObjects",
                [P("rope-hooks", "Binding"), P("rope-hooks", "Handles"), Warning("rope-hooks", "Validation"), Shot("rope-hooks", "Handles", "guide-rope-hook-handles.png")],
                ["first-level", "movement-paths", "validation-review"]),
            A("magic-hats", "ComplexObjects",
                [P("magic-hats", "Pairing"), P("magic-hats", "Mouth"), Shot("magic-hats", "Pair", "guide-magic-hats.png")],
                ["diagnostic-overlays", "select-transform"]),
            A("movement-paths", "ComplexObjects",
                [P("movement-paths", "Modes"), Steps("movement-paths", "Edit"), Tip("movement-paths", "Angles"), Note("movement-paths", "Limit"), Shot("movement-paths", "Polyline", "guide-movement-path.png")],
                ["modifier-keys", "diagnostic-overlays", "edit-properties"]),
            A("conveyors", "ComplexObjects",
                [P("conveyors", "Belt"), P("conveyors", "Ants"), Shot("conveyors", "Handles", "guide-conveyors.png")],
                ["movement-paths", "select-transform", "edit-properties"]),
            A("tubes", "ComplexObjects",
                [P("tubes", "Bamboo"), P("tubes", "Steam"), Shot("tubes", "Overlay", "guide-tubes.png")],
                ["diagnostic-overlays", "select-transform"]),
            A("rockets", "ComplexObjects",
                [P("rockets", "Aim"), Tip("rockets", "Playtest"), Shot("rockets", "Selected", "guide-rocket.png")],
                ["edit-properties", "save-export-playtest"]),
            A("mechanical-hands", "ComplexObjects",
                [P("mechanical-hands", "Segments"), P("mechanical-hands", "Split"), Note("mechanical-hands", "Dial"), Shot("mechanical-hands", "Controls", "guide-mechanical-hand.png")],
                ["modifier-keys", "select-transform"]),
            A("tutorial-objects", "ComplexObjects",
                [P("tutorial-objects", "Text"), P("tutorial-objects", "Editing"), P("tutorial-objects", "Language"), Shot("tutorial-objects", "Width", "guide-tutorial-objects.png")],
                ["edit-properties", "level-settings"]),

            A("object-reference", "Reference",
                [H("object-reference", "Goals"), P("object-reference", "Goals"), H("object-reference", "Forces"), P("object-reference", "Forces"), H("object-reference", "Mechanics"), P("object-reference", "Mechanics"), Note("object-reference", "Details")],
                ["add-place-objects", "edit-properties", "rope-hooks"]),
            A("menus-commands", "Reference",
                [H("menus-commands", "File"), P("menus-commands", "File"), H("menus-commands", "Edit"), P("menus-commands", "Edit"), H("menus-commands", "View"), P("menus-commands", "View"), H("menus-commands", "Help"), P("menus-commands", "Help")],
                ["keyboard-shortcuts", "know-the-editor"]),
            A("keyboard-shortcuts", "Reference",
                [Note("keyboard-shortcuts", "Platform"), ShortcutTable("keyboard-shortcuts",
                    "New", "Open", "Save", "SaveAs", "Screenshot", "Close", "Undo", "Redo",
                    "Clipboard", "SelectAll", "Delete", "Zoom", "Animation", "TutorialText")],
                ["modifier-keys", "menus-commands", "pointer-gestures"]),
            A("pointer-gestures", "Reference",
                [P("pointer-gestures", "Mouse"), P("pointer-gestures", "Touch"), P("pointer-gestures", "Context")],
                ["modifier-keys", "select-transform", "movement-paths"]),
            A("modifier-keys", "Reference",
                [ShortcutTable("modifier-keys", "Select", "Duplicate", "PathAngle", "HorizontalScroll", "Rotation", "SplitHand", "LineBreak"), Note("modifier-keys", "Option")],
                ["keyboard-shortcuts", "mechanical-hands", "movement-paths", "snapping"]),
            A("snapping", "Reference",
                [P("snapping", "Toggles"), P("snapping", "Temporary"), Tip("snapping", "Properties")],
                ["modifier-keys", "edit-properties", "select-transform"]),

            A("animation-preview", "PreviewFinish",
                [P("animation-preview", "Usage"), Note("animation-preview", "Scope")],
                ["diagnostic-overlays", "save-export-playtest", "keyboard-shortcuts"]),
            A("diagnostic-overlays", "PreviewFinish",
                [P("diagnostic-overlays", "Usage"), Tip("diagnostic-overlays", "EditorOnly"), Shot("diagnostic-overlays", "Enabled", "guide-diagnostic-overlays.png")],
                ["magic-hats", "movement-paths", "tubes"]),
            A("validation-review", "PreviewFinish",
                [P("validation-review", "Validation"), P("validation-review", "Review"), Note("validation-review", "Warnings"), Shot("validation-review", "Dialogs", "guide-validation-review.png")],
                ["save-export-playtest", "troubleshooting"]),
            A("save-export-playtest", "PreviewFinish",
                [P("save-export-playtest", "Save"), P("save-export-playtest", "Playtest"), Warning("save-export-playtest", "Browser")],
                ["validation-review", "keyboard-shortcuts", "troubleshooting"]),
            A("troubleshooting", "PreviewFinish",
                [H("troubleshooting", "Palette"), P("troubleshooting", "Palette"), H("troubleshooting", "Selection"), P("troubleshooting", "Selection"), H("troubleshooting", "Playtest"), P("troubleshooting", "Playtest"), H("troubleshooting", "Assets"), P("troubleshooting", "Assets")],
                ["level-settings", "layers", "save-export-playtest"]),
        ];

        /// <summary>Creates a localized article definition from stable naming conventions.</summary>
        /// <param name="id">Stable article identifier and localization-key segment.</param>
        /// <param name="section">Localization-key suffix for the article's section.</param>
        /// <param name="blocks">Structured content in reading order.</param>
        /// <param name="related">Stable identifiers for related-topic links.</param>
        /// <returns>A catalog article whose visible metadata resolves through <c>Localizer</c>.</returns>
        private static GuideArticle A(
            string id,
            string section,
            IReadOnlyList<GuideBlock> blocks,
            IReadOnlyList<string> related)
        {
            string root = $"Guide.Article.{id}";
            return new GuideArticle(
                id,
                $"Guide.Section.{section}",
                $"{root}.Title",
                $"{root}.Summary",
                $"{root}.SearchTerms",
                blocks,
                related);
        }

        /// <summary>Creates a localized paragraph block.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Block-specific localization-key suffix.</param>
        /// <returns>A paragraph backed by the convention-based localization key.</returns>
        private static GuideParagraph P(string id, string name)
        {
            return new GuideParagraph(Key(id, name));
        }

        /// <summary>Creates a localized in-article heading.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Heading-specific localization-key suffix.</param>
        /// <returns>A heading backed by the convention-based localization key.</returns>
        private static GuideHeading H(string id, string name)
        {
            return new GuideHeading(Key(id, $"{name}.Heading"));
        }

        /// <summary>Creates a localized ordered procedure.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Procedure-specific localization-key suffix.</param>
        /// <returns>A steps block whose localized value is split on newlines.</returns>
        private static GuideSteps Steps(string id, string name)
        {
            return new GuideSteps(Key(id, $"{name}.Steps"));
        }

        /// <summary>Creates a localized optional-advice callout.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Callout-specific localization-key suffix.</param>
        /// <returns>A tip callout backed by the convention-based localization key.</returns>
        private static GuideCallout Tip(string id, string name)
        {
            return new GuideCallout(GuideCalloutKind.Tip, Key(id, $"{name}.Tip"));
        }

        /// <summary>Creates a localized contextual-note callout.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Callout-specific localization-key suffix.</param>
        /// <returns>A note callout backed by the convention-based localization key.</returns>
        private static GuideCallout Note(string id, string name)
        {
            return new GuideCallout(GuideCalloutKind.Note, Key(id, $"{name}.Note"));
        }

        /// <summary>Creates a localized warning callout.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Callout-specific localization-key suffix.</param>
        /// <returns>A warning callout backed by the convention-based localization key.</returns>
        private static GuideCallout Warning(string id, string name)
        {
            return new GuideCallout(GuideCalloutKind.Warning, Key(id, $"{name}.Warning"));
        }

        /// <summary>Creates a replaceable, localized screenshot slot.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Caption-specific localization-key suffix.</param>
        /// <param name="suggestedFileName">Filename shown while the screenshot asset is absent.</param>
        /// <returns>A screenshot block with an empty source and visible placeholder.</returns>
        private static GuideScreenshot Shot(string id, string name, string suggestedFileName)
        {
            return new GuideScreenshot(Key(id, $"{name}.Caption"), suggestedFileName);
        }

        /// <summary>Creates a localized shortcut table from convention-based row names.</summary>
        /// <param name="id">Article identifier used in every row's localization-key prefix.</param>
        /// <param name="names">Row suffixes whose <c>Action</c> and <c>Keys</c> values form the table.</param>
        /// <returns>A shortcut table preserving the supplied row order.</returns>
        private static GuideShortcutTable ShortcutTable(string id, params string[] names)
        {
            GuideShortcut[] rows = new GuideShortcut[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string root = Key(id, $"Shortcut.{names[i]}");
                rows[i] = new GuideShortcut($"{root}.Action", $"{root}.Keys");
            }
            return new GuideShortcutTable(rows);
        }

        /// <summary>Builds a content localization key for one article.</summary>
        /// <param name="id">Stable article identifier.</param>
        /// <param name="name">Content-specific key suffix.</param>
        /// <returns>A key under <c>Guide.Article.&lt;id&gt;</c>.</returns>
        private static string Key(string id, string name)
        {
            return $"Guide.Article.{id}.{name}";
        }
    }
}
