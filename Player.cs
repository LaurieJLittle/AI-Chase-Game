using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;

public class Player : MonoBehaviour
{
	public Action OnPositionChanged;
	
	private Vector3 _cachedPosition;
	private static SeperatePathFinderAIsCoordinator _coordinator;
    private Rigidbody _rigidbody;
    private bool _braking;

    [SerializeField]
    private bool _debugCopMovement = false; // manual police chase to current location on space when true
    [SerializeField]
    private float _accelerationModifier = 25f;
    [SerializeField]
    private float _dragModifier = 0.01f;
    [SerializeField]
    private float _turningModifier = 70f;

    private void Awake()
    {
         _rigidbody = GetComponent<Rigidbody>();
    }
    private void Update()
	{
		if (!_debugCopMovement || Input.GetKeyDown(KeyCode.Space))
		{
			if ((transform.position - _cachedPosition).sqrMagnitude > 1)
			{
				_cachedPosition = transform.position;
				if (OnPositionChanged != null)
				{
					OnPositionChanged();
				}
			}
		}

        float verticalAxis = Input.GetAxis("Vertical");
        float horizontalAxis = Input.GetAxis("Horizontal");
        float directionalAccelerationModifier = verticalAxis > 0.01f ? _accelerationModifier : 0;

        _braking = Input.GetKey(KeyCode.LeftShift);
        if (_braking)
        {
	        _rigidbody.AddForce(-5f * _rigidbody.velocity);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
	        ResetPosition();
        }
        
		// updating physics
        Vector3 velocity = _rigidbody.velocity;
        Vector3 scaledForwardTransform = transform.forward * velocity.magnitude;
        float turningDeceleration = Vector3.Dot(velocity.normalized, scaledForwardTransform.normalized);
        Vector3 xzVelocity = Mathf.Pow(turningDeceleration,2) * Vector3.RotateTowards(_rigidbody.velocity, scaledForwardTransform, float.MaxValue, float.MaxValue);
        _rigidbody.velocity = new Vector3(xzVelocity.x, _rigidbody.velocity.y, xzVelocity.z);

        _rigidbody.AddRelativeForce(new Vector3(0, 0, verticalAxis * directionalAccelerationModifier));
        _rigidbody.drag = _dragModifier * Mathf.Pow(_rigidbody.velocity.magnitude, 3);

        // steering
        if (_rigidbody.velocity.magnitude > 1f || verticalAxis > 0.01f)
        {
            float adjustTurningSpeedForBraking = _braking ? 0.5f : 1f;
            _rigidbody.AddRelativeTorque(new Vector3(0, horizontalAxis * _turningModifier * adjustTurningSpeedForBraking, 0));
        }
    }

    private void ResetPosition()
    {
	    transform.position = new Vector3(5, 2, 5);
    }
}
