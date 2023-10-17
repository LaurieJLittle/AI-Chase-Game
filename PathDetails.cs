using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathDetails
{
    public PathDetails(List<PointPathPair> point2PointPaths, Node startNode, Node endNode)
    {
        Point2PointPaths = point2PointPaths;
        StartNode = startNode;
        EndNode = endNode;
    }

    public List<PointPathPair> Point2PointPaths;
    public Node StartNode;
    public Node EndNode;

    public Vector2 GetHalfwayPoint()
    {
        if (Point2PointPaths.Count != 0)
        {
            int halfwayIndex = Point2PointPaths.Count / 2;
            return Point2PointPaths[halfwayIndex].Point;
        }
        else
        {
            Debug.LogError("Trying to find halfway point of a zero length path");
            return new Vector2(0, 0);
        }
    }
}