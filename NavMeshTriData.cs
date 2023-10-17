using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavMeshTriData : ScriptableObject
{
	public void Init(Triangle[] triangles)
	{
		_allTriangles = triangles;
	}

	[SerializeField]
	private Triangle[] _allTriangles;
	
	public Triangle[] AllTriangles
	{
		get { return _allTriangles; }
	}
}
