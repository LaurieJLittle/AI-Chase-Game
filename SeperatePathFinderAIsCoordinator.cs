using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;
using Unity.Jobs;
using Unity.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.VersionControl;

public class SeperatePathFinderAIsCoordinator : MonoBehaviour
{
	public enum Behaviour
	{
		Patrol,
		Chase
	}
	
	private const int kMaxExpectedNodesInPath = 10;
	
	private Behaviour _activeBehaviour = Behaviour.Chase;
	private NodalNetworkManager _networkManager;
    private SeparatePathCalculationJob _separatePathCalculationJob;
    private JobHandle _calculationJobHandle;
    private bool _waitingOnJob;
    
    private static List<SeparatePathFinderAI> _seperatePathFinders = new List<SeparatePathFinderAI>();
	private static Dictionary<string, Node> _pathFinderPreviousPatrolNodeDict = new Dictionary<string, Node>();
	private static char _pathFinderLabel = 'A';

    public void Start()
	{
		_networkManager = Service<NodalNetworkManager>.Find();
		
		if (_activeBehaviour == Behaviour.Chase)
		{
			_networkManager.Player.GetComponent<Player>().OnPositionChanged += AssignChasePaths;
			AssignChasePaths();
		}
	}
	
	public void AddPathFinder(SeparatePathFinderAI pathFinderAI)
	{
		_seperatePathFinders.Add(pathFinderAI);
		pathFinderAI.Name = "unit" + _pathFinderLabel;
		_pathFinderLabel = (char)(_pathFinderLabel + 1);
	}

	public void SetActiveBehaviour(Behaviour newBehaviour)
	{
		if (_activeBehaviour != newBehaviour)
		{
			_activeBehaviour = newBehaviour;
			if (_activeBehaviour == Behaviour.Chase)
			{
				AssignChasePaths();
			}
		}
	}
	
    private void Update()
	{
		if (_activeBehaviour == Behaviour.Patrol)
		{
			CheckAssignPatrolPaths();
		}
		else if (_activeBehaviour == Behaviour.Chase && _waitingOnJob && _calculationJobHandle.IsCompleted)
        {
            _waitingOnJob = false;
            _calculationJobHandle.Complete();
            _separatePathCalculationJob.NodeDetailsBlittable.Dispose();
            _separatePathCalculationJob.PathFinderPositionsBlittable.Dispose();

            string pathsAsString = string.Empty;
            foreach (var number in _separatePathCalculationJob.PathsBlittable)
            {
                pathsAsString += number + ", ";
            }

            List<Path> establishedPaths = new List<Path>();
            int pathDataLength = kMaxExpectedNodesInPath * SeparatePathCalculationJob.KNodeDataLength;
            for (int i = 0; i < _separatePathCalculationJob.PathsBlittable.Length; i += pathDataLength)
            {

                Path path = new Path();
                path.Nodes = new List<Node>();
                path.Default = _separatePathCalculationJob.PathsBlittable[i] == 0 ? false : true;
                path.TotalWeight = _separatePathCalculationJob.PathsBlittable[i + 1];
                for (int k = 2 + i; k < int.MaxValue; k += 11)
                {
                    if (_separatePathCalculationJob.PathsBlittable[k] == -2)
                    {
                        break;
                    }
                    
                    // Note: haven't added connected node data here as we don't use it, but might be worth adding if we end up doing something else with the paths
                    Node node = new Node(
                        new Vector2(_separatePathCalculationJob.PathsBlittable[k + 1], _separatePathCalculationJob.PathsBlittable[k + 2]),
                        ((char)_separatePathCalculationJob.PathsBlittable[k]).ToString());
                    
                    path.Nodes.Add(node);
                }

                establishedPaths.Add(path);
            }

            List<int> assignedPathsIndexes = new List<int>();
            for (int i = establishedPaths.Count - 1; i >= 0; i--)
            {
                for (int j = _seperatePathFinders.Count - 1; j >= 0; j--)
                {
                    if (GridUtilities.SnapToGrid(_seperatePathFinders[j].transform.position) == establishedPaths[i].Nodes[0].Position && !assignedPathsIndexes.Contains(j))
                    {
                        assignedPathsIndexes.Add(j);
                        _seperatePathFinders[j].SetPath(establishedPaths[i], true);
                        break;
                    }
                }
            }

            _separatePathCalculationJob.PathsBlittable.Dispose();
        }
    }

