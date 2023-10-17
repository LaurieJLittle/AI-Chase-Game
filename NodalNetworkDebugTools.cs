using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NodalNetworkDebugTools
{
	private static GameObject _redSphere;
	private static GameObject _greenSphere;
	private static GameObject _blueSphere;
	private static GameObject _blackSphere;
	private static GameObject _orangeSphere;
	private static GameObject _orangeSphereTransparent;
	private static GameObject _connector;
	public static List<Vector2> CheckedPoints = new List<Vector2>();

	public static GameObject RedSphere
	{
		get
		{
			if (_redSphere == null)
			{
				_redSphere = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Debug/RedSphere.prefab", typeof(GameObject));
			}

			return _redSphere;
		}
	}

	public static GameObject GreenSphere
	{
		get
		{
			if (_greenSphere == null)
			{
				_greenSphere = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Debug/GreenSphere.prefab", typeof(GameObject));
			}

			return _greenSphere;
		}
	}

	public static GameObject BlueSphere
	{
		get
		{
			if (_blueSphere == null)
			{
				_blueSphere = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Debug/BlueSphere.prefab", typeof(GameObject));
			}

			return _blueSphere;
		}
	}

	public static GameObject BlackSphere
	{
		get
		{
			if (_blackSphere == null)
			{
				_blackSphere = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Debug/BlackSphere.prefab", typeof(GameObject));
			}

			return _blackSphere;
		}
	}

	public static GameObject OrangeSphere
	{
		get
		{
			if (_orangeSphere == null)
			{
				_orangeSphere = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Debug/OrangeSphere.prefab", typeof(GameObject));
			}

			return _orangeSphere;
		}
	}

	public static GameObject OrangeSphereTransparent
	{
		get
		{
			if (_orangeSphereTransparent == null)
			{
				_orangeSphereTransparent = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Debug/OrangeSphereTransparent.prefab", typeof(GameObject));
			}

			return _orangeSphereTransparent;
		}
	}

	public static GameObject Connector
	{
		get
		{
			if (_connector == null)
			{
				_connector = (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Debug/Connector.prefab", typeof(GameObject));
			}

			return _connector;
		}
	}
	
	public static IEnumerator FindSurroundingNodesDebug(Node node, List<Node> Network)
	{
		List<NodePathPair> surroundingNodes = new List<NodePathPair>();

		Queue<PointPathPair> queuedPointPathPairs = new Queue<PointPathPair>();
		var nodeSurroundingPoints = NodalNetworkTools.FindSurroundingPointsOnNavMesh(node.Position);

		foreach (var point in nodeSurroundingPoints)
		{
			queuedPointPathPairs.Enqueue(new PointPathPair(point, 1));
		}

		List<Vector2> checkedPoints = new List<Vector2>();
		checkedPoints.Add(node.Position);

		int loops = 0;
		while (queuedPointPathPairs.Count > 0 && loops < 1000)
		{
			loops += 1;

			PointPathPair pointPathPair = queuedPointPathPairs.Dequeue();
			checkedPoints.Add(pointPathPair.Point);

			yield return IsNodeDebug(pointPathPair.Point);
			if (NodalNetworkTools.IsTrueNode(pointPathPair.Point))
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
			}
			else
			{
				List<Vector2> surroundingPoints = NodalNetworkTools.FindSurroundingPointsOnNavMesh(pointPathPair.Point);
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
							queuedPointPathPairs.Enqueue(new PointPathPair(surroundingPoint, pointPathPair.PathWeight + 1));
						}
					}
				}
			}
		}

		yield break;
		//return surroundingNodes;
	}

	public static IEnumerator IsNodeDebug(Vector2 point)
	{
		if (CheckedPoints.Contains(point))
		{
			yield break;
		}

		CheckedPoints.Add(point);

		GameObject checkIfNodeMarker = GameObject.Instantiate(BlueSphere, new Vector3(point.x, -0.3f, point.y), Quaternion.identity);
		yield return new WaitForSeconds(0.12f);
		List<Vector2> surroundingPointsOnNavMesh = NodalNetworkTools.FindSurroundingPointsOnNavMesh(point);

		List<Vector2> surroundingPoints = new List<Vector2>
		{
			new Vector2 (point.x - 1, point.y),
			new Vector2 (point.x, point.y + 1),
			new Vector2 (point.x + 1, point.y),
			new Vector2 (point.x, point.y - 1)
		};

		List<GameObject> temporaryConnectionDisplaySpheres = new List<GameObject>();
		for (int j = 0; j < surroundingPoints.Count; j ++)
		{
			for (int i = 0; i < surroundingPointsOnNavMesh.Count; i++)
			{
				temporaryConnectionDisplaySpheres.Add(GameObject.Instantiate(OrangeSphereTransparent, new Vector3(surroundingPointsOnNavMesh[i].x, -0.2f, surroundingPointsOnNavMesh[i].y), Quaternion.identity));
				if (surroundingPointsOnNavMesh[i].RoughlyEquals(surroundingPoints[j]))
				{
					GameObject.Instantiate(Connector, new Vector3(point.x, -0.2f, point.y), Quaternion.Euler(0, (j+2) * 90, 0));
				}
			}
		}
		yield return new WaitForSeconds(0.5f);

		if (surroundingPointsOnNavMesh.Count != 2)
		{
			// Node Marker
			GameObject.Instantiate(RedSphere, new Vector3(point.x, -0.3f, point.y), Quaternion.identity);
		}

		
		GameObject.Destroy(checkIfNodeMarker);
		foreach (var sphere in temporaryConnectionDisplaySpheres)
		{
			GameObject.Destroy(sphere);
		}
	}
	

	
	public static IEnumerator MapNodalNetworkDebug(Node firstNode)
	{
		List<Node> allNodes = new List<Node>(); 
		allNodes.Add(firstNode);

		int iterations = 0;
		while (iterations < NodalNetworkTools.MaxExpectedNetworkSize)
		{
			Node unconnectedNode;
			if (NodalNetworkTools.FindUnconnectedNode(allNodes, out unconnectedNode))
			{
				yield return FindSurroundingNodesDebug(unconnectedNode, allNodes);
				unconnectedNode.ConnectedNodes = NodalNetworkTools.FindSurroundingNodes(unconnectedNode, allNodes);
			}
			else
			{
				break;
			}

			iterations += 1;
		}

		if (iterations == NodalNetworkTools.MaxExpectedNetworkSize)
		{
			Debug.LogError("Failed to map all nodes");
		}
	}
	
}
