using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PathUtilities
{
	public static Path GetShortestPath(NodalNetwork network, Node endNode, Node startNode, Node ignoredNode = null)
	{
		network.CalculateDistancesToTargetNode(endNode);
		
		NodeHeuristic startNodeH = new NodeHeuristic(startNode, network.DistanceToTargetNodeDict[startNode.Name], 0, null);
		List<NodeHeuristic> nodesHeuristics = new List<NodeHeuristic> { startNodeH };
		int weightOfShortestPathToEnd = int.MaxValue;

		for (int i = 0; i < network.Nodes.Count; i++)
		{
			float lowestPathWeightPlusHeuristic = float.MaxValue;
			int[] lowestPathWeightIndex = { int.MaxValue, int.MaxValue };
			int weightOfNewPath = int.MaxValue;
			bool nodeAlreadyAdded = false;

			//  --------- Checking for shorter paths to mapped nodes ---------
			for (int j = 0; j < nodesHeuristics.Count; j++)
			{
				var nodeH = nodesHeuristics[j];
				for (int k = 0; k < nodeH.Node.ConnectedNodes.Count; k++)
				{
					var potentialNode = nodeH.Node.ConnectedNodes[k];

					if (ignoredNode != null && potentialNode.NodeName == ignoredNode.Name)
					{
						continue; // ignore paths that go through the ignore node
					}

					float weightPlusHeuristic = nodeH.Node.ConnectedNodes[k].PathWeight 
					                    + nodeH.Weight
					                    + network.DistanceToTargetNodeDict[nodeH.Node.ConnectedNodes[k].NodeName];

					if (weightPlusHeuristic < lowestPathWeightPlusHeuristic && weightPlusHeuristic < weightOfShortestPathToEnd)
					{
						bool shorterPathToNodeAlreadyBeenAdded = false;
						nodeAlreadyAdded = false;

						foreach (var nodeHeuristic in nodesHeuristics)
						{
							if (nodeHeuristic.Node.Name == nodesHeuristics[j].Node.ConnectedNodes[k].NodeName)
							{
								nodeAlreadyAdded = true;
								shorterPathToNodeAlreadyBeenAdded = nodeHeuristic.Weight <= nodeH.Weight + nodeH.Node.ConnectedNodes[k].PathWeight;
								if (!shorterPathToNodeAlreadyBeenAdded)
									Debug.LogError(nodeHeuristic.Weight + " < " + (nodeH.Weight + nodeH.Node.ConnectedNodes[k].PathWeight));
							}
						}

						if (!shorterPathToNodeAlreadyBeenAdded)
						{
							lowestPathWeightPlusHeuristic = weightPlusHeuristic;
							weightOfNewPath = nodeH.Weight + nodeH.Node.ConnectedNodes[k].PathWeight;
							lowestPathWeightIndex[0] = j;
							lowestPathWeightIndex[1] = k;
						}
					}
				}
			}
			
			// ------------------------ if shortest path found, return it -----------------------------------
			if (lowestPathWeightIndex[0] == int.MaxValue && lowestPathWeightIndex[1] == int.MaxValue) 
			{
				List<Node> route = new List<Node>();
				NodeHeuristic endNodeH = null;

				foreach (var nodeHeuristic in nodesHeuristics)
				{
					if (nodeHeuristic.Node.Name == endNode.Name)
					{
						endNodeH = nodeHeuristic;
					}
				}

				if (endNodeH == null)
				{
					Debug.LogError("couldn't find route to node");
                    return new Path { Default = true };
				}

				route.Add(endNodeH.Node);
				NodeHeuristic nodeH = endNodeH;
				while (nodeH.PreviousNode != null)
				{
					route.Add(nodeH.PreviousNode);
					nodeH = nodesHeuristics.Find(nH => nH.Node.Name == nodeH.PreviousNode.Name);
				}
				route.Reverse();
				return new Path { Nodes = route, TotalWeight = endNodeH.Weight }; //algorithm complete
			}
			
			// --------------------------------- Add new Node Heuristic ------------------------------------------------
			Node previousNode = nodesHeuristics[lowestPathWeightIndex[0]].Node;
			NodePathPair newNode = nodesHeuristics[lowestPathWeightIndex[0]].Node.ConnectedNodes[lowestPathWeightIndex[1]];
			NodeHeuristic newNodeHeuristic = new NodeHeuristic(network[newNode.NodeName], network.DistanceToTargetNodeDict[newNode.NodeName], weightOfNewPath, previousNode);

			if (newNode.NodeName == endNode.Name)
			{
				weightOfShortestPathToEnd = newNodeHeuristic.Weight;
			}

			if (nodeAlreadyAdded)
			{
				for (int l = nodesHeuristics.Count - 1; l >= 0; l--)
				{
					if (nodesHeuristics[l].Node.Name == newNode.NodeName)
					{
						nodesHeuristics.RemoveAt(l);
						Debug.LogError("REMOVING NODE " + nodesHeuristics[l].Node.Name);
					}
				}
			}

			nodesHeuristics.Add(newNodeHeuristic);
		}

		Debug.LogError("couldn't find route to node");
        return new Path { Default = true };
    }

	/// <summary>
	/// Returns a the shortest Path that is below the max overlap parameter
	/// (where overlap is defined as the fraction of the shorter path that overlaps the longer path)
	/// </summary>
	public static Path GetShortestMostUniquePath(float maxOverlap,  Path ProposedPath, List<Path> pathsToBeDifferentFrom, NodalNetwork nodalNetwork)
	{
		var AlternatePaths =  GetAlternatePaths(ProposedPath, nodalNetwork);
		AlternatePaths.Sort((a, b) => { return a.TotalWeight - b.TotalWeight; });

		foreach (Path alternatePath in AlternatePaths)
		{
			bool overlapsExistingPath = false;
			foreach (Path establishedPath in pathsToBeDifferentFrom)
			{
				if (alternatePath.Similarity(establishedPath) > maxOverlap)
				{
					overlapsExistingPath = true;
				}
			}

			if (!overlapsExistingPath)
			{
				return alternatePath;
			}
		}

		Debug.LogWarning("Couldn't find a sufficiently non overlapping path so just returning shortest");
		return ProposedPath;
	}

	/// <summary>
	/// iterates through each node on a path which we wish to avoid, and returns the shortest path that avoids each node
	/// e.g if trying to avoid path [StartPoint -> A -> B -> C -> EndPoint], 3 paths should be returned, the shortest path
	/// without using A, the shortest path without using B, and the shortest path without using C. 
	/// </summary>
	public static List<Path> GetAlternatePaths(Path shortestPath, NodalNetwork nodalNetwork)
	{
		List<Path> potentialPaths = new List<Path>();
		Node startNode = shortestPath.Nodes[0];
		Node endNode = shortestPath.Nodes[shortestPath.Nodes.Count - 1];

		for (int i = 1; i < shortestPath.Nodes.Count - 1; i++)
		{
			var alternatePath = GetShortestPath(nodalNetwork, endNode, startNode, shortestPath.Nodes[i]);
			if (!alternatePath.Default)
			{
				potentialPaths.Add(alternatePath);
			}
		}

		return potentialPaths;
	}

	public static Path FindPathFromUnitToDestination(IEnumerable<Node> nodes, Vector3 unitPosition3D, Vector3 destination3D)
    {
		Vector2 unitPosition = GridUtilities.SnapToGrid(unitPosition3D);
		Vector2 destination = GridUtilities.SnapToGrid(destination3D);
		List<Node> modifiedNetwork = NetworkWithUnitAndDestination(nodes, unitPosition, destination, out Node unitNode, out Node playerNode);

		return GetShortestPath(new NodalNetwork(modifiedNetwork), playerNode, unitNode);
    }

	public static List<Node> NetworkWithUnitAndDestination(IEnumerable<Node> network, Vector2 unitPosition, Vector2 destination, out Node unitNode, out Node destinationNode)
	{
        IEnumerable<Node> networkWithUnit = AddPositionToNetwork(unitPosition, network, out unitNode, "unit");
		return AddPositionToNetwork(destination, networkWithUnit, out destinationNode, "player");
	}

	private static List<Node> AddPositionToNetwork(Vector2 unitPosition, IEnumerable<Node> networkNodes, out Node newNode, string newNodeName)
    {
        List<Node> establishedNetworkNodes = new List<Node>();
        foreach(Node node in networkNodes)
        {
            establishedNetworkNodes.Add(node);
        }

        // Check if unit position is already on a Node, in which case we won't need to add anything to the network
        int establishedNetworkNodesCount = establishedNetworkNodes.Count;
        Vector2 playerGridPosition = unitPosition;
		for (int i = 0; i < establishedNetworkNodesCount; i++)
		{
			if (establishedNetworkNodes[i].Position == playerGridPosition)
			{
				newNode = establishedNetworkNodes[i];
				return establishedNetworkNodes;
			}
		}

		// Replication Nodal network so we don't edit the original and adding to new node to the new network
		newNode = new Node(playerGridPosition, newNodeName);
        List<Node> nodeList = new List<Node>();
        for (int i = 0; i < establishedNetworkNodesCount; i++)
        {
			nodeList.Add(new Node(establishedNetworkNodes[i])); 
		}
		nodeList.Add(newNode);
		NodalNetwork nodes = new NodalNetwork(nodeList);


		// Adding surrounding Node connections to new Node
        newNode.ConnectedNodes = NodalNetworkTools.FindSurroundingNodes(newNode, nodes.Nodes);
        // Adding new Node connection to surrounding Nodes
        foreach (var nodePathPair in newNode.ConnectedNodes)
		{
			nodes[nodePathPair.NodeName].ConnectedNodes.Add(new NodePathPair(newNode.Name, nodePathPair.PathWeight));
		}

        // Remove redundant node connections.  (if new Node X is between Nodes A and B Remove A from B's connected node list
        // and vice versa as travel between the two will always go via X)
        string pnFirstNeighbourName = nodes[newNode.ConnectedNodes[0].NodeName].Name;
        Node pnFirstNeighbour = nodes[newNode.ConnectedNodes[0].NodeName];
        string pnSecondNeighbourName = nodes[newNode.ConnectedNodes[1].NodeName].Name;
        Node pnSecondNeighbour = nodes[newNode.ConnectedNodes[1].NodeName];
		
		for (int i = pnSecondNeighbour.ConnectedNodes.Count - 1; i >= 0; i--)
        {
	        if (nodes[pnSecondNeighbour.ConnectedNodes[i].NodeName].Name == pnFirstNeighbourName)
			{
				pnSecondNeighbour.ConnectedNodes.RemoveAt(i);
			}
		}

		for (int i = pnFirstNeighbour.ConnectedNodes.Count - 1; i >= 0; i--)
		{
			if (nodes[pnFirstNeighbour.ConnectedNodes[i].NodeName].Name == pnSecondNeighbourName)
			{
				pnFirstNeighbour.ConnectedNodes.RemoveAt(i);
			}
		}

        return nodes.Nodes;
	}
}
