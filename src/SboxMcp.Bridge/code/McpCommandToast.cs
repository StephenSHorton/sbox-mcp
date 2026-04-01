namespace SboxMcp.Bridge;

/// <summary>
/// Toast notification shown when an MCP command is being executed.
/// Reuses a single toast instance, auto-dismisses after completion.
/// </summary>
internal class McpCommandToast : ToastWidget
{
	static McpCommandToast _active;

	string _command;
	int _completed;
	int _total;

	public McpCommandToast() : base()
	{
		Icon = "smart_toy";
		BorderColor = Theme.Blue;
		FixedWidth = 320;
		FixedHeight = 76;
		DrawTimer = true;
	}

	/// <summary>
	/// Show a toast for a new MCP command. Reuses existing toast if one is active.
	/// </summary>
	public static void Show( string command )
	{
		if ( _active.IsValid() )
		{
			_active._total++;
			_active.UpdateDisplay();
			_active.Reset();
			return;
		}

		_active = new McpCommandToast();
		_active._command = command;
		_active._completed = 0;
		_active._total = 1;
		_active.UpdateDisplay();
	}

	/// <summary>
	/// Mark the current command as completed.
	/// </summary>
	public static void Complete( string command, bool success )
	{
		if ( !_active.IsValid() )
			return;

		_active._completed++;
		_active._command = command;
		_active.IsRunning = _active._completed < _active._total;

		if ( success )
		{
			_active.BorderColor = Theme.Green;
			_active.Icon = "check_circle";
		}
		else
		{
			_active.BorderColor = Theme.Red;
			_active.Icon = "error";
		}

		_active.UpdateDisplay();

		if ( !_active.IsRunning )
			ToastManager.Remove( _active, 2 );
	}

	void UpdateDisplay()
	{
		Title = $"MCP: {_command}";

		if ( IsRunning )
		{
			Subtitle = _total > 1
				? $"Running ({_completed}/{_total} complete)..."
				: "Running...";
		}
		else
		{
			Subtitle = _total > 1
				? $"{_total} commands completed"
				: "Done";
		}
	}

	public override void Tick()
	{
		// Keep display fresh
		UpdateDisplay();
	}
}