    // Chase paths
	public void AssignChasePaths()
	{
		if (_activeBehaviour != Behaviour.Chase)
		{
			return;
		}

        if (_waitingOnJob)
        {
            Debug.LogWarning("Can't assign new chase paths, still waiting on previous job");
            return;
        }

        _separatePathCalculationJob = new SeparatePathCalculationJob(_networkManager, _seperatePathFinders, 10);
        _waitingOnJob = true;
        _calculationJobHandle = _separatePathCalculationJob.Schedule();
    }

	// Patrol paths
	public void CheckAssignPatrolPaths()
	{
		foreach (var pathFinder in _seperatePathFinders)
		{
			//Only Assign a new patrol path if there isn't one currently assigned
			if ((pathFinder.AssignedPath.Nodes != null
				&& pathFinder.AssignedPath.Nodes.Count > 0
				&& pathFinder.PathAssigned))
			{
				continue;
			}

			// Set Unit position to be end of previous path for calculation purposes in case the unit hasn't quite arrived there yet
			Vector2 unitPosition = pathFinder.PreviousDestination.HasValue ? pathFinder.PreviousDestination.Value.To2D() : GridUtilities.SnapToGrid(pathFinder.transform.position);
			
			bool unitIsOnNode = false;
			Node playerNode = null;
			foreach (var node in _networkManager.RawNetWork.Nodes)
			{
				unitIsOnNode = node.Position.RoughlyEquals(unitPosition);
				if (unitIsOnNode)
				{
					playerNode = node;
					break;
				}
			}

			List<NodePathPair> possibleDestinations;

			if (unitIsOnNode)
			{
				string nodeList = string.Empty;
				foreach (var node in playerNode.ConnectedNodes)
				{
					nodeList += _networkManager.RawNetWork[node.NodeName].Position;
				}
				possibleDestinations = new List<NodePathPair>(playerNode.ConnectedNodes);
				
				PathCalculationLog("Player Node is " + playerNode.Name + ", " + playerNode.Position);
				PathCalculationLog("Connected nodes = " + playerNode.ConnectedNodeInfo( _networkManager.RawNetWork));
			}
			else
			{
				// if not on node
				playerNode = new Node(unitPosition);
				possibleDestinations = NodalNetworkTools.FindSurroundingNodes(playerNode, _networkManager.RawNetWork.Nodes);

				string nodeList = string.Empty;
				foreach (var node in possibleDestinations)
				{
					nodeList += _networkManager.RawNetWork[node.NodeName].Position;
				}
				
				PathCalculationLog("NOT on node! Position = " + playerNode.Position + " connected nodes are " + nodeList);
			}

			Node previousNode = null;
			// remove previous node to stop U-turns
			if (possibleDestinations.Count > 1 && _pathFinderPreviousPatrolNodeDict.TryGetValue(pathFinder.Name, out previousNode))
			{
				for (int i = possibleDestinations.Count - 1; i >= 0; i--)
				{
					if (possibleDestinations[i].NodeName == previousNode.Name)
					{
						PathCalculationLog("------- Removing previous Node " + previousNode.Position);
						possibleDestinations.RemoveAt(i);
						break;
					}
				}
			}
			
			int randomIndex = Random.Range(0, possibleDestinations.Count);
			Node patrolDestination = _networkManager.RawNetWork[possibleDestinations[randomIndex].NodeName];

			if (unitIsOnNode)
			{
				if (!_pathFinderPreviousPatrolNodeDict.ContainsKey(pathFinder.Name))
				{
					_pathFinderPreviousPatrolNodeDict.Add(pathFinder.Name, playerNode);
				}
				else
				{
					_pathFinderPreviousPatrolNodeDict[pathFinder.Name] = playerNode;
				}
			}

			var path = new Path
			{
				Nodes = new List<Node> { patrolDestination }
			};
			pathFinder.SetPath(path);
		}
	}

