using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnvironmentRefs", menuName = "ScriptableObjects/BuildingReferenceHolder", order = 1)]
public class EnvironmentReferenceHolder : ScriptableObject
{
	[SerializeField]
	private GameObject[] _1x1buildings;
	[SerializeField]
	private GameObject[] _2x1buildings;

	[SerializeField]
	private GameObject _roadDeadEnd;
	[SerializeField]
	private GameObject _roadStraight;
	[SerializeField]
	private GameObject _roadCorner;
	[SerializeField]
	private GameObject _roadTJunction;
	[SerializeField]
	private GameObject _roadCrossroads;

	public GameObject RoadDeadEnd { get { return _roadDeadEnd; } }
	public GameObject RoadStraight { get { return _roadStraight; } }
	public GameObject RoadCorner { get { return _roadCorner; } }
	public GameObject RoadTJunction { get { return _roadTJunction; } }
	public GameObject RoadCrossroads { get { return _roadCrossroads; } }

	private List<GameObject> _allBuildings;

	public GameObject GetRandom1x1Building()
	{
		return _1x1buildings[Random.Range(0, _1x1buildings.Length)];
	}

	public GameObject GetRandomBuilding(out bool is1x1)
	{
		if (_allBuildings == null)
		{
			_allBuildings = new List<GameObject>();
			_allBuildings.AddRange(_1x1buildings);
			_allBuildings.AddRange(_2x1buildings);
		}

		int selectedBuildingIndex = Random.Range(0, _allBuildings.Count);
		is1x1 = selectedBuildingIndex < _1x1buildings.Length ? true : false;
		return _allBuildings[selectedBuildingIndex];
	}
}
