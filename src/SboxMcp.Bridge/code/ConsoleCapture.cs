namespace SboxMcp.Bridge;

/// <summary>
/// Captures recent Log output so editor.console_output can return it.
/// Appends entries via AddEntry() — call that from any log hook or manually.
/// </summary>
public static class ConsoleCapture
{
	private static readonly List<string> _entries = new();
	private static readonly object _lock = new();
	private const int MaxEntries = 200;
	private static bool _hooked;

	/// <summary>
	/// Call once during bridge initialisation to start capturing log output.
	/// Safe to call multiple times — only hooks once.
	/// </summary>
	public static void EnsureHooked()
	{
		if ( _hooked )
			return;
		_hooked = true;

		// Hook into the s&box log system if the API is available.
		// Log.OnEntry is the standard s&box event for log messages.
		try
		{
			Log.OnEntry += OnLogEntry;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[MCP Bridge] ConsoleCapture: could not hook Log.OnEntry: {ex.Message}" );
		}
	}

	/// <summary>
	/// Manually append a line (e.g. from ExecutionHandler output).
	/// </summary>
	public static void AddEntry( string line )
	{
		lock ( _lock )
		{
			_entries.Insert( 0, line );
			if ( _entries.Count > MaxEntries )
				_entries.RemoveAt( _entries.Count - 1 );
		}
	}

	/// <summary>
	/// Returns a snapshot of recent log lines (newest-first).
	/// </summary>
	public static List<string> GetRecent()
	{
		lock ( _lock )
		{
			return new List<string>( _entries );
		}
	}

	private static void OnLogEntry( string channel, LogLevel level, string message, string trace )
	{
		var line = $"[{level}] {message}";
		AddEntry( line );
	}
}
