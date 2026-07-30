namespace CtrDxEditor.Platform
{
    /// <summary>
    /// Draws the user's attention to the editor window - a flashing taskbar button on Windows, a
    /// bouncing dock icon on macOS. Absent on heads that have no such signal (the browser).
    /// </summary>
    public interface IUserAttention
    {
        /// <summary>
        /// Asks the platform to alert the user that the editor needs them. Best-effort: it never steals
        /// focus, and does nothing where the platform offers no attention signal. Call on the UI thread.
        /// </summary>
        void Demand();
    }
}