	private void PathCalculationLog(string message)
	{
		#if LOG_PATH_FINDING
			Debug.Log(message);
		#endif
	}
}


public struct SeparatePathCalculationJob : IJob
{
    public SeparatePathCalculationJob(NodalNetworkManager networkManager, List<SeparatePathFinderAI> pathFinders, int maxExpectedNodesInPath = 10)
    {
	    _playerPosition = networkManager.PlayerPosition;
	    List<Node> nodes = networkManager.RawNetWork.Nodes;
        NodeDetailsBlittable = nodes.ToBlittable();
        PathFinderPositionsBlittable = pathFinders.ToBlittable();
        _blittablePathLengthAllocation = maxExpectedNodesInPath * KNodeDataLength; // each node has 11 pieces of data
        PathsBlittable = new NativeArray<int>(pathFinders.Count * _blittablePathLengthAllocation, Allocator.Persistent);
    }

    public const int KNodeDataLength = 11;
    
    private const int kPathCalcIterationLimit = 100;
    private const float kMaxFirstPassSimilarityThreshold = 0.4f;
    private const float kMaxSecondPassSimilarityThreshold = 0.7f;

    private int _blittablePathLengthAllocation;
    private Vector3 _playerPosition;
    
    public NativeArray<float> PathFinderPositionsBlittable;
    public NativeArray<int>  NodeDetailsBlittable;
    public NativeArray<int> PathsBlittable;
    
    public void Execute()
    {
        Node[] nodes = NodeDetailsBlittable.ToNodes();
        Vector3[] pathFindersPositions = PathFinderPositionsBlittable.ToPositions();

        CalculatePaths(pathFindersPositions, nodes);
    }

