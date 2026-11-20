namespace Sims1Pkg;

/// <summary>
/// Records each change to the game folder with a way to undo it, so a failed install or uninstall can put everything
/// back.
/// </summary>
/// <param name="afterStep">Called after each step with its number, from 1. Tests use it to fail at every step.</param>
internal sealed class Journal(Action<int>? afterStep)
{
    private readonly Stack<Action> _undo = new();
    private int _steps;

    /// <summary>
    /// Does <paramref name="action"/>, and remembers <paramref name="undo"/> once it has succeeded.
    /// </summary>
    public void Do(Action action, Action undo)
    {
        action();
        _undo.Push(undo);
        afterStep?.Invoke(++_steps);
    }

    /// <summary>
    /// Moves a file without replacing anything at <paramref name="to"/>.
    /// </summary>
    public void MoveFile(string from, string to) =>
        Do(() => File.Move(from, to), () => File.Move(to, from));

    /// <summary>
    /// Moves a folder without replacing anything at <paramref name="to"/>.
    /// </summary>
    public void MoveFolder(string from, string to) =>
        Do(() => Directory.Move(from, to), () => Directory.Move(to, from));

    /// <summary>
    /// Creates a folder and any missing parents. Undoing removes the ones it created.
    /// </summary>
    public void CreateFolder(string path)
    {
        var missing = new List<string>();
        for (string? folder = path; folder is not null && !Directory.Exists(folder); folder = Path.GetDirectoryName(folder))
        {
            missing.Add(folder);
        }

        missing.Reverse();
        foreach (string folder in missing)
        {
            Do(() => Directory.CreateDirectory(folder), () => Directory.Delete(folder));
        }
    }

    /// <summary>
    /// Forgets every undo, once the change is complete.
    /// </summary>
    public void Commit() => _undo.Clear();

    /// <summary>
    /// Undoes every step, newest first. Keeps going past failures.
    /// </summary>
    /// <returns>The undos that failed. Empty when the folder is back as it was.</returns>
    public IReadOnlyList<Exception> Rollback()
    {
        var failures = new List<Exception>();
        while (_undo.TryPop(out Action? undo))
        {
            try
            {
                undo();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                failures.Add(exception);
            }
        }

        return failures;
    }
}
