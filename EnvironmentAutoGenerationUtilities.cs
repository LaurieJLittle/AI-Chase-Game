using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EnvironmentAutoGenerationUtilities
{
	private const float kLevelHeight = -0.5f;
	private static List<Vector2> _spawnPoints;
	
	[MenuItem("NavMeshExtraction/Generate Environment")]
	public static void GenerateEnvironment()
	{
		DeterminePointsToSpawnBuildings();
		SpawnBuildings();
		SpawnRoadTiles();
	}

	public static void DeterminePointsToSpawnBuildings()
	{
		var navigablePoints = NodalNetworkTools.FindAllPointsOnNavMesh();
		_spawnPoints = new List<Vector2>();

		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;

		foreach (var point in navigablePoints)
		{
			minX = Mathf.Min(minX, point.x);
			minY = Mathf.Min(minY, point.y);
			maxX = Mathf.Max(maxX, point.x);
			maxY = Mathf.Max(maxY, point.y);
			_spawnPoints.AddRange(NodalNetworkTools.FindSurroundingPointsOffNavMesh(point));
		}

		_spawnPoints = _spawnPoints.Distinct().ToList();
		for (int i = _spawnPoints.Count - 1; i >= 0; i--)
		{
			var point = _spawnPoints[i];
			if (minX > point.x || minY > point.y || maxX < point.x || maxY < point.y)
			{
				_spawnPoints.RemoveAt(i);
			}
		}
	}

	private static void SpawnRoadTiles()
	{
		var buildingReferences = Resources.Load<EnvironmentReferenceHolder>("ScriptableObjects/BuildingRefs");
		List<Vector2> navigablePoints = NodalNetworkTools.FindAllPointsOnNavMesh();
		navigablePoints = navigablePoints.Distinct().ToList();
		foreach(var point in navigablePoints)
		{
			SpawnRoadTile(buildingReferences, point);
		}
	}

	public static IEnumerator SpawnRoadTilesRoutine() // real time demo version
	{
		var buildingReferences = Resources.Load<EnvironmentReferenceHolder>("ScriptableObjects/BuildingRefs");
		List<Vector2> navigablePoints = NodalNetworkTools.FindAllPointsOnNavMesh();
		navigablePoints = navigablePoints.Distinct().ToList();
		foreach(var point in navigablePoints)
		{
			SpawnRoadTile(buildingReferences, point);
			yield return new WaitForSeconds(0.1f);
		}
	}

	private static void SpawnRoadTile(EnvironmentReferenceHolder buildrefs, Vector2 point)
	{
		List<Vector2> surroundingPointsOnNavMesh = NodalNetworkTools.FindSurroundingPointsOnNavMesh(point);
		GameObject roadToInstantiate = buildrefs.RoadDeadEnd;
		Vector3 rotation = new Vector3(0, 0, 0);

		bool above = false;
		bool below = false;
		bool left = false;
		bool right = false;

		switch (surroundingPointsOnNavMesh.Count)
		{
			case 1:
				roadToInstantiate = buildrefs.RoadDeadEnd;
				if (point.DirectlyAbove(surroundingPointsOnNavMesh[0]))
				{
					rotation += new Vector3(0, 180, 0);
				}
				else if (point.DirectlyAdjacentRight(surroundingPointsOnNavMesh[0]))
				{
					rotation += new Vector3(0, -90, 0);
				}
				else if(point.DirectlyBelow(surroundingPointsOnNavMesh[0]))
				{
					rotation += new Vector3(0, 0, 0);
				}
				else if (point.DirectlyAdjacentLeft(surroundingPointsOnNavMesh[0]))
				{
					rotation += new Vector3(0, 90, 0);
				}

				break;
			case 2:

				foreach (var surroundingPoint in surroundingPointsOnNavMesh)
				{
					above |= surroundingPoint.DirectlyAbove(point);
					right |= surroundingPoint.DirectlyAdjacentRight(point);
					below |= surroundingPoint.DirectlyBelow(point);
					left |= surroundingPoint.DirectlyAdjacentLeft(point);
				}

				if ((above && below) || (right && left))
				{
					roadToInstantiate = buildrefs.RoadStraight;
					if (above && below)
					{
						rotation += new Vector3(0, 0, 0);
					}
					if (left && right)
					{
						rotation += new Vector3(0, 90, 0);
					}
				}
				else
				{
					roadToInstantiate = buildrefs.RoadCorner;
					if (above && right)
					{
						rotation += new Vector3(0, -90, 0);
					}
					if (right && below)
					{
						rotation += new Vector3(0, 0, 0);
					}
					if (below && left)
					{
						rotation += new Vector3(0, 90, 0);
					}
					if (left && above)
					{
						rotation += new Vector3(0, 180, 0);
					}
				}
				break;
			case 3:
				roadToInstantiate = buildrefs.RoadTJunction;

				foreach (var surroundingPoint in surroundingPointsOnNavMesh)
				{
					above |= surroundingPoint.DirectlyAbove(point);
					right |= surroundingPoint.DirectlyAdjacentRight(point);
					below |= surroundingPoint.DirectlyBelow(point);
					left |= surroundingPoint.DirectlyAdjacentLeft(point);
				}

				if (!above)
				{
					rotation += new Vector3(0, 0, 0);
				}
				if (!right)
				{
					rotation += new Vector3(0, 90, 0);
				}
				if (!below)
				{
					rotation += new Vector3(0, 180, 0);
				}
				if (!left)
				{
					rotation += new Vector3(0, -90, 0);
				}

				break;
			case 4:
				roadToInstantiate = buildrefs.RoadCrossroads;
				break;
			default:
				Debug.LogWarningFormat("network node has {0} connections, should be between 1 and 4...", surroundingPointsOnNavMesh.Count);
				break;

		}

		GameObject.Instantiate(roadToInstantiate, new Vector3 (point.x, -0.49f, point.y), Quaternion.Euler(rotation));
	}

	private static void SpawnBuildings()
	{
		var buildingReferences = Resources.Load<EnvironmentReferenceHolder>("ScriptableObjects/BuildingRefs");
		while (_spawnPoints.Count > 0)
		{
			SpawnBuilding(buildingReferences);
		}
	}

	public static IEnumerator SpawnBuildingsRoutine() // real time demo version
	{
		var buildingReferences = Resources.Load<EnvironmentReferenceHolder>("ScriptableObjects/BuildingRefs");
		while(_spawnPoints.Count > 0)
		{
			SpawnBuilding(buildingReferences);
			yield return new WaitForSeconds(0.1f);
		}
	}

	private static void SpawnBuilding(EnvironmentReferenceHolder buildrefs)
	{
		Vector2 point = _spawnPoints[0];
		bool longBuildingPossible = false; // place longer 2x1 buildings sometimes where we have two unnavigable points next to each other
		List<Vector2> adjacentUnnavigablePoints = new List<Vector2>();

		foreach (var otherPoint in _spawnPoints)
		{
			if (point.NextTo(otherPoint))
			{
				adjacentUnnavigablePoints.Add(otherPoint);
				longBuildingPossible = true;
			}
		}

		bool is1x1 = true;
		GameObject building;
		Quaternion buildingRotation = Quaternion.identity;
		Vector3 buildingLocation = new Vector3(point.x, kLevelHeight, point.y);

		if (longBuildingPossible)
		{
			building = buildrefs.GetRandomBuilding(out is1x1);
		}
		else
		{
			building = buildrefs.GetRandom1x1Building();
		}

		if (is1x1)
		{
			buildingRotation = AssignRotationFacingRoad(point);
		}
		else
		{
			//adjacentUnnavigablePoints
			bool forceVerticalRotation = false;
			bool forceHorizontalRotation = false;
			Vector2 selectedAdjacentPoint = adjacentUnnavigablePoints[Random.Range(0, adjacentUnnavigablePoints.Count - 1)];

			if (selectedAdjacentPoint.DirectlyAbove(point) || selectedAdjacentPoint.DirectlyBelow(point))
			{
				forceVerticalRotation = true;
			}

			if (selectedAdjacentPoint.DirectlyAdjacentLeft(point) || selectedAdjacentPoint.DirectlyAdjacentRight(point))
			{
				forceHorizontalRotation = true;
			}

			buildingLocation = new Vector3((point.x + selectedAdjacentPoint.x)/2f, kLevelHeight, (point.y + selectedAdjacentPoint.y) / 2f);
			buildingRotation = AssignRotationFacingRoad(point, forceHorizontalRotation, forceVerticalRotation);

			for (int i = _spawnPoints.Count - 1; i >= 0; i--)
			{
				if (_spawnPoints[i].RoughlyEquals(selectedAdjacentPoint))
				{
					_spawnPoints.RemoveAt(i);
				}
			}
		}

		GameObject.Instantiate(building, buildingLocation, buildingRotation);
		_spawnPoints.RemoveAt(0);
	}

	private static Quaternion AssignRotationFacingRoad(Vector2 point, bool forceHorizontalRotation = false, bool forceVerticalRotation = false)
	{
		Debug.Assert(!(forceHorizontalRotation && forceVerticalRotation), "forceHorizontalRotation and forceVerticalRotation should not both be true");
		var adjacentNavigablePoints = NodalNetworkTools.FindSurroundingPointsOnNavMesh(point);

		List<Vector3> rotations = new List<Vector3>();
		foreach (var adjacentPoint in adjacentNavigablePoints)
		{
			if (!forceHorizontalRotation && adjacentPoint.RoughlyEquals(point - new Vector2(1, 0)))
			{
				rotations.Add(new Vector3(0, -90, 0));
			}
			if (!forceVerticalRotation && adjacentPoint.RoughlyEquals(point - new Vector2(0, 1)))
			{
				rotations.Add(new Vector3(0, 180, 0));
			}
			if (!forceHorizontalRotation && adjacentPoint.RoughlyEquals(point + new Vector2(1, 0)))
			{
				rotations.Add(new Vector3(0, 90, 0));
			}
			if (!forceVerticalRotation && adjacentPoint.RoughlyEquals(point + new Vector2(0, 1)))
			{
				rotations.Add(new Vector3(0, 0, 0));
			}
		}

		if (rotations.Count == 0)
		{
			Debug.LogErrorFormat("no valid rotations found at point {0}", point);
		}
		
		Vector3 randomRotation = rotations[Random.Range(0, rotations.Count)];
		return Quaternion.Euler(randomRotation);
	}
}