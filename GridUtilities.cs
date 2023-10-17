using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GridUtilities
{
	// this assumes a 1x1 grid, will need editing if we want different grid dimensions
	public static Vector2 SnapToGrid(Vector3 position)
	{
		return new Vector2(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
	}
	
	public static Vector2 To2D(this Vector3 vector)
	{
		return new Vector2(vector.x, vector.z);
	}
}
