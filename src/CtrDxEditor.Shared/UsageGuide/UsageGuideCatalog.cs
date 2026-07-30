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
            Article("welcome", "StartHere",
                [Para("welcome", "Intro"), Heading("welcome", "Route"), List("welcome", "Route"), EmbeddedIllustration("welcome", "Layout", "guide-welcome-editor-layout.png"), Tip("welcome", "Search")],
                ["first-level", "know-the-editor", "object-reference"]),
            Article("first-level", "StartHere",
                [List("first-level", "Build"), Note("first-level", "Validation"), EmbeddedIllustration("first-level", "Example", "guide-first-level.png")],
                ["add-place-objects", "level-settings", "save-export-playtest"]),
            Article("know-the-editor", "StartHere",
                [Heading("know-the-editor", "Expanded"), Para("know-the-editor", "Expanded"), Heading("know-the-editor", "Compact"), Para("know-the-editor", "Compact"), EmbeddedIllustration("know-the-editor", "Layouts", "guide-editor-layouts.png")],
                ["menus-commands", "layers", "edit-properties"]),

            Article("add-place-objects", "EditLevel",
                [List("add-place-objects", "Place"), Note("add-place-objects", "Availability"), EmbeddedIllustration("add-place-objects", "Drag", "guide-place-object.png")],
                ["select-transform", "object-reference", "snapping"]),
            Article("select-transform", "EditLevel",
                [Para("select-transform", "Selection"), Para("select-transform", "Handles"), Tip("select-transform", "Duplicate"), Note("select-transform", "RotationSnap"), EmbeddedIllustration("select-transform", "Handles", "guide-transform-handles.png")],
                ["clipboard-lock-hide", "modifier-keys", "snapping"]),
            Article("clipboard-lock-hide", "EditLevel",
                [Para("clipboard-lock-hide", "Clipboard"), Para("clipboard-lock-hide", "Locking"), Warning("clipboard-lock-hide", "DeleteLayer")],
                ["layers", "keyboard-shortcuts", "select-transform"]),
            Article("layers", "EditLevel",
                [Para("layers", "Intro"), List("layers", "Organize"), Note("layers", "InheritedState"), EmbeddedIllustration("layers", "Panel", "guide-layers-panel.png")],
                ["clipboard-lock-hide", "know-the-editor"]),
            Article("edit-properties", "EditLevel",
                [Para("edit-properties", "Fields"), Para("edit-properties", "Help"), Tip("edit-properties", "Precision")],
                ["select-transform", "level-settings", "object-reference"]),
            Article("level-settings", "EditLevel",
                [Para("level-settings", "Mechanics"), Para("level-settings", "Water"), Para("level-settings", "Physics"), Para("level-settings", "Customization"), Note("level-settings", "Palette"), EmbeddedIllustration("level-settings", "Dialog", "guide-level-settings.png")],
                ["objectives", "light-bulbs", "validation-review", "first-level"]),

            Article("object-reference", "Reference",
                [Para("object-reference", "Intro"), Heading("object-reference", "Goals"), Para("object-reference", "Goals"), Heading("object-reference", "Forces"), Para("object-reference", "Forces"), Heading("object-reference", "Hazards"), Para("object-reference", "Hazards"), Heading("object-reference", "Night"), Para("object-reference", "Night"), Heading("object-reference", "Transport"), Para("object-reference", "Transport"), Heading("object-reference", "Experiments"), Para("object-reference", "Experiments"), Note("object-reference", "Details")],
                ["add-place-objects", "edit-properties", "objectives", "motion"]),
            Article("menus-commands", "Reference",
                [Heading("menus-commands", "File"), Para("menus-commands", "File"), Heading("menus-commands", "Edit"), Para("menus-commands", "Edit"), Heading("menus-commands", "View"), Para("menus-commands", "View"), Heading("menus-commands", "Help"), Para("menus-commands", "Help")],
                ["keyboard-shortcuts", "know-the-editor"]),
            Article("keyboard-shortcuts", "Reference",
                [Note("keyboard-shortcuts", "Platform"), ShortcutTable("keyboard-shortcuts",
                    "New", "Open", "Save", "SaveAs", "Screenshot", "Close", "Undo", "Redo",
                    "Clipboard", "SelectAll", "Delete", "Zoom", "Animation", "TutorialText",
                    "RemovePoint")],
                ["modifier-keys", "menus-commands", "pointer-gestures"]),
            Article("pointer-gestures", "Reference",
                [Para("pointer-gestures", "Mouse"), Para("pointer-gestures", "Touch"), Para("pointer-gestures", "Context"), Note("pointer-gestures", "Locked")],
                ["modifier-keys", "select-transform", "motion"]),
            Article("modifier-keys", "Reference",
                [ShortcutTable("modifier-keys", "Select", "Duplicate", "PathAngle", "HorizontalScroll", "Zoom", "Rotation", "SplitHand", "RopeTension", "LineBreak"), Note("modifier-keys", "Option")],
                ["keyboard-shortcuts", "mechanical-hands", "motion", "snapping"]),
            Article("snapping", "Reference",
                [Para("snapping", "Toggles"), Para("snapping", "Temporary"), Tip("snapping", "Properties")],
                ["modifier-keys", "edit-properties", "select-transform"]),

            Article("objectives", "GameObjects",
                [Para("objectives", "InGame"), Para("objectives", "Editing"), Para("objectives", "Numbering"), Warning("objectives", "Validation")],
                ["stars", "rope-hooks", "lanterns", "validation-review"]),
            Article("stars", "GameObjects",
                [Para("stars", "InGame"), Para("stars", "Editing"), Para("stars", "Properties"), Tip("stars", "Timeout")],
                ["objectives", "motion", "light-bulbs"]),
            Article("rope-hooks", "GameObjects",
                [Para("rope-hooks", "InGame"), Para("rope-hooks", "Editing"), Para("rope-hooks", "Binding"), Para("rope-hooks", "Rope"), Para("rope-hooks", "Variants"), Note("rope-hooks", "Bee"), Warning("rope-hooks", "Validation"), EmbeddedIllustration("rope-hooks", "Handles", "guide-rope-hook-handles.png")],
                ["objectives", "light-bulbs", "motion", "validation-review"]),
            Article("bubbles", "GameObjects",
                [Para("bubbles", "InGame"), Para("bubbles", "Editing"), Tip("bubbles", "Hitbox")],
                ["objectives", "ghosts", "diagnostic-overlays"]),
            Article("air-cushions", "GameObjects",
                [Para("air-cushions", "InGame"), Para("air-cushions", "Editing"), Tip("air-cushions", "ForceField")],
                ["steam-pipes", "diagnostic-overlays", "snapping"]),
            Article("gravity-buttons", "GameObjects",
                [Para("gravity-buttons", "InGame"), Para("gravity-buttons", "Editing"), Note("gravity-buttons", "Single")],
                ["objectives", "spikes", "object-reference"]),
            Article("bouncers", "GameObjects",
                [Para("bouncers", "InGame"), Para("bouncers", "Editing"), Para("bouncers", "Properties"), Note("bouncers", "Hitbox")],
                ["ghosts", "motion", "diagnostic-overlays"]),
            Article("spikes", "GameObjects",
                [Para("spikes", "InGame"), Para("spikes", "Editing"), Para("spikes", "Properties"), Warning("spikes", "Overlap")],
                ["electric-sparks", "motion", "validation-review"]),
            Article("electric-sparks", "GameObjects",
                [Para("electric-sparks", "InGame"), Para("electric-sparks", "Editing"), Para("electric-sparks", "Properties"), Tip("electric-sparks", "Timing")],
                ["spikes", "motion", "animation-preview"]),
            Article("ghosts", "GameObjects",
                [Para("ghosts", "InGame"), Para("ghosts", "Editing"), Para("ghosts", "Properties"), Warning("ghosts", "Idle")],
                ["bubbles", "rope-hooks", "bouncers", "validation-review"]),
            Article("magic-hats", "GameObjects",
                [Para("magic-hats", "InGame"), Para("magic-hats", "Editing"), Para("magic-hats", "Grouping"), Note("magic-hats", "Mouth"), Tip("magic-hats", "Xmas"), EmbeddedIllustration("magic-hats", "Pair", "guide-magic-hats.png")],
                ["bamboo-tubes", "diagnostic-overlays", "motion"]),
            Article("light-bulbs", "GameObjects",
                [Para("light-bulbs", "InGame"), Para("light-bulbs", "InGame2"), Para("light-bulbs", "Editing"), Para("light-bulbs", "Properties"), Warning("light-bulbs", "NightLevel")],
                ["lanterns", "rope-hooks", "level-settings", "stars"]),
            Article("lanterns", "GameObjects",
                [Para("lanterns", "InGame"), Para("lanterns", "Editing"), Para("lanterns", "Properties"), Note("lanterns", "NoCandy")],
                ["light-bulbs", "objectives", "validation-review"]),
            Article("vinyl", "GameObjects",
                [Para("vinyl", "InGame"), Para("vinyl", "Editing"), Para("vinyl", "Properties"), Tip("vinyl", "Contained")],
                ["motion", "select-transform", "object-reference"]),
            Article("mice", "GameObjects",
                [Para("mice", "InGame"), Para("mice", "Editing"), Para("mice", "Properties"), Note("mice", "Index")],
                ["snails", "objectives", "object-reference"]),
            Article("snails", "GameObjects",
                [Para("snails", "InGame"), Para("snails", "Editing"), Tip("snails", "Weight")],
                ["mice", "objectives", "mechanical-hands"]),
            Article("conveyors", "GameObjects",
                [Para("conveyors", "InGame"), Para("conveyors", "Editing"), Para("conveyors", "Properties"), Note("conveyors", "Manual"), EmbeddedIllustration("conveyors", "Handles", "guide-conveyors.png")],
                ["ant-conveyors", "select-transform", "snapping"]),
            Article("ant-conveyors", "GameObjects",
                [Para("ant-conveyors", "InGame"), Para("ant-conveyors", "Editing"), Para("ant-conveyors", "Properties"), Tip("ant-conveyors", "Shape")],
                ["conveyors", "motion", "object-reference"]),
            Article("bamboo-tubes", "GameObjects",
                [Para("bamboo-tubes", "InGame"), Para("bamboo-tubes", "Editing"), Note("bamboo-tubes", "Openings"), EmbeddedIllustration("bamboo-tubes", "Overlay", "guide-tubes.png")],
                ["steam-pipes", "magic-hats", "snapping"]),
            Article("steam-pipes", "GameObjects",
                [Para("steam-pipes", "InGame"), Para("steam-pipes", "Editing"), Tip("steam-pipes", "ForceField")],
                ["air-cushions", "bamboo-tubes", "diagnostic-overlays"]),
            Article("rockets", "GameObjects",
                [Para("rockets", "InGame"), Para("rockets", "Editing"), Para("rockets", "Properties"), Para("rockets", "Movement"), Tip("rockets", "Playtest"), EmbeddedIllustration("rockets", "Selected", "guide-rocket.png")],
                ["motion", "edit-properties", "objectives", "save-export-playtest"]),
            Article("mechanical-hands", "GameObjects",
                [Para("mechanical-hands", "InGame"), Para("mechanical-hands", "Segments"), Para("mechanical-hands", "Split"), Para("mechanical-hands", "Properties"), Note("mechanical-hands", "Dial"), EmbeddedIllustration("mechanical-hands", "Controls", "guide-mechanical-hand.png")],
                ["modifier-keys", "select-transform", "snails"]),
            Article("motion", "GameObjects",
                [Heading("motion", "Paths"), Para("motion", "Modes"), List("motion", "Edit"), Tip("motion", "Angles"), Note("motion", "Limit"), Heading("motion", "Spin"), Para("motion", "Spin"), EmbeddedIllustration("motion", "Polyline", "guide-movement-path.png")],
                ["modifier-keys", "diagnostic-overlays", "rope-hooks", "edit-properties"]),
            Article("tutorial-objects", "GameObjects",
                [Para("tutorial-objects", "InGame"), Para("tutorial-objects", "Icons"), Para("tutorial-objects", "Text"), Para("tutorial-objects", "Editing"), Warning("tutorial-objects", "Language"), EmbeddedIllustration("tutorial-objects", "Width", "guide-tutorial-objects.png")],
                ["edit-properties", "level-settings", "keyboard-shortcuts"]),

            Article("animation-preview", "PreviewFinish",
                [Para("animation-preview", "Usage"), Note("animation-preview", "Scope")],
                ["diagnostic-overlays", "save-export-playtest", "keyboard-shortcuts"]),
            Article("diagnostic-overlays", "PreviewFinish",
                [Para("diagnostic-overlays", "Hitboxes"), Para("diagnostic-overlays", "ForceFields"), Para("diagnostic-overlays", "Paths"), Tip("diagnostic-overlays", "EditorOnly"), EmbeddedIllustration("diagnostic-overlays", "Enabled", "guide-diagnostic-overlays.png")],
                ["magic-hats", "motion", "air-cushions", "level-settings"]),
            Article("validation-review", "PreviewFinish",
                [Para("validation-review", "Validation"), List("validation-review", "Checks"), Para("validation-review", "Review"), Note("validation-review", "Warnings"), EmbeddedIllustration("validation-review", "Dialogs", "guide-validation-review.png")],
                ["save-export-playtest", "troubleshooting", "objectives"]),
            Article("save-export-playtest", "PreviewFinish",
                [Para("save-export-playtest", "Save"), Para("save-export-playtest", "Playtest"), Warning("save-export-playtest", "Browser")],
                ["validation-review", "keyboard-shortcuts", "troubleshooting"]),
            Article("troubleshooting", "PreviewFinish",
                [Heading("troubleshooting", "Palette"), Para("troubleshooting", "Palette"), Heading("troubleshooting", "Selection"), Para("troubleshooting", "Selection"), Heading("troubleshooting", "Rope"), Para("troubleshooting", "Rope"), Heading("troubleshooting", "Playtest"), Para("troubleshooting", "Playtest"), Heading("troubleshooting", "Assets"), Para("troubleshooting", "Assets")],
                ["level-settings", "layers", "rope-hooks", "save-export-playtest"]),
        ];

        /// <summary>Creates a localized article definition from stable naming conventions.</summary>
        /// <param name="id">Stable article identifier and localization-key segment.</param>
        /// <param name="section">Localization-key suffix for the article's section.</param>
        /// <param name="blocks">Structured content in reading order.</param>
        /// <param name="related">Stable identifiers for related-topic links.</param>
        /// <returns>A catalog article whose visible metadata resolves through <c>Localizer</c>.</returns>
        private static GuideArticle Article(
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
        private static GuideParagraph Para(string id, string name)
        {
            return new GuideParagraph(Key(id, name));
        }

        /// <summary>Creates a localized in-article heading.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Heading-specific localization-key suffix.</param>
        /// <returns>A heading backed by the convention-based localization key.</returns>
        private static GuideHeading Heading(string id, string name)
        {
            return new GuideHeading(Key(id, $"{name}.Heading"));
        }

        /// <summary>Creates a localized ordered procedure.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Procedure-specific localization-key suffix.</param>
        /// <returns>A steps block whose localized value is split on newlines.</returns>
        private static GuideSteps List(string id, string name)
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

        /// <summary>Creates a localized screenshot block backed by a packaged guide image.</summary>
        /// <param name="id">Article identifier used in the localization-key prefix.</param>
        /// <param name="name">Caption-specific localization-key suffix.</param>
        /// <param name="fileName">Filename packaged under the shared guide asset directory.</param>
        /// <returns>A screenshot block whose source resolves to the packaged Avalonia resource.</returns>
        private static GuideScreenshot EmbeddedIllustration(string id, string name, string fileName)
        {
            return new GuideScreenshot(
                Key(id, $"{name}.Caption"),
                fileName,
                $"avares://CtrDxEditor.Shared/Assets/Guide/{fileName}");
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
