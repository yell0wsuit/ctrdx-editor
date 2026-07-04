namespace CtrDxEditor.Content
{
    /// <summary>Which stage of a content install is currently running.</summary>
    public enum InstallStage
    {
        /// <summary>Transferring bytes from the network; the reported fraction is meaningful.</summary>
        Downloading,

        /// <summary>Unpacking and hash-verifying the bundle; progress is indeterminate.</summary>
        Verifying,
    }

    /// <summary>Progress of a content install: the current stage plus a [0, 1] fraction (only meaningful while <see cref="InstallStage.Downloading"/>).</summary>
    public readonly record struct InstallProgress(InstallStage Stage, double Fraction);
}
