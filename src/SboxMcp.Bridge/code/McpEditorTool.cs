namespace SboxMcp.Bridge;

/// <summary>
/// s&box Editor Dock entry point for the MCP Bridge.
/// Appears in the editor as a dockable panel and manages the WebSocket client lifecycle.
/// </summary>
[Dock( "Editor", "MCP Bridge", "smart_toy" )]
public class McpBridgeDock : Widget
{
	public static McpBridgeDock Current { get; private set; }

	public int CommandCount { get; set; }

	private McpBridgeClient _client;
	private int _port = 29015;
	private readonly List<string> _logEntries = new();
	private const int MaxLogEntries = 50;
	private DateTime _connectedAt;

	private Label _statusDot;
	private Label _statusText;
	private Label _urlLabel;
	private Button _connectButton;
	private Label _commandCountLabel;
	private Label _uptimeLabel;
	private Layout _logLayout;
	private LineEdit _portField;

	public McpBridgeDock( Widget parent ) : base( parent )
	{
		Current = this;

		ConsoleCapture.EnsureHooked();

		MinimumSize = 200;
		Layout = Layout.Column();
		Layout.Margin = 8;
		Layout.Spacing = 8;

		BuildHeader();
		BuildControls();
		BuildStats();
		BuildLog();

		StartClient();
	}

	private void BuildHeader()
	{
		var header = Layout.AddRow();
		header.Spacing = 6;

		_statusDot = new Label( "●", this );
		_statusDot.SetStyles( "color: #e05252; font-size: 18px;" );
		header.Add( _statusDot );

		var textCol = header.AddColumn();
		textCol.Spacing = 2;

		_statusText = new Label( "Disconnected", this );
		textCol.Add( _statusText );

		_urlLabel = new Label( $"ws://localhost:{_port}", this );
		_urlLabel.SetStyles( "color: #888888; font-size: 11px;" );
		textCol.Add( _urlLabel );

		header.AddStretchCell();
	}

	private void BuildControls()
	{
		var row = Layout.AddRow();
		row.Spacing = 6;

		var portLabel = new Label( "Port:", this );
		row.Add( portLabel );

		_portField = new LineEdit( this );
		var portField = _portField;
		portField.Text = _port.ToString();
		portField.MinimumWidth = 80;
		portField.MaximumWidth = 100;
		portField.TextEdited += v =>
		{
			if ( int.TryParse( v, out var p ) )
			{
				_port = p;
				_urlLabel.Text = $"ws://localhost:{_port}";
			}
		};
		row.Add( portField );

		row.AddStretchCell();

		_connectButton = new Button( "Connect", this );
		_connectButton.Icon = "link";
		_connectButton.Clicked = () =>
		{
			if ( _client != null && _client.IsConnected )
				StopClient();
			else
				StartClient();
		};
		row.Add( _connectButton );
	}

	private void BuildStats()
	{
		var statsRow = Layout.AddRow();
		statsRow.Spacing = 12;

		var cmdCol = statsRow.AddColumn();
		cmdCol.Spacing = 2;
		cmdCol.Add( new Label( "Commands", this ) );
		_commandCountLabel = new Label( "0", this );
		_commandCountLabel.SetStyles( "font-size: 18px; font-weight: bold;" );
		cmdCol.Add( _commandCountLabel );

		var uptimeCol = statsRow.AddColumn();
		uptimeCol.Spacing = 2;
		uptimeCol.Add( new Label( "Uptime", this ) );
		_uptimeLabel = new Label( "--", this );
		_uptimeLabel.SetStyles( "font-size: 18px; font-weight: bold;" );
		uptimeCol.Add( _uptimeLabel );

		statsRow.AddStretchCell();
	}

	private void BuildLog()
	{
		Layout.Add( new Label( "Activity Log", this ) );

		var scroll = new ScrollArea( this );
		scroll.Canvas = new Widget( this );
		scroll.Canvas.Layout = Layout.Column();
		_logLayout = scroll.Canvas.Layout;
		_logLayout.Spacing = 3;
		_logLayout.Margin = 4;
		Layout.Add( scroll, 1 );
	}

	public override void OnDestroyed()
	{
		if ( Current == this )
			Current = null;
		StopClient();
		base.OnDestroyed();
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( !_statusDot.IsValid() || !_statusText.IsValid() || !_connectButton.IsValid() )
			return;

		var connected = _client != null && _client.IsConnected;

		_statusDot.SetStyles( connected ? "color: #52e052; font-size: 18px;" : "color: #e05252; font-size: 18px;" );
		_statusText.Text = connected ? "Connected" : "Disconnected";
		_connectButton.Text = connected ? "Disconnect" : "Connect";
		_connectButton.Icon = connected ? "link_off" : "link";
		_commandCountLabel.Text = CommandCount.ToString();
		_portField.ReadOnly = connected;

		if ( connected && _connectedAt != default )
		{
			var elapsed = DateTime.Now - _connectedAt;
			_uptimeLabel.Text = elapsed.TotalHours >= 1
				? $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m"
				: $"{elapsed.Minutes}m {elapsed.Seconds:D2}s";
		}
		else
		{
			_uptimeLabel.Text = "--";
		}
	}

	public void AddLog( string message )
	{
		var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
		_logEntries.Insert( 0, entry );
		if ( _logEntries.Count > MaxLogEntries )
			_logEntries.RemoveAt( _logEntries.Count - 1 );

		_logLayout.Clear( true );
		foreach ( var e in _logEntries )
		{
			var lbl = new Label( e, this );
			lbl.SetStyles( "font-size: 11px;" );
			_logLayout.Add( lbl );
		}
	}

	private void StartClient()
	{
		StopClient();
		var url = $"ws://localhost:{_port}";
		_client = new McpBridgeClient( url );
		_client.Connect();
		_connectedAt = DateTime.Now;
		Log.Info( $"[MCP Bridge] Client started, connecting to {url}" );
		AddLog( $"Connecting to {url}..." );
	}

	private void StopClient()
	{
		if ( _client is null )
			return;
		_client.Disconnect();
		_client.Dispose();
		_client = null;
		_connectedAt = default;
		AddLog( "Disconnected." );
	}
}
