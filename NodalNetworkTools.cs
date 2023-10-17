using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System.Net.Security;
using UnityEditor;

public static class NodalNetworkTools
{
	private const string kTriangleDataPath = "Assets/Resources/ScriptableObjects/NavMeshTris.asset";
	private const int kLevelsizeX = 10;
	private const int kLevelsizeY = 10;

	private static List<Node> _allNodes = new List<Node>();
	private static Triangle[] _allTriangles;

	public static int MaxExpectedNetworkSize
	{
		get { return kLevelsizeX * kLevelsizeY; }
	}

	[MenuItem("NavMeshExtraction/Create Nodal Network From NavMesh")]
	public static void CreateNodalNetworkFromNavMeshData()
    {
		_allNodes.Clear();
		InitNavMeshTriangles();
		CreateNodalNetwork();
	}
	
	public static void InitNavMeshTriangles()
	{
		NavMeshTriData triData = AssetDatabase.LoadAssetAtPath<NavMeshTriData>(kTriangleDataPath);
		if (triData != null)
		{
			_allTriangles = triData.AllTriangles;
		}
		else
		{
			InitTriangleListFromNavMeshData();

			var triangleData = ScriptableObject.CreateInstance<NavMeshTriData>();
			triangleData.Init(_allTriangles);
			AssetDatabase.CreateAsset(triangleData, kTriangleDataPath);
		}
	}

	private static void InitTriangleListFromNavMeshData()
	{
		NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        List<Triangle> allTriangles = new List<Triangle>();
		for (int i = 0; i < triangulation.indices.Length; i = i + 3)
		{
			int index1 = triangulation.indices[i];
			int index2 = triangulation.indices[i + 1];
			int index3 = triangulation.indices[i + 2];

			Vector2 corner1 = new Vector2(triangulation.vertices[index1].x, triangulation.vertices[index1].z);
			Vector2 corner2 = new Vector2(triangulation.vertices[index2].x, triangulation.vertices[index2].z);
			Vector2 corner3 = new Vector2(triangulation.vertices[index3].x, triangulation.vertices[index3].z);
			var triangle = new Triangle(corner1, corner2, corner3);

			allTriangles.Add(triangle);
		}

        _allTriangles = allTriangles.ToArray();
	}

	public static NodalNetwork CreateNodalNetwork()
	{
		Vector2 startPoint = FindStartPoint();
		Node firstNode = FindStartingNode(startPoint);
		_allNodes.Clear();
		_allNodes.Add(firstNode);

		int iterations = 0;
		while (iterations < MaxExpectedNetworkSize)
		{
			if (FindUnconnectedNode(_allNodes, out Node unconnectedNode))
			{
				unconnectedNode.ConnectedNodes = FindSurroundingNodes(unconnectedNode, _allNodes);
			}
			else
			{
				break;
			}
			
			iterations += 1;
		}

		if (iterations == MaxExpectedNetworkSize)
		{
			Debug.LogError("Failed to map all nodes");
		}

		bool addedHalfwayNode = AddHalfWayNode();
		while (addedHalfwayNode)
		{
			addedHalfwayNode = AddHalfWayNode();
		}

#if DEBUG_LOG_NETWORKINFO
		foreach (var node in _allNodes)
		{
			Debug.LogFormat("Node {2} at ({0}, {1})", node.Position.x, node.Position.y, node.Name);
		}
#endif
		
		return new NodalNetwork(_allNodes);
	}
	
	public static Vector2 FindStartPoint(MonoBehaviour monoBehaviour = null)
	{
		for (int i = 0; i < kLevelsizeX; i++)
		{
			for (int j = 0; j < kLevelsizeY; j++)
			{
				foreach (var triangle in _allTriangles)
				{
					if (triangle.PointInTriangle(new Vector2(i, j)))
					{
						return new Vector2(i, j);
					}
				}
			}
		}

		return new Vector2(0, 0);
	}

