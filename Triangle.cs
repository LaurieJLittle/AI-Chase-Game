using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Triangle
{
	public Vector2[] Vertices;

	public Vector2 Center
	{
		get
		{
			return (Vertices[0] + Vertices[1] + Vertices[2]) / 3f;
		}
	}

	private float Area
	{
		get
		{
			Vector2 p0 = Vertices[0];
			Vector2 p1 = Vertices[1];
			Vector2 p2 = Vertices[2];

			return (float)0.5 * (-p1.y * p2.x + p0.y * (-p1.x + p2.x) + p0.x * (p1.y - p2.y) + p1.x * p2.y);
		}
	}

	public bool PointInTriangle(Vector2 point)
	{

		Vector2 p0 = Vertices[0];
		Vector2 p1 = Vertices[1];
		Vector2 p2 = Vertices[2];

		float s = 1f / (2f * Area) * (p0.y * p2.x - p0.x * p2.y + (p2.y - p0.y) * point.x + (p0.x - p2.x) * point.y);
		float t = 1f / (2f * Area) * (p0.x * p1.y - p0.y * p1.x + (p0.y - p1.y) * point.x + (p1.x - p0.x) * point.y);

		if (s >= -0.01f && t >= -0.01f && 1 - s - t >= -0.01f)
		{
			return true;
		}
		
		return false;
	}

	public Triangle(Vector2 corner1, Vector2 corner2, Vector2 corner3)
	{
        Vertices = new Vector2[3];
        Vertices[0] = corner1;
		Vertices[1] = corner2;
		Vertices[2] = corner3;
	}

	bool isTriangleConnected(Triangle otherTriangle)
	{
		int vertsInCommon = 0;

		foreach (var vert in Vertices)
		{
			foreach (var otherVert in otherTriangle.Vertices)
			{
				if (RoughlyEquals(vert, otherVert))
				{
					vertsInCommon += 1;
				}
			}
		}

		if (vertsInCommon == 2)
		{
			return true;
		}

		return false;
	}

	bool RoughlyEquals(Vector2 v1, Vector2 v2)
	{
		return RoughlyEquals(v1.x, v2.x) && RoughlyEquals(v1.y, v2.y);
	}

	bool RoughlyEquals(float f1, float f2)
	{
		if (Mathf.Abs(f1 - f2) < 0.01f)
		{
			return true;
		}

		return false;
	}

	public string PrintVertices()
	{
		return Vertices[0] + " " + Vertices[1] + " " + Vertices[2];
	}

    public float[] ToBlittableType()
    {
        return new float[] { Vertices[0].x, Vertices[0].y, Vertices[1].x, Vertices[1].y, Vertices[2].x, Vertices[2].y };
    }
}
