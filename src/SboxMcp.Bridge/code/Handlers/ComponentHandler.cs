using SboxMcp.Bridge;

namespace SboxMcp.Bridge.Handlers;

/// <summary>
/// Handles component-related commands: component.list, component.get,
/// component.set, component.add, component.remove.
/// </summary>
public static class ComponentHandler
{
	/// <summary>
	/// component.list — List all components on a GameObject.
	/// Params: { "id": "guid-string" }
	/// </summary>
	public static Task<object> ListComponents( BridgeRequest request )
	{
		var go = ResolveGameObject( request );
		var list = new List<object>();

		foreach ( var comp in go.Components.GetAll() )
		{
			list.Add( new
			{
				type    = comp.GetType().Name,
				enabled = comp.Enabled,
			} );
		}

		return Task.FromResult<object>( list );
	}

	/// <summary>
	/// component.get — Get a specific component's properties by type name.
	/// Params: { "id": "guid-string", "type": "TypeName" }
	/// </summary>
	public static Task<object> GetComponent( BridgeRequest request )
	{
		var go       = ResolveGameObject( request );
		var typeName = GetParam( request, "type" );
		var comp     = FindComponentByType( go, typeName );

		var props = new Dictionary<string, object>();
		try
		{
			var td = TypeLibrary.GetType( comp.GetType() );
			if ( td is not null )
			{
				foreach ( var prop in td.Properties )
				{
					try { props[prop.Name] = prop.GetValue( comp )?.ToString() ?? ""; }
					catch { props[prop.Name] = "<error>"; }
				}
			}
			else
			{
				// Fall back to standard reflection if TypeLibrary returns null.
				foreach ( var prop in comp.GetType().GetProperties() )
				{
					try { props[prop.Name] = prop.GetValue( comp )?.ToString() ?? ""; }
					catch { props[prop.Name] = "<error>"; }
				}
			}
		}
		catch ( Exception ex )
		{
			props["_error"] = ex.Message;
		}

		return Task.FromResult<object>( (object)new
		{
			type       = comp.GetType().Name,
			enabled    = comp.Enabled,
			properties = props,
		} );
	}

	/// <summary>
	/// component.set — Set a property on a component.
	/// Params: { "id": "guid", "type": "TypeName", "property": "PropName", "value": "value" }
	/// </summary>
	public static Task<object> SetComponent( BridgeRequest request )
	{
		var go       = ResolveGameObject( request );
		var typeName = GetParam( request, "type" );
		var propName = GetParam( request, "property" );
		var rawValue = GetParam( request, "value" );
		var comp     = FindComponentByType( go, typeName );

		var td = TypeLibrary.GetType( comp.GetType() );
		if ( td is not null )
		{
			var prop = td.Properties.FirstOrDefault( p => p.Name == propName );
			if ( prop is null )
				throw new KeyNotFoundException( $"Property '{propName}' not found on {typeName}" );

			var converted = Convert.ChangeType( rawValue, prop.PropertyType );
			prop.SetValue( comp, converted );
		}
		else
		{
			// Fall back to standard reflection.
			var prop = comp.GetType().GetProperty( propName )
				?? throw new KeyNotFoundException( $"Property '{propName}' not found on {typeName}" );
			var converted = Convert.ChangeType( rawValue, prop.PropertyType );
			prop.SetValue( comp, converted );
		}

		return Task.FromResult<object>( (object)new { set = true, property = propName, value = rawValue } );
	}

	/// <summary>
	/// component.add — Add a component to a GameObject by type name.
	/// Params: { "id": "guid", "type": "TypeName" }
	/// </summary>
	public static Task<object> AddComponent( BridgeRequest request )
	{
		var go       = ResolveGameObject( request );
		var typeName = GetParam( request, "type" );

		var typeDesc = TypeLibrary.GetType( typeName );
		if ( typeDesc is null )
			throw new TypeLoadException( $"Type not found: {typeName}" );

		var comp = go.Components.Create( typeDesc );
		Log.Info( $"[MCP Bridge] Added component {typeName} to {go.Name}" );

		return Task.FromResult<object>( (object)new
		{
			added   = true,
			type    = comp.GetType().Name,
			enabled = comp.Enabled,
		} );
	}

	/// <summary>
	/// component.remove — Remove a component by type name.
	/// Params: { "id": "guid", "type": "TypeName" }
	/// </summary>
	public static Task<object> RemoveComponent( BridgeRequest request )
	{
		var go       = ResolveGameObject( request );
		var typeName = GetParam( request, "type" );
		var comp     = FindComponentByType( go, typeName );

		comp.Destroy();
		Log.Info( $"[MCP Bridge] Removed component {typeName} from {go.Name}" );

		return Task.FromResult<object>( (object)new { removed = true, type = typeName } );
	}

	// -------------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------------

	private static GameObject ResolveGameObject( BridgeRequest request )
	{
		var id = GetParam( request, "id" );
		if ( !Guid.TryParse( id, out var guid ) )
			throw new ArgumentException( $"Invalid GUID: {id}" );

		return SceneHandler.FindObjectById( guid )
			?? throw new KeyNotFoundException( $"GameObject not found: {id}" );
	}

	private static Component FindComponentByType( GameObject go, string typeName )
	{
		foreach ( var comp in go.Components.GetAll() )
		{
			if ( comp.GetType().Name.Equals( typeName, StringComparison.OrdinalIgnoreCase ) )
				return comp;
		}
		throw new KeyNotFoundException( $"Component '{typeName}' not found on '{go.Name}'" );
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
