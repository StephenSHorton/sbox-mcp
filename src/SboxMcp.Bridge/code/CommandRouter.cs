using SboxMcp.Bridge.Handlers;

namespace SboxMcp.Bridge;

/// <summary>
/// Routes incoming BridgeRequests to the appropriate handler based on command prefix.
/// All handler calls are dispatched to the main thread since s&box editor APIs
/// must run on the main thread.
/// </summary>
public static class CommandRouter
{
	private delegate Task<object> HandlerFunc( BridgeRequest request );

	private static readonly Dictionary<string, HandlerFunc> Handlers = new()
	{
		// Scene commands
		["scene.list"]          = r => SceneHandler.ListObjects( r ),
		["scene.get"]           = r => SceneHandler.GetObject( r ),
		["scene.create"]        = r => SceneHandler.CreateObject( r ),
		["scene.delete"]        = r => SceneHandler.DeleteObject( r ),
		["scene.find"]          = r => SceneHandler.FindObjects( r ),
		["scene.set_transform"] = r => SceneHandler.SetTransform( r ),
		["scene.hierarchy"]     = r => SceneHandler.GetHierarchy( r ),

		// Component commands
		["component.list"]   = r => ComponentHandler.ListComponents( r ),
		["component.get"]    = r => ComponentHandler.GetComponent( r ),
		["component.set"]    = r => ComponentHandler.SetComponent( r ),
		["component.add"]    = r => ComponentHandler.AddComponent( r ),
		["component.remove"] = r => ComponentHandler.RemoveComponent( r ),

		// File commands
		["file.read"]  = r => FileHandler.ReadFile( r ),
		["file.write"] = r => FileHandler.WriteFile( r ),
		["file.list"]  = r => FileHandler.ListFiles( r ),

		// Project commands
		["project.info"] = r => FileHandler.ProjectInfo( r ),

		// Editor commands
		["editor.get_selection"] = r => EditorHandler.HandleGetSelection( r ),
		["editor.select"]        = r => EditorHandler.HandleSelectObject( r ),
		["editor.undo"]          = r => EditorHandler.HandleUndo( r ),
		["editor.redo"]          = r => EditorHandler.HandleRedo( r ),
		["editor.save_scene"]    = r => EditorHandler.HandleSaveScene( r ),
		["editor.screenshot"]    = r => EditorHandler.HandleScreenshot( r ),

		// Execution commands
		["execute.csharp"] = r => ExecutionHandler.ExecuteCSharp( r ),
		["console.run"]    = r => ExecutionHandler.RunConsoleCommand( r ),
	};

	/// <summary>
	/// Routes a request to its handler. Returns an error response for unknown commands.
	/// Handlers are called directly — the editor main thread dispatches incoming messages.
	/// </summary>
	public static async Task<BridgeResponse> Route( BridgeRequest request )
	{
		if ( !Handlers.TryGetValue( request.Command, out var handler ) )
		{
			Log.Warning( $"[MCP Bridge] Unknown command: {request.Command}" );
			return BridgeResponse.Fail( request.Id, $"Unknown command: {request.Command}" );
		}

		try
		{
			object data = null;

			// Dispatch to main thread — s&box editor APIs must run on the main thread.
			var tcs = new System.Threading.Tasks.TaskCompletionSource<object>();
			MainThread.Queue( async () =>
			{
				try
				{
					var result = await handler( request );
					tcs.SetResult( result );
				}
				catch ( Exception ex )
				{
					tcs.SetException( ex );
				}
			} );

			data = await tcs.Task;
			return BridgeResponse.Ok( request.Id, data );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[MCP Bridge] Handler error for '{request.Command}': {ex.Message}" );
			return BridgeResponse.Fail( request.Id, ex.Message );
		}
	}
}
