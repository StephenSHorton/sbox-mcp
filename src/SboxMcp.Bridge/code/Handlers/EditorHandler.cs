namespace SboxMcp.Bridge.Handlers;

/// <summary>
/// Handles editor-specific commands: editor.get_selection, editor.select,
/// editor.undo, editor.redo, editor.save_scene, editor.screenshot, scene.hierarchy.
/// </summary>
public static class EditorHandler
{
	/// <summary>
	/// editor.get_selection — Get the currently selected GameObjects in the editor.
	/// </summary>
	public static Task<object> HandleGetSelection( BridgeRequest request )
	{
		try
		{
			// NOTE: s&box API - verify
			var selected = SceneEditorSession.Active?.Selection?.OfType<GameObject>().ToList();

			if ( selected is null || selected.Count == 0 )
				return Task.FromResult<object>( new List<object>() );

			var results = selected.Select( go => (object)new
			{
				id   = go.Id.ToString(),
				name = go.Name,
			} ).ToList();

			return Task.FromResult<object>( results );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[MCP Bridge] editor.get_selection failed: {ex.Message}" );
			throw new InvalidOperationException( $"Could not get editor selection: {ex.Message}", ex );
		}
	}

	/// <summary>
	/// editor.select — Select a GameObject by ID.
	/// Params: { "objectId": "guid-string" }
	/// </summary>
	public static Task<object> HandleSelectObject( BridgeRequest request )
	{
		var objectId = GetParam( request, "objectId" );

		if ( !Guid.TryParse( objectId, out var guid ) )
			throw new ArgumentException( $"Invalid GUID: {objectId}" );

		var scene = Game.ActiveScene; // NOTE: s&box API - verify
		if ( scene is null )
			throw new InvalidOperationException( "No active scene." );

		GameObject target = null;
		foreach ( var go in EnumerateAll( scene ) )
		{
			if ( go.Id == guid )
			{
				target = go;
				break;
			}
		}

		if ( target is null )
			throw new KeyNotFoundException( $"GameObject not found: {objectId}" );

		try
		{
			// NOTE: s&box API - verify
			var session = SceneEditorSession.Active;
			if ( session is not null )
			{
				session.Selection.Clear();
				session.Selection.Add( target );
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[MCP Bridge] editor.select could not set gizmo selection: {ex.Message}" );
		}

		Log.Info( $"[MCP Bridge] Selected GameObject '{target.Name}' ({objectId})" );
		return Task.FromResult<object>( (object)new
		{
			selected = true,
			id       = target.Id.ToString(),
			name     = target.Name,
		} );
	}

	/// <summary>
	/// editor.undo — Undo the last editor action.
	/// </summary>
	public static Task<object> HandleUndo( BridgeRequest request )
	{
		try
		{
			// NOTE: s&box API - verify
			SceneEditorSession.Active?.UndoSystem.Undo();
			Log.Info( "[MCP Bridge] editor.undo dispatched" );
			return Task.FromResult<object>( (object)new { success = true, action = "undo" } );
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"Undo failed: {ex.Message}", ex );
		}
	}

	/// <summary>
	/// editor.redo — Redo the last undone editor action.
	/// </summary>
	public static Task<object> HandleRedo( BridgeRequest request )
	{
		try
		{
			// NOTE: s&box API - verify
			SceneEditorSession.Active?.UndoSystem.Redo();
			Log.Info( "[MCP Bridge] editor.redo dispatched" );
			return Task.FromResult<object>( (object)new { success = true, action = "redo" } );
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"Redo failed: {ex.Message}", ex );
		}
	}

	/// <summary>
	/// editor.save_scene — Save the current scene.
	/// </summary>
	public static Task<object> HandleSaveScene( BridgeRequest request )
	{
		try
		{
			// NOTE: s&box API - verify
			SceneEditorSession.Active?.Save( false );
			Log.Info( "[MCP Bridge] editor.save_scene dispatched" );
			return Task.FromResult<object>( (object)new { success = true, action = "save_scene" } );
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"Save scene failed: {ex.Message}", ex );
		}
	}

	/// <summary>
	/// editor.screenshot — Take a screenshot of the editor viewport.
	/// Returns the file path of the saved screenshot.
	/// </summary>
	public static Task<object> HandleScreenshot( BridgeRequest request )
	{
		// TODO: Implement viewport screenshot capture via Camera.RenderToPixmap
		// The s&box editor does not expose a simple screenshot API.
		// A future implementation could grab the active SceneViewWidget's camera
		// and render to a Pixmap, then save to disk.
		Log.Info( "[MCP Bridge] editor.screenshot — not yet implemented" );
		return Task.FromResult<object>( (object)new { success = false, error = "Screenshot not yet implemented. Use the built-in screenshot_highres console command instead." } );
	}

	// -------------------------------------------------------------------------
	// Hierarchy helpers (used by SceneHandler.HandleHierarchy)
	// -------------------------------------------------------------------------

	/// <summary>
	/// Builds an indented tree string for the given scene, e.g.:
	/// Scene
	/// ├── Directional Light
	/// ├── Player
	/// │   ├── Camera
	/// │   └── Model
	/// └── Ground
	/// </summary>
	public static string BuildHierarchyText( Scene scene )
	{
		var sb = new System.Text.StringBuilder();
		sb.AppendLine( scene.Name ?? "Scene" );

		var rootChildren = scene.Children.ToList(); // NOTE: s&box API - verify
		for ( var i = 0; i < rootChildren.Count; i++ )
		{
			var isLast = i == rootChildren.Count - 1;
			AppendNode( sb, rootChildren[i], "", isLast );
		}

		return sb.ToString().TrimEnd();
	}

	private static void AppendNode( System.Text.StringBuilder sb, GameObject go, string indent, bool isLast )
	{
		var connector = isLast ? "└── " : "├── ";
		sb.AppendLine( indent + connector + go.Name );

		var childIndent = indent + ( isLast ? "    " : "│   " );
		var children    = go.Children.ToList();
		for ( var i = 0; i < children.Count; i++ )
		{
			var childIsLast = i == children.Count - 1;
			AppendNode( sb, children[i], childIndent, childIsLast );
		}
	}

	// -------------------------------------------------------------------------
	// Private helpers
	// -------------------------------------------------------------------------

	private static IEnumerable<GameObject> EnumerateAll( Scene scene )
	{
		foreach ( var child in scene.Children )
		{
			foreach ( var go in EnumerateRecursive( child ) )
				yield return go;
		}
	}

	private static IEnumerable<GameObject> EnumerateRecursive( GameObject go )
	{
		yield return go;
		foreach ( var child in go.Children )
		{
			foreach ( var desc in EnumerateRecursive( child ) )
				yield return desc;
		}
	}

	private static string GetParam( BridgeRequest request, string key )
	{
		if ( request.Params is JsonElement el && el.TryGetProperty( key, out var prop ) )
		{
			var val = prop.GetString();
			if ( val is not null ) return val;
		}
		throw new ArgumentException( $"Missing required parameter: {key}" );
	}
}
