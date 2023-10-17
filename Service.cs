using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Service<T> where T: class
{
	private static T _service;

	public static T Find()
	{
		return _service;
	}

	static bool Exists()
	{
		if (_service != null)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static void Init(T service)
	{
		if (service != null)
		{
			if (!Exists())
			{
				_service = service;
			}
			else
			{
				Debug.LogWarningFormat("service {0} already exists!", service.GetType());
			}
		}
		else
		{
			Debug.LogError("failed to initialise service!");
		}
	}
}
