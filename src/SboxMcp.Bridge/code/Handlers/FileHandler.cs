using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox; // NOTE: s&box API - verify against your version

namespace SboxMcp.Bridge.Handlers;

/// <summary>
/// Handles file and project commands: file.read, file.write, file.list, project.info.
/// </summary>
public static class FileHandler
{
	/// <summary>
	/// file.read — Read a file relative to the project root.
	/// Params: { "path": "relative/path/to/file.txt" }
	/// </summary>
	public static Task<object?> ReadFile( BridgeRequest request )
	{
		var path = GetParam( request, "path" );

		// Try the s&box mounted filesystem first, fall back to System.IO.
		string content;
		try
		{
			content = FileSystem.Mounted.ReadAllText( path ); // NOTE: s&box API - verify against your version
		}
		catch
		{
			var fullPath = ResolveAbsolutePath( path );
			content = File.ReadAllText( fullPath );
		}

		return Task.FromResult<object?>( new { path, content } );
	}

	/// <summary>
	/// file.write — Write content to a file relative to the project root.
	/// Params: { "path": "relative/path", "content": "file content" }
	/// </summary>
	public static Task<object?> WriteFile( BridgeRequest request )
	{
		var path    = GetParam( request, "path" );
		var content = GetParam( request, "content" );

		bool wrote = false;
		try
		{
			FileSystem.Mounted.WriteAllText( path, content ); // NOTE: s&box API - verify against your version
			wrote = true;
		}
		catch
		{
			// Fall back to System.IO.
			var fullPath = ResolveAbsolutePath( path );
			var dir = Path.GetDirectoryName( fullPath );
			if ( dir is not null )
				Directory.CreateDirectory( dir );
			File.WriteAllText( fullPath, content );
			wrote = true;
		}

		Log.Info( $"[MCP Bridge] Wrote file: {path}" ); // NOTE: s&box API - verify against your version
		return Task.FromResult<object?>( new { path, written = wrote } );
	}

	/// <summary>
	/// file.list — List files in a directory, with optional glob pattern.
	/// Params: { "directory": "path/to/dir", "pattern"?: "*.cs" }
	/// </summary>
	public static Task<object?> ListFiles( BridgeRequest request )
	{
		var directory = GetParam( request, "directory" );
		var pattern   = GetParamOptional( request, "pattern" ) ?? "*";

		var files = new List<string>();

		// Try s&box FileSystem first.
		try
		{
			var found = FileSystem.Mounted.FindFile( directory, pattern ); // NOTE: s&box API - verify against your version
			files.AddRange( found );
		}
		catch
		{
			// Fall back to System.IO.
			var fullPath = ResolveAbsolutePath( directory );
			if ( Directory.Exists( fullPath ) )
			{
				foreach ( var f in Directory.GetFiles( fullPath, pattern, SearchOption.TopDirectoryOnly ) )
					files.Add( Path.GetRelativePath( fullPath, f ) );
			}
		}

		return Task.FromResult<object?>( new { directory, pattern, files } );
	}

	/// <summary>
	/// project.info — Return metadata about the current project.
	/// Params: (none)
	/// </summary>
	public static Task<object?> ProjectInfo( BridgeRequest request )
	{
		string title      = "Unknown";
		string type       = "Unknown";
		string path       = "";
		string activeScene = "";
		int    objectCount = 0;

		try
		{
			var project = Game.ActiveScene?.GameProject; // NOTE: s&box API - verify against your version
			if ( project is not null )
			{
				title = project.Title ?? title; // NOTE: s&box API - verify against your version
				type  = project.Type  ?? type;  // NOTE: s&box API - verify against your version
				path  = project.Path  ?? path;  // NOTE: s&box API - verify against your version
			}
		}
		catch { /* project API may differ between versions */ }

		try
		{
			var scene = Game.ActiveScene; // NOTE: s&box API - verify against your version
			if ( scene is not null )
			{
				activeScene = scene.Name ?? ""; // NOTE: s&box API - verify against your version
				foreach ( var _ in scene.Children ) // NOTE: s&box API - verify against your version
					objectCount++;
			}
		}
		catch { /* scene may be null */ }

		return Task.FromResult<object?>( new
		{
			title,
			type,
			path,
			activeScene,
			gameObjectCount = objectCount,
		} );
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Resolves a relative path against the project root directory.
	/// Falls back to the current working directory if the project root cannot be determined.
	/// </summary>
	private static string ResolveAbsolutePath( string relativePath )
	{
		string root;
		try
		{
			root = Game.ActiveScene?.GameProject?.Path // NOTE: s&box API - verify against your version
				?? Directory.GetCurrentDirectory();

			// If path points to the project file itself, use its directory.
			if ( File.Exists( root ) )
				root = Path.GetDirectoryName( root ) ?? Directory.GetCurrentDirectory();
		}
		catch
		{
			root = Directory.GetCurrentDirectory();
		}

		return Path.Combine( root, relativePath );
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

	private static string? GetParamOptional( BridgeRequest request, string key )
	{
		if ( request.Params is JsonElement el && el.TryGetProperty( key, out var prop ) )
			return prop.GetString();
		return null;
	}
}
