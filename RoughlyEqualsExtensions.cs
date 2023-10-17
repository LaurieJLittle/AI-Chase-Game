using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoughlyEqualsExtensions
{
	private static bool RoughlyEquals(this float f1, float f2)
	{
		if (Mathf.Abs(f1 - f2) < 0.01f)
		{
			return true;
		}

		return false;
	}

	public static bool RoughlyEquals(this Vector2 v1, Vector2 v2)
	{
		if (v1.x.RoughlyEquals(v2.x) && v1.y.RoughlyEquals(v2.y))
		{
			return true;
		}

		return false;
	}

	public static bool NextTo(this Vector2 v1, Vector2 v2)
	{
		if (v2.RoughlyEquals(v1 + new Vector2(1, 0)))
		{
			return true;
		}
		if (v2.RoughlyEquals(v1 + new Vector2(0, 1)))
		{
			return true;
		}
		if (v2.RoughlyEquals(v1 - new Vector2(1, 0)))
		{
			return true;
		}
		if (v2.RoughlyEquals(v1 - new Vector2(0, 1)))
		{
			return true;
		}

		return false;
	}

	public static bool DirectlyAbove(this Vector2 v1, Vector2 v2)
	{
		if (v2.RoughlyEquals(v1 + new Vector2(0, 1)))
		{
			return true;
		}
		return false;
	}

	public static bool DirectlyBelow(this Vector2 v1, Vector2 v2)
	{
		if (v2.RoughlyEquals(v1 - new Vector2(0, 1)))
		{
			return true;
		}
		return false;
	}

	public static bool DirectlyAdjacentLeft(this Vector2 v1, Vector2 v2)
	{
		if (v2.RoughlyEquals(v1 - new Vector2(1, 0)))
		{
			return true;
		}
		return false;
	}

	public static bool DirectlyAdjacentRight(this Vector2 v1, Vector2 v2)
	{
		if (v2.RoughlyEquals(v1 + new Vector2(1, 0)))
		{
			return true;
		}
		return false;
	}
}
