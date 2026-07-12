# Contributing to _Cut the Rope DX: Level Editor_

Thank you for your interest in contributing! Please take a moment to review this guide before submitting issues, feature requests, or pull requests.

This guideline applies to code and documentation contributions only. Game assets (images, audio, level files, etc.) are handled internally by the team, so please do not submit pull requests that add or modify assets.

> [!NOTE]
> This guide is not exhaustive. Project practices may evolve, and new situations may arise. When in doubt, feel free to ask questions or open an issue for clarification.

## 📬 Submitting issues and feature requests

To report bugs or request features, please [open an issue](https://github.com/yell0wsuit/ctrdx-editor/issues) and choose the appropriate category.

## 🔀 Submitting pull requests

### ✅ What you should do

- **Use a modern code editor** (e.g., Visual Studio Code) with the C# Dev Kit extension enabled for code checking.
    - On Windows, you can also use Visual Studio 2022 or later for ease of testing.

- **Format your code** before committing and pushing. Periodically run `dotnet format`, or configure your IDE to auto-format on save.

- **Test your code thoroughly** before pushing. Resolve any C# errors, and make sure `dotnet build` and `dotnet test` pass.

- **Use clear, concise variable names** following the [C# naming rules and conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names). Names should be self-explanatory, avoid abbreviations, and reflect the variable's intent or data type.

- **Use a clear, concise pull request title**. If possible, we recommend following [semantic commit message conventions](https://gist.github.com/joshbuchea/6f47e86d2510bce28f8e7f42ae84c716). Examples:
    - `fix: align steam body and conveyor hitbox`
    - `feat: add steam pipe editor support`

        If the title doesn't fully describe your changes, please provide a detailed description in the PR body.

- If your changes aren't ready yet, consider using a **draft pull request** and prefix the title with `[WIP]` (Work In Progress). When it's ready, remove the prefix and mark the PR as ready.

- If your pull request is outdated compared to the main branch, we recommend rebasing it to keep the commit history linear. Merging `main` into your branch is discouraged, as it introduces unnecessary merge commits and makes the history harder to review.
    - If any of your changes conflict with the main branch, resolve the conflicts manually and ensure the final result is compatible with the current codebase.
    - If the rebase is complex, consider restarting your branch from the latest `main` and cherry-picking your commits onto the new branch.

- Use **multiple small commits** with clear messages when possible. This improves readability and makes it easier to review specific changes.

- Before submitting a **large pull request** or major change, open an issue first and select the appropriate category. After a review by our team, you can start your work.

- Because the editor authors levels for [Cut the Rope: DX](https://github.com/yell0wsuit/cuttherope-dx), object behavior must **match the game's actual code**, which is the source of truth. When changing how an object is parsed, placed, or rendered, verify it against the game rather than guessing.

- The editor must preserve a **lossless XML round-trip**: opening and re-saving a level must never rewrite layers or attributes the editor doesn't understand. Keep unknown data verbatim.

- After completing your changes, run the following commands to ensure code quality and consistency:

    ```bash
    # Format code (most important)
    dotnet format

    # Test for building errors
    dotnet build

    # Run the test suite
    dotnet test
    ```

### 🧪 Review process

- All PRs are reviewed before merging. Please be responsive to feedback.
   When addressing comments, make a new commit with a message like:
   `address feedback by @<username>`

### 🤔 What you should NOT do

- Submit low-effort or noise PRs, including, but not limited to: unnecessary README edits, cosmetic documentation tweaks, or changes unrelated to an actual issue or improvement.
    - If a change does not fix a bug, add a feature, or meaningfully improve documentation, it likely does not belong in a pull request.
    - Users who repeatedly submit low-effort or spam PRs may be blocked from further contributions.

- Submit pull requests with **only cosmetic changes** (e.g., whitespace tweaks or reformatting without functional impact).
    - These changes clutter diffs and make code reviews harder. [See this comment by the Rails team](https://github.com/rails/rails/pull/13771#issuecomment-32746700).
    - Always run `dotnet format` before committing to avoid unnecessary diffs.

- Submit a pull request with **one or several big commit(s)**. This makes it difficult to review.

- Use unclear, vague, or default commit messages like `Update file`, `fix`, or `misc changes`.

- Modify configuration files (e.g., `.editorconfig`, `*.slnx`, `*.csproj`, `Directory.*.props`, etc.) or any files in the `.github` folder without prior discussion.

### 🚫 Prohibited actions

- Add code that is unclear in intent or function.

- Add code or commits that:
    - Are **malicious** or **unsafe**
    - **Execute scripts from external sources** associated with malicious, unsafe, or illegal behavior
    - Attempt to introduce **backdoors** or hidden functionality

        Any code violating these rules will result in the contributor being **blocked** and **reported** to GitHub for Terms of Service violations.

- Add code, assets, or implementation details copied from leaked, stolen, or otherwise unauthorized sources.
    - Public availability does not mean the material is legally or ethically safe to use.
    - Contributions based on leaked or unauthorized material will be rejected.

- Use expletives or offensive language. This project is intended for everyone, and we strive to maintain a respectful environment for all contributors and users.

---

Thank you again for helping us improve the project!
