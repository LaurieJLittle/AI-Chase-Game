using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class SeparatePathFinderAI : MonoBehaviour
{
	private const string kPlayerTag = "Player";
	private const float kStationaryRotationThreshold = 60f;
	private const float kStationaryRotationRate = 3f;
	
	[SerializeField]
	private NavMeshAgent _agent;
	[SerializeField]
	private SeperatePathFinderAIsCoordinator _coordinatorPrefab;
	
	private static SeperatePathFinderAIsCoordinator _coordinator;

	private Coroutine _rotateThenTravelCoroutine;
	private Action OnDestinationReached = null;

	
	public string Name;
	public Path AssignedPath { get; private set; } = new Path();

	public bool PathAssigned;
	public Vector3? PreviousDestination { get; private set; }
	public Vector3 Destination { get; private set; }

	private void Awake()
	{
		if (_coordinator == null)
		{
			_coordinator = Instantiate(_coordinatorPrefab);
		}

		_coordinator.AddPathFinder(this);

		OnDestinationReached += SetNextDestination;
	}

	private void Update()
	{
		if ((Destination - transform.position).sqrMagnitude < 1f && AssignedPath.Nodes.Count > 0)
		{
			if (OnDestinationReached != null && PathAssigned)
			{
				OnDestinationReached();
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == kPlayerTag)
		{
			if (true) // TODO: Add proper LOS check here
			{
				_coordinator.SetActiveBehaviour(SeperatePathFinderAIsCoordinator.Behaviour.Chase);
			}
		}
	}

	public void SetPath(Path path, bool dudFirstNode = false)
	{
		if (_rotateThenTravelCoroutine != null)
		{
			StopCoroutine(_rotateThenTravelCoroutine);
			_rotateThenTravelCoroutine = null;
		}

		if (dudFirstNode)
		{
			path.Nodes.RemoveAt(0);
		}

		if (path.Nodes.Count == 0)
		{
			Debug.LogWarning("Trying to set empty path");
			return;
		}

		PathAssigned = true;
		AssignedPath = path;
		Destination = new Vector3(path.Nodes[0].Position.x, 0, path.Nodes[0].Position.y);

        Vector3 directionOfTravel = Destination - transform.position;
        Vector2 directionOfTravel2D = directionOfTravel.To2D();
        Vector2 facingDirection2D = transform.forward.To2D();

        float angle = Vector2.Angle(facingDirection2D, directionOfTravel2D);

        if (angle > kStationaryRotationThreshold)
        {
            _agent.updateRotation = false;
			_rotateThenTravelCoroutine = StartCoroutine(RotateToDestinationThenTravel(Destination));
        }
        else
        {
            _agent.SetDestination(Destination);
        }
	}

    private IEnumerator RotateToDestinationThenTravel(Vector3 Destination)
    {
        float angle = float.MaxValue;
        while (angle > kStationaryRotationThreshold)
        {
            Vector3 directionOfTravel = Destination - transform.position;
            Vector2 directionOfTravel2D = new Vector2(directionOfTravel.x, directionOfTravel.z);
            Vector2 FacingDirection2D = new Vector2(transform.forward.x, transform.forward.z);

            angle = Vector2.Angle(FacingDirection2D, directionOfTravel2D);
            
            Vector3 newDirection = Vector3.RotateTowards(transform.forward, directionOfTravel, Time.deltaTime * kStationaryRotationRate, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDirection);
            yield return null;
        }

        _agent.updateRotation = true;
        _agent.SetDestination(Destination);
    }

	private void SetNextDestination()
	{
		AssignedPath.Nodes.RemoveAt(0);

		PreviousDestination = Destination;
		if (!AssignedPath.Default && AssignedPath.Nodes.Count > 0)
		{
			Destination = new Vector3(AssignedPath.Nodes[0].Position.x, 0, AssignedPath.Nodes[0].Position.y);
			_agent.SetDestination(Destination);
		}
		else
		{
			Destination = Vector3.negativeInfinity;
			PathAssigned = false;
		}
	}
}
