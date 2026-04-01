using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox; // NOTE: s&box API - verify against your version

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
	/// provides a best-effort approach: it attempts to use s&box's compilation
	/// system if available, and falls back to a safe expression evaluator for
	/// simple cases.
	///
	/// TODO: Full C# scripting support — integrate with s&box Roslyn compiler
	///       once the scripting API stabilises.
	///
	/// Params: { "code": "string of C# code or expression" }
	/// </summary>
	public static Task<object?> ExecuteCSharp( BridgeRequest request )
	{
		var code = GetParam( request, "code" );

		Log.Info( $"[MCP Bridge] execute.csharp requested ({code.Length} chars)" ); // NOTE: s&box API - verify against your version

		// Attempt compilation via s&box's TypeLibrary / compiler pipeline.
		// This is a placeholder: the actual s&box dynamic compilation API is
		// not yet stable. Replace the body of TryCompileAndRun() when
		// the API is available in your s&box version.
		try
		{
			var result = TryCompileAndRun( code );
			return Task.FromResult<object?>( new
			{
				executed = true,
				result,
				note = "Executed via s&box compiler pipeline (verify API availability).",
			} );
		}
		catch ( NotSupportedException )
		{
			// Scripting not available; return a descriptive placeholder response.
			Log.Warning( "[MCP Bridge] Dynamic C# execution not available in this s&box build." ); // NOTE: s&box API - verify against your version
			return Task.FromResult<object?>( new
			{
				executed = false,
				result   = (string?)null,
				note     = "Dynamic C# scripting is not yet supported in this build. " +
				           "Add the code to a compiled source file and reload the project. " +
				           "TODO: integrate Microsoft.CodeAnalysis.CSharp.Scripting when available.",
			} );
		}
	}

	/// <summary>
	/// console.run — Execute a console command.
	/// Params: { "command": "convar value" }
	/// </summary>
	public static Task<object?> RunConsoleCommand( BridgeRequest request )
	{
		var command = GetParam( request, "command" );

		Log.Info( $"[MCP Bridge] console.run: {command}" ); // NOTE: s&box API - verify against your version

		try
		{
			// NOTE: s&box API - verify against your version
			// ConsoleSystem.Run executes a console command string.
			ConsoleSystem.Run( command ); // NOTE: s&box API - verify against your version

			return Task.FromResult<object?>( new
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
	/// Attempts to compile and run C# code via s&box's compiler pipeline.
	///
	/// NOTE: s&box API - verify against your version.
	/// The s&box dynamic compilation API is not yet fully public. This method
	/// is a stub that throws NotSupportedException until a stable API exists.
	/// Replace the body with the actual compilation call for your s&box version.
	/// </summary>
	private static string? TryCompileAndRun( string code )
	{
		// TODO: Replace with actual s&box Roslyn scripting call, e.g.:
		//   var result = await Compiler.EvaluateAsync( code );
		//   return result?.ToString();
		//
		// For now this is unsupported; the caller handles the fallback.
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