	/// <summary>
	/// HalfWay nodes are not true nodes but we need them to get the correct behaviour out of our nav mesh agents. <br/><br/>
	/// If two nodes have a direct connection but the shortest path between them is via another Node (rather than along the direct path)
	/// then we need to add a HalfWay node on the direct path in order to actually be able to use it.
	/// </summary>
	private static bool AddHalfWayNode()
	{
		var basicNetwork = new NodalNetwork(_allNodes);
		foreach (var node in basicNetwork.Nodes)
		{
			Path shortestConnectingPath = new Path {Default = true};

			for (int j = 0; j < node.ConnectedNodes.Count; j++)
			{
				NodePathPair connectedNodePathPair = node.ConnectedNodes[j];
				Node connectedNode = basicNetwork[connectedNodePathPair.NodeName];

				int k;
				NodePathPair? mirrorConnectedNodePathPair = null;
				for (k = 0; k < connectedNode.ConnectedNodes.Count; k++)
				{
					if (connectedNode.ConnectedNodes[k].NodeName == node.Name)
					{
						mirrorConnectedNodePathPair = connectedNode.ConnectedNodes[k];

						// Remove the the connections between the two nodes and see what the shortest path between them is
						connectedNode.ConnectedNodes.RemoveAt(k);
						node.ConnectedNodes.RemoveAt(j);
						bool eitherNodeDeadEnd = node.ConnectedNodes.Count <= 1 || basicNetwork[connectedNode.Name].ConnectedNodes.Count <= 1; // if either node is a dead end we will never require a halfway node
						shortestConnectingPath = eitherNodeDeadEnd ? new Path {Default = true} : PathUtilities.GetShortestPath(basicNetwork, node, basicNetwork[connectedNode.Name]);

						break;
					}
				}

				if (mirrorConnectedNodePathPair != null && shortestConnectingPath.Default || connectedNodePathPair.PathWeight < shortestConnectingPath.TotalWeight)
				{
					// Re-add the connections between the two nodes
					node.ConnectedNodes.Insert(j, connectedNodePathPair);
					connectedNode.ConnectedNodes.Insert(k, mirrorConnectedNodePathPair.Value);
					
				}
				// if there is a shorter path between the two nodes than the via the connection we need to add a halfway node to the network
				else if (!shortestConnectingPath.Default && connectedNodePathPair.PathWeight >= shortestConnectingPath.TotalWeight)
				{
					FindSurroundingNodes(node, basicNetwork.Nodes, out List<PathDetails> surroundingPaths);
					PathDetails pathToInsertHalfWayNode = null;
					foreach (PathDetails pathDetails in surroundingPaths)
					{
						if (pathDetails.StartNode.Name == node.Name && pathDetails.EndNode.Name == connectedNodePathPair.NodeName)
						{
							pathToInsertHalfWayNode = pathDetails;
						}
					}

					if (pathToInsertHalfWayNode != null)
					{
						Node halfwayNode = new Node(pathToInsertHalfWayNode.GetHalfwayPoint());
						halfwayNode.ConnectedNodes = FindSurroundingNodes(halfwayNode, basicNetwork.Nodes);

						foreach (var connection in halfwayNode.ConnectedNodes)
						{
							if (connection.NodeName == node.Name)
							{
								node.ConnectedNodes.Add(new NodePathPair(halfwayNode.Name, connection.PathWeight));
							}
							else if (connection.NodeName == connectedNodePathPair.NodeName)
							{
								basicNetwork[connectedNodePathPair.NodeName].ConnectedNodes.Add(new NodePathPair(halfwayNode.Name, connection.PathWeight));
							}
						}

						_allNodes.Add(halfwayNode);
						
#if DEBUG_LOG_NETWORKINFO
						Debug.LogFormat("Added halfway node {0} at {1}", halfwayNode.Name, halfwayNode.Position);
#endif
						
						return true;
					}
				}
			}
		}
		return false;
	}

	public static bool FindUnconnectedNode(List<Node> nodes, out Node unconnectedNode)
	{
		unconnectedNode = null;

		foreach (var node in nodes)
		{
			if (node.ConnectedNodes == null || node.ConnectedNodes.Count == 0)
			{
				unconnectedNode = node;
				return true;
			}
		}

		return false;
	}


	public static List<Vector2> FindAllPointsOnNavMesh()
	{
		List<Vector2> points = new List<Vector2>();
		for (int i = 0; i < kLevelsizeX; i++)
		{
			for (int j = 0; j < kLevelsizeY; j++)
			{
				foreach (var triangle in _allTriangles)
				{
					if (triangle.PointInTriangle(new Vector2(i, j)))
					{
						points.Add(new Vector2(i, j));
					}
				}
			}
		}

		return points;
	}

	public static Node FindStartingNode(Vector2 startPoint)
	{
		List<Vector2> potentialNodes = new List<Vector2> { startPoint };

		if (PointOnNavMesh(startPoint))
		{
			int iterations = 0;
			while (potentialNodes.Count > 0 && iterations < kLevelsizeX * kLevelsizeY)
			{
				iterations += 1;
				Vector2 potentialNode = potentialNodes[0];

				if (isNode(potentialNode, out List<Vector2> surroundingPointsOnNavMesh))
				{
					return new Node(potentialNode);
				}
				else
				{
					potentialNodes.Remove(potentialNode);
					foreach (var point in surroundingPointsOnNavMesh)
					{
						potentialNodes.Add(point);
					}
				}
			}

			Debug.LogError("checked all potential nodes, but could not find one");
			return null;
		}
		else
		{
			Debug.LogError("Start point for finding nodes not on Navmesh");
			return null;
		}
	}

	private static bool isNode(Vector2 point, out List<Vector2>surroundingPointsOnNavMesh)
	{
		surroundingPointsOnNavMesh = FindSurroundingPointsOnNavMesh(point);
		return surroundingPointsOnNavMesh.Count != 2;
	}

	public static List<NodePathPair> FindSurroundingNodes(Node node, List<Node> Network)
	{
		return FindSurroundingNodes(node, Network, out List<PathDetails> pathDetails);
	}

