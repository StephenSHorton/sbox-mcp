using SboxMcp.Bridge;

namespace SboxMcp.Bridge.Handlers;

/// <summary>
/// Handles cloud asset / local asset commands: asset.search, asset.fetch,
/// asset.mount, asset.browse_local.
/// </summary>
public static class AssetHandler
{
	/// <summary>
	/// asset.search — Search the s&box asset store for packages.
	/// Params: { "query": string, "type"?: string, "amount"?: string (default "20") }
	/// </summary>
	public static async Task<object> SearchAssets( BridgeRequest request )
	{
		var query  = GetParam( request, "query" );
		var amount = GetParamOptional( request, "amount" ) ?? "20";

		if ( !int.TryParse( amount, out var take ) || take <= 0 )
			take = 20;

		try
		{
			var findResult = await Package.FindAsync( query, take: take );

			var results = new List<object>();
			if ( findResult.Packages is not null )
			{
				foreach ( var pkg in findResult.Packages )
				{
					results.Add( new
					{
						ident       = pkg.FullIdent,
						title       = pkg.Title,
						description = pkg.Description,
						type        = pkg.TypeName ?? "",
						thumb       = pkg.Thumb,
					} );
				}
			}

			Log.Info( $"[MCP Bridge] asset.search '{query}' -> {results.Count} results" );
			return results;
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"asset.search failed: {ex.Message}", ex );
		}
	}

	/// <summary>
	/// asset.fetch — Fetch metadata for a specific package by ident.
	/// Params: { "ident": string }
	/// </summary>
	public static async Task<object> FetchAsset( BridgeRequest request )
	{
		var ident = GetParam( request, "ident" );

		try
		{
			var pkg = await Package.Fetch( ident, false );
			if ( pkg is null )
				throw new KeyNotFoundException( $"Package not found: {ident}" );

			Log.Info( $"[MCP Bridge] asset.fetch '{ident}' ok" );
			return new
			{
				ident        = pkg.FullIdent,
				title        = pkg.Title,
				description  = pkg.Description,
				type         = pkg.TypeName ?? "",
				thumb        = pkg.Thumb,
				primaryAsset = pkg.GetMeta( "PrimaryAsset", "" ),
			};
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"asset.fetch failed: {ex.Message}", ex );
		}
	}

	/// <summary>
	/// asset.mount — Mount a package by ident so its assets are available locally.
	/// Params: { "ident": string }
	/// </summary>
	public static async Task<object> MountAsset( BridgeRequest request )
	{
		var ident = GetParam( request, "ident" );

		try
		{
			var pkg = await Package.Fetch( ident, false );
			if ( pkg is null )
				throw new KeyNotFoundException( $"Package not found: {ident}" );

			await pkg.MountAsync();
			AddPackageReference( ident );

			var primary = pkg.GetMeta( "PrimaryAsset", "" );
			Log.Info( $"[MCP Bridge] asset.mount '{ident}' ok, primaryAsset={primary}" );
			return new
			{
				mounted      = true,
				ident        = pkg.FullIdent,
				primaryAsset = primary,
			};
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"asset.mount failed: {ex.Message}", ex );
		}
	}

	/// <summary>
	/// asset.browse_local — Enumerate local project assets.
	/// Params: { "directory"?: string (default "/"), "extension"?: string (e.g. ".vmdl") }
	/// </summary>
	public static Task<object> BrowseLocalAssets( BridgeRequest request )
	{
		var directory = GetParamOptional( request, "directory" ) ?? "/";
		var extension = GetParamOptional( request, "extension" );

		try
		{
			var results = new List<object>();

			IEnumerable<Asset> assets = AssetSystem.All;

			foreach ( var asset in assets )
			{
				var path = asset.Path;

				// Filter by directory prefix
				if ( !string.IsNullOrEmpty( directory ) && directory != "/" )
				{
					var dir = directory.TrimEnd( '/' );
					if ( !path.StartsWith( dir, StringComparison.OrdinalIgnoreCase ) )
						continue;
				}

				// Filter by extension
				if ( !string.IsNullOrEmpty( extension ) )
				{
					if ( !path.EndsWith( extension, StringComparison.OrdinalIgnoreCase ) )
						continue;
				}

				results.Add( new
				{
					path      = asset.Path,
					name      = asset.Name,
					assetType = asset.AssetType?.ToString() ?? "",
				} );
			}

			Log.Info( $"[MCP Bridge] asset.browse_local dir='{directory}' ext='{extension}' -> {results.Count} results" );
			return Task.FromResult<object>( results );
		}
		catch ( Exception ex )
		{
			throw new InvalidOperationException( $"asset.browse_local failed: {ex.Message}", ex );
		}
	}

	// -------------------------------------------------------------------------
	// Project reference helpers
	// -------------------------------------------------------------------------

	/// <summary>
	/// Adds a package ident to the project's PackageReferences in .sbproj so it
	/// auto-mounts on project load. Safe to call multiple times — skips duplicates.
	/// </summary>
	public static void AddPackageReference( string ident )
	{
		try
		{
			// Find .sbproj by searching the project root directory
			var assetsPath = Project.Current?.GetAssetsPath();
			if ( assetsPath is null ) return;

			var projectDir = System.IO.Path.GetDirectoryName( assetsPath.TrimEnd( '/', '\\' ) );
			if ( projectDir is null ) return;

			var sbprojFiles = System.IO.Directory.GetFiles( projectDir, "*.sbproj" );
			if ( sbprojFiles.Length == 0 ) return;

			var projectPath = sbprojFiles[0];

			var json = System.IO.File.ReadAllText( projectPath );
			var doc = System.Text.Json.JsonDocument.Parse( json );

			// Check if already present
			if ( doc.RootElement.TryGetProperty( "PackageReferences", out var refs ) )
			{
				foreach ( var item in refs.EnumerateArray() )
				{
					if ( item.GetString() == ident )
						return; // already referenced
				}
			}

			// Re-serialize with the new reference added
			var node = System.Text.Json.Nodes.JsonNode.Parse( json );
			var arr = node["PackageReferences"]?.AsArray();
			if ( arr is null )
			{
				arr = new System.Text.Json.Nodes.JsonArray();
				node["PackageReferences"] = arr;
			}
			arr.Add( ident );

			var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
			System.IO.File.WriteAllText( projectPath, node.ToJsonString( options ) );
			Log.Info( $"[MCP Bridge] Added '{ident}' to PackageReferences" );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[MCP Bridge] Could not add package reference: {ex.Message}" );
		}
	}

	// -------------------------------------------------------------------------
	// Param helpers
	// -------------------------------------------------------------------------

	private static string GetParam( BridgeRequest request, string key )
	{
		var val = GetParamOptional( request, key );
		if ( val is null )
			throw new ArgumentException( $"Missing required parameter: {key}" );
		return val;
	}

	private static string GetParamOptional( BridgeRequest request, string key )
	{
		if ( request.Params is not JsonElement el )
			return null;
		if ( el.TryGetProperty( key, out var prop ) )
			return prop.GetString();
		return null;
	}
}