    public void CalculatePaths(Vector3[] pathFindersPositions, Node[] _nodes)
    {
	    FindShortestPathForClosestUnit(pathFindersPositions, _nodes, 
		    out Path shortestPathForClosestUnit, out List<Path> directPaths, out List<Path> establishedPaths);

        directPaths.Sort((a, b) => { return a.TotalWeight - b.TotalWeight; });
        int iterations = 0;
        while (establishedPaths.Count < pathFindersPositions.Length && iterations < kPathCalcIterationLimit)
        {
            iterations += 1;
            bool pathAdded = TryEstablishDirectPaths(ref directPaths, ref establishedPaths);

            if (pathAdded)
            {
                continue;
            }

            for (int i = 0; i < directPaths.Count; i++)
            {
                if (directPaths[i].Similarity(shortestPathForClosestUnit) > kMaxSecondPassSimilarityThreshold)
                {
                    //paths are too similar so find a longer route
                    var networkWithUnitAndPlayer = PathUtilities.NetworkWithUnitAndDestination(
	                    _nodes, directPaths[i].Nodes[0].Position, directPaths[i].Nodes[directPaths[i].Nodes.Count - 1].Position, 
	                    out Node unitNode, out Node destNode);
                    
                    establishedPaths.Add(PathUtilities.GetShortestMostUniquePath(kMaxSecondPassSimilarityThreshold, directPaths[i], establishedPaths, new NodalNetwork(networkWithUnitAndPlayer)));
                    directPaths.RemoveAt(i);
                    break;
                }
                else
                {
	                establishedPaths.Add(directPaths[i]);
	                directPaths.RemoveAt(i);
                }
            }
        }

        for (int g = 0; g < establishedPaths.Count * _blittablePathLengthAllocation; g = g + _blittablePathLengthAllocation)
        {
            PathsBlittable[g] = establishedPaths[g / _blittablePathLengthAllocation].Default.CompareTo(false);
            PathsBlittable[g + 1] = establishedPaths[g / _blittablePathLengthAllocation].TotalWeight;

            int k = g + 2;
            int lastAssignedIndex = int.MinValue;
            List<Node> nodes = establishedPaths[g / _blittablePathLengthAllocation].Nodes;
            for (int h = 0; h < nodes.Count * 11; h += 11)
            {
                int i = h / 11;
                PathsBlittable[k + h] = (int)nodes[i].Name.ToCharArray()[0];
                PathsBlittable[k + h + 1] = (int)nodes[i].Position.x;
                PathsBlittable[k + h + 2] = (int)nodes[i].Position.y;
                int m = h + 3;
                for (int j = 0; j < 4; j++)
                {
                    if (j < nodes[i].ConnectedNodes.Count)
                    {
                        PathsBlittable[k + m + (j * 2)] = (int)nodes[i].ConnectedNodes[j].NodeName.ToCharArray()[0];
                        PathsBlittable[k + m + (j * 2) + 1] = (int)nodes[i].ConnectedNodes[j].PathWeight;
                    }
                    else
                    {
                        PathsBlittable[k + m + (j * 2)] = -1;
                        PathsBlittable[k + m + (j * 2) + 1] = -1;
                    }
                    lastAssignedIndex = k + m + (j * 2) + 1;
                }
            }

            for (int i = lastAssignedIndex + 1; i < g + _blittablePathLengthAllocation; i++)
            {
                PathsBlittable[i] = -2;
            }
        }
    }

    private void FindShortestPathForClosestUnit(Vector3[] pathFindersPositions, Node[] nodes,
	    out Path shortestPathForClosestUnit, out List<Path> paths, out List<Path> establishedPaths)
    {
	    shortestPathForClosestUnit = new Path();
	    paths = new List<Path>();
	    establishedPaths = new List<Path>();
	    
	    // ------------------------------------------------------------------------------
	    //  ----- Finding first path (shortest path for the closest unit to player) -----
        
	    // get each Units shortest path to the destination
	    for (int i = 0; i < pathFindersPositions.Length; i++)
	    {
		    paths.Add(PathUtilities.FindPathFromUnitToDestination(nodes, pathFindersPositions[i], _playerPosition));
	    }

	    int shortestPathLength = int.MaxValue;
	    int shortestPathIndex = int.MaxValue;
	    for (int i = 0; i < paths.Count; i++)
	    {
		    if (paths[i].TotalWeight < shortestPathLength)
		    {
			    shortestPathForClosestUnit = paths[i];
			    shortestPathLength = paths[i].TotalWeight;
			    shortestPathIndex = i;
		    }
	    }

	    establishedPaths.Add(shortestPathForClosestUnit);
	    paths.RemoveAt(shortestPathIndex);
    }

    private bool TryEstablishDirectPaths(ref List<Path> directPaths, ref List<Path> establishedPaths)
    {
	    // Try and find a path for remaining PathFinders that is under the similarity threshold when compared to established paths
	    for (int i = 0; i < directPaths.Count; i++)
	    {
		    bool pathTooSimilarToEstablishedPaths = false;
		    foreach (var establishedPath in establishedPaths)
		    {
			    if (directPaths[i].Similarity(establishedPath) > kMaxFirstPassSimilarityThreshold)
			    {
				    pathTooSimilarToEstablishedPaths = true;
			    }
		    }

		    if (pathTooSimilarToEstablishedPaths)
		    {
			    continue;
		    }

		    establishedPaths.Add(directPaths[i]);
		    directPaths.RemoveAt(i);
		    return true;
	    }

	    return false;
    }
}