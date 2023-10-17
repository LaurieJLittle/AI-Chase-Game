using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Path
{
	private struct PathComponent
	{
		public string Name;
		public int Weight;
	}
	
    public bool Default;
	public List<Node> Nodes;
	public int TotalWeight;

	public string NodesInfo()
	{
		string nodeInfo = Nodes[0].Name;
		for (int i = 1; i < Nodes.Count; i++)
		{
			nodeInfo += " -> " + Nodes[i].Name + " " + Nodes[i].Position;
		}

		return nodeInfo;
	}

	public float Similarity(Path otherPath)
	{
		List<PathComponent> p1Components = GetComponents();
		List<PathComponent> p2Components = otherPath.GetComponents();

		float sharedWeight = 0;
		foreach (var p1Component in p1Components)
		{
			foreach (var p2Component in p2Components)
			{
				if (p1Component.Name == p2Component.Name)
				{
					sharedWeight += p1Component.Weight;
				}
			}
		}
		float sharedWeightFraction = sharedWeight / Mathf.Min(TotalWeight, otherPath.TotalWeight);

		return sharedWeightFraction;
	}

	private List<PathComponent> GetComponents()
	{
		List<PathComponent> components = new List<PathComponent>();
		for (int i = 1; i < Nodes.Count; i++)
		{
			int weight = int.MaxValue;
			foreach (var node in Nodes[i - 1].ConnectedNodes)
			{
				if (node.NodeName == Nodes[i].Name)
				{
					weight = node.PathWeight;
				}
			}

			PathComponent pathComponent = new PathComponent
			{
				Name = Nodes[i - 1].Name.ToString() + Nodes[i].Name.ToString(),
				Weight = weight
			};

			components.Add(pathComponent);
		}

		return components;
	}
}
