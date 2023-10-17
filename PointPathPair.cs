using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointPathPair
{
	public PointPathPair(Vector2 point, int pathWeight, PointPathPair previousPoint)
	{
		Point = point;
		PathWeight = pathWeight;
		PreviousPoint = previousPoint;
	}
	public PointPathPair(Vector2 point, int pathWeight)
	{
		Point = point;
		PathWeight = pathWeight;
	}

	public PointPathPair PreviousPoint;
	public Vector2 Point;
	public int PathWeight;
}
