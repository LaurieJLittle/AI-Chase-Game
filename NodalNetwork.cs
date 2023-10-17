using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodalNetwork
{
	public NodalNetwork(List<Node> nodes)
	{
		Nodes = nodes;
	}

	public List<Node> Nodes { get; private set; }
	public Dictionary<string, float> DistanceToTargetNodeDict;

	public void CalculateDistancesToTargetNode(Node targetNode)
	{
		DistanceToTargetNodeDict = new Dictionary<string, float>();
		foreach (var node in Nodes)
		{
			DistanceToTargetNodeDict.Add(node.Name, node.DistanceTo(targetNode));
		}
	}

	public Node this[string nodeName]
	{
		get
		{
			foreach (var node in Nodes)
			{
				if (node.Name == nodeName)
				{
					return node;
				}
			}

			return null;
		}
	}
}
