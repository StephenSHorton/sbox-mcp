namespace SboxMcp.Bridge.Handlers;

/// <summary>
/// Handles code execution and console commands: execute.csharp, console.run.
/// </summary>
public static class ExecutionHandler
{
	/// <summary>
	/// execute.csharp — Execute C# code in the editor context.
	///
	/// Full dynamic Roslyn scripting requires Microsoft.CodeAnalysis.CSharp.Scripting,
	/// which may not be available in all s&box editor builds. This implementation
	/// provides a best-effort approach and returns a descriptive placeholder until
	/// the scripting API stabilises.
	///
	/// Params: { "code": "string of C# code or expression" }
	/// </summary>
	public static Task<object> ExecuteCSharp( BridgeRequest request )
	{
		var code = GetParam( request, "code" );

		Log.Info( $"[MCP Bridge] execute.csharp requested ({code.Length} chars)" );

		try
		{
			var result = TryCompileAndRun( code );
			return Task.FromResult<object>( (object)new
			{
				executed = true,
				result,
				note = "Executed via s&box compiler pipeline (verify API availability).",
			} );
		}
		catch ( NotSupportedException )
		{
			Log.Warning( "[MCP Bridge] Dynamic C# execution not available in this s&box build." );
			return Task.FromResult<object>( (object)new
			{
				executed = false,
				result   = "",
				note     = "Dynamic C# scripting is not yet supported in this build. " +
				           "Add the code to a compiled source file and reload the project.",
			} );
		}
	}

	/// <summary>
	/// console.run — Execute a console command.
	/// Params: { "command": "convar value" }
	/// </summary>
	public static Task<object> RunConsoleCommand( BridgeRequest request )
	{
		var command = GetParam( request, "command" );

		Log.Info( $"[MCP Bridge] console.run: {command}" );

		try
		{
			Sandbox.ConsoleSystem.Run( command );

			return Task.FromResult<object>( (object)new
			{
				executed = true,
				command,
				note = "Command dispatched to ConsoleSystem.",
			} );
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"Console command failed: {ex.Message}", ex );
		}
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Stub: throws NotSupportedException until a stable s&box Roslyn scripting API exists.
	/// </summary>
	private static string TryCompileAndRun( string code )
	{
		throw new NotSupportedException( "Dynamic C# execution not implemented." );
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
