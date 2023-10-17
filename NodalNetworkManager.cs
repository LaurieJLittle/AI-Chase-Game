using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NodeHeuristic
{
	public NodeHeuristic(Node node, float heuristic, int weight, Node previousNode)
	{
		Node = node;
		Heuristic = heuristic;
		Weight = weight;
		PreviousNode = previousNode;
	}

	public Node Node;
	public float Heuristic;
	public int Weight;
	public Node PreviousNode;
}

public class NodalNetworkManager : MonoBehaviour
{
	public NodalNetwork RawNetWork { get; private set; }

	public GameObject Player;

	public Vector3 PlayerPosition
	{
		get { return Player.transform.position; }
	}

	private void Awake()
	{
		Service<NodalNetworkManager>.Init(this);

        NodalNetworkTools.InitNavMeshTriangles();
		RawNetWork = NodalNetworkTools.CreateNodalNetwork();

		// Environment generationDemo
		/*EnvironmentAutoGenerationUtilities.DeterminePointsToSpawnBuildings();
		StartCoroutine(GenerateEnvironmentDemo());*/

		// Nodal Network mapping visual Debug
		// StartCoroutine(MapNetworkVisualDebug());
	}

	private IEnumerator GenerateEnvironmentDemo()
	{
		yield return EnvironmentAutoGenerationUtilities.SpawnBuildingsRoutine();
		yield return EnvironmentAutoGenerationUtilities.SpawnRoadTilesRoutine();
	}

	private IEnumerator MapNetworkVisualDebug()
	{
		Debug.Log("map network debug");
		NodalNetworkDebugTools.CheckedPoints.Clear();
		Vector2 startPoint = NodalNetworkTools.FindStartPoint();
		Instantiate(NodalNetworkDebugTools.BlackSphere, new Vector3(startPoint.x, -0.3f, startPoint.y), Quaternion.identity);
		yield return new WaitForSeconds(1f);

		Debug.LogFormat("start point for node search is {0}, {1}", startPoint.x, startPoint.y);
		Node startNode = NodalNetworkTools.FindStartingNode(startPoint);
		Instantiate(NodalNetworkDebugTools.RedSphere, new Vector3(startPoint.x, -0.3f, startPoint.y), Quaternion.identity);
		yield return NodalNetworkDebugTools.MapNodalNetworkDebug(startNode);
	}
}
