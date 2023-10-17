using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NodePathPair
{
	public NodePathPair(string nodeName, int pathWeight)
	{
		NodeName = nodeName;
		PathWeight = pathWeight;
	}

	public string NodeName;
	public int PathWeight;
}

public class Node
{
	private static int _nodeNumber = 65; // corresponds to char A
	
	public string Name;
	public List<NodePathPair> ConnectedNodes;
	public Vector2 Position;

	public Node(Vector2 position, string nameOverride = null)
	{
		ConnectedNodes = new List<NodePathPair>();
		Position = position;
		Name = nameOverride != null ? nameOverride : ((char) _nodeNumber).ToString();
		if (nameOverride == null)
		{
			_nodeNumber += 1;
		}
	}

	public Node (Node node)
	{
		Name = node.Name;
		ConnectedNodes = new List<NodePathPair>();
		foreach (var connectedNode in node.ConnectedNodes)
		{
			ConnectedNodes.Add(new NodePathPair(connectedNode.NodeName, connectedNode.PathWeight));
		}
		Position = node.Position;
	}
	
	public string ConnectedNodeInfo(NodalNetwork network)
	{
		if (ConnectedNodes == null)
		{
			return String.Empty;
		}

		string nodeInfo = String.Empty;
		for (int i = 0; i < ConnectedNodes.Count; i++)
		{
			nodeInfo += ConnectedNodes[i].NodeName + " " + network[ConnectedNodes[i].NodeName].Position + ", ";
		}

		return nodeInfo;
	}
	
	public float DistanceTo(Node otherNode)
	{
		return (Position - otherNode.Position).magnitude;
	}
}
