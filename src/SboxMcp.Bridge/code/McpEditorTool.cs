using System;
using System.Collections.Generic;
using Editor; // NOTE: s&box API - verify against your version
using Sandbox; // NOTE: s&box API - verify against your version

namespace SboxMcp.Bridge;

/// <summary>
/// s&box Editor Tool entry point for the MCP Bridge.
/// Appears in the editor as a panel and manages the WebSocket client lifecycle.
/// </summary>
[Title( "MCP Bridge" )] // NOTE: s&box API - verify against your version
[Icon( "smart_toy" )]   // NOTE: s&box API - verify against your version
[EditorTool]            // NOTE: s&box API - verify against your version
public class McpEditorTool : EditorWindow // NOTE: s&box API - verify against your version
{
	private McpBridgeClient? _client;

	private int _port = 29015;
	private readonly List<string> _recentCommands = new();
	private const int MaxLogEntries = 50;

	/// <summary>
	/// Called when the editor tool window is first opened.
	/// </summary>
	protected override void OnCreate() // NOTE: s&box API - verify against your version
	{
		base.OnCreate();
		Title = "MCP Bridge";
		Size = new Vector2( 320, 480 ); // NOTE: s&box API - verify against your version

		// Auto-start connection when the editor loads.
		StartClient();
	}

	/// <summary>
	/// Called when the window is closed/destroyed.
	/// </summary>
	protected override void OnDestroy() // NOTE: s&box API - verify against your version
	{
		StopClient();
		base.OnDestroy();
	}

	private void StartClient()
	{
		StopClient();
		var url = $"ws://localhost:{_port}";
		_client = new McpBridgeClient( url );
		_client.Connect();
		Log.Info( $"[MCP Bridge] Client started, connecting to {url}" ); // NOTE: s&box API - verify against your version
		AddLog( $"Connecting to {url}..." );
	}

	private void StopClient()
	{
		if ( _client is null )
			return;
		_client.Disconnect();
		_client.Dispose();
		_client = null;
		AddLog( "Disconnected." );
	}

	private void AddLog( string message )
	{
		var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
		_recentCommands.Insert( 0, entry );
		if ( _recentCommands.Count > MaxLogEntries )
			_recentCommands.RemoveAt( _recentCommands.Count - 1 );
	}

	/// <summary>
	/// Renders the editor panel UI using the s&box immediate-mode UI system.
	/// </summary>
	protected override void OnPaint() // NOTE: s&box API - verify against your version
	{
		base.OnPaint();

		using var layout = Layout.Column(); // NOTE: s&box API - verify against your version
		layout.Margin = 8;
		layout.Spacing = 6;

		// --- Status row ---
		using ( layout.Row() )
		{
			var connected = _client?.IsConnected ?? false;
			var statusColor = connected ? Color.Green : Color.Red; // NOTE: s&box API - verify against your version
			var statusText  = connected ? "Connected" : "Disconnected";

			Paint.SetPen( statusColor ); // NOTE: s&box API - verify against your version
			layout.Add( new Label( $"Status: {statusText}" ) ); // NOTE: s&box API - verify against your version
		}

		// --- Port field ---
		using ( layout.Row() )
		{
			layout.Add( new Label( "Port:" ) ); // NOTE: s&box API - verify against your version
			var portField = new LineEdit( _port.ToString() ); // NOTE: s&box API - verify against your version
			portField.ValueChanged += v =>
			{
				if ( int.TryParse( v, out var p ) )
					_port = p;
			};
			layout.Add( portField );
		}

		// --- Connect / Disconnect button ---
		using ( layout.Row() )
		{
			var connected = _client?.IsConnected ?? false;
			var btn = new Button( connected ? "Disconnect" : "Connect" ); // NOTE: s&box API - verify against your version
			btn.Clicked += () =>
			{
				if ( _client?.IsConnected == true )
					StopClient();
				else
					StartClient();
			};
			layout.Add( btn );
		}

		layout.AddSeparator(); // NOTE: s&box API - verify against your version

		// --- Recent command log ---
		layout.Add( new Label( "Recent Commands:" ) ); // NOTE: s&box API - verify against your version
		using var scroll = new ScrollArea(); // NOTE: s&box API - verify against your version
		var logLayout = scroll.SetContent( Layout.Column() );
		foreach ( var entry in _recentCommands )
		{
			logLayout.Add( new Label( entry ) ); // NOTE: s&box API - verify against your version
		}
		layout.Add( scroll );
	}
}