	private static List<NodePathPair> FindSurroundingNodes(Node node, List<Node> Network, out List<PathDetails> allPathDetails)
	{
		allPathDetails = new List<PathDetails>();
		List<NodePathPair> surroundingNodes = new List<NodePathPair>();

		Queue<PointPathPair> queuedPointPathPairs = new Queue<PointPathPair>();
		PointPathPair potentialPointPathPair = new PointPathPair(node.Position, 0);
		var nodeSurroundingPoints = FindSurroundingPointsOnNavMesh(node.Position);

		foreach (var point in nodeSurroundingPoints)
		{
			queuedPointPathPairs.Enqueue(new PointPathPair(point, 1, potentialPointPathPair));
		}

		List<Vector2> checkedPoints = new List<Vector2> {node.Position};

		int iterations = 0;
		while (queuedPointPathPairs.Count > 0 && iterations < NodalNetworkTools.MaxExpectedNetworkSize)
		{
			iterations += 1;
			PointPathPair pointPathPair = queuedPointPathPairs.Dequeue();
			checkedPoints.Add(pointPathPair.Point);

			if (IsNode(pointPathPair.Point, Network))
			{
				Node newNode = null;
				foreach (var knownNode in Network)
				{
					if (pointPathPair.Point.RoughlyEquals(knownNode.Position))
					{
						newNode = knownNode;
					}
				}

				if (newNode == null)
				{
					newNode = new Node(pointPathPair.Point);
					Network.Add(newNode);
				}
				surroundingNodes.Add(new NodePathPair(newNode.Name, pointPathPair.PathWeight));

				PointPathPair currentPoint = pointPathPair;
				List<PointPathPair> pointPathPairs = new List<PointPathPair> { currentPoint };

				PointPathPair previousPointOnPath = pointPathPairs[pointPathPairs.Count - 1].PreviousPoint;
				while (previousPointOnPath != null)
				{
						pointPathPairs.Add(previousPointOnPath);
						previousPointOnPath = pointPathPairs[pointPathPairs.Count - 1].PreviousPoint;
				}

				allPathDetails.Add(new PathDetails(pointPathPairs, node, newNode));
			}
			else
			{
				List<Vector2> surroundingPoints = FindSurroundingPointsOnNavMesh(pointPathPair.Point);
				foreach (var surroundingPoint in surroundingPoints)
				{
					for (int i = 0; i < checkedPoints.Count; i++)
					{
						if (checkedPoints[i].RoughlyEquals(surroundingPoint))
						{
							break;
						}

						if (i == checkedPoints.Count - 1)
						{
							queuedPointPathPairs.Enqueue(new PointPathPair(surroundingPoint, pointPathPair.PathWeight + 1, pointPathPair));
						}
					}
				}
			}
		}

		if (iterations == NodalNetworkTools.MaxExpectedNetworkSize)
		{
			Debug.LogError("loop broken - force breaking out of loop");
		}
		
		return surroundingNodes;
	}
	
	public static List<Vector2> FindSurroundingPointsOnNavMesh(Vector2 point)
	{
		List<Vector2> surroundingPoints = new List<Vector2>
		{
			new Vector2 (point.x - 1, point.y),
			new Vector2 (point.x, point.y + 1),
			new Vector2 (point.x + 1, point.y),
			new Vector2 (point.x, point.y - 1)
		};

		for (int i = surroundingPoints.Count -1; i >= 0; i--)
		{
			if (!PointOnNavMesh(surroundingPoints[i]))
			{
				surroundingPoints.Remove(surroundingPoints[i]);
			}
		}

		return surroundingPoints;
	}
	
	public static List<Vector2> FindSurroundingPointsOffNavMesh(Vector2 point)
	{
		List<Vector2> surroundingPoints = new List<Vector2>
		{
			new Vector2 (point.x - 1, point.y),
			new Vector2 (point.x, point.y + 1),
			new Vector2 (point.x + 1, point.y),
			new Vector2 (point.x, point.y - 1)
		};

		for (int i = surroundingPoints.Count; i >= 0; i--)
		{
			if (PointOnNavMesh(surroundingPoints[i]))
			{
				surroundingPoints.Remove(surroundingPoints[i]);
			}
		}

		return surroundingPoints;
	}

	private static bool PointOnNavMesh(Vector2 point)
	{
		foreach (var triangle in _allTriangles)
		{
			if (triangle.PointInTriangle(point))
			{
				return true;
			}
		}

		return false;
	}


	public static bool IsTrueNode(Vector2 point)
	{
		var surroundingPointsOnNavMesh = FindSurroundingPointsOnNavMesh(point);

		if (surroundingPointsOnNavMesh.Count != 2)
		{
			return true;
		}
		
		return false;
	}

	private static bool IsNode(Vector2 point, List<Node> RegisteredNodes)
	{
		var surroundingPointsOnNavMesh = FindSurroundingPointsOnNavMesh(point);

		if (surroundingPointsOnNavMesh.Count != 2)
		{
			return true;
		}
		
		foreach (var node in RegisteredNodes) // catches halfway nodes
		{
			if (node.Position.RoughlyEquals(point))
			{
				return true;
			}
		}

		return false;
	}
}