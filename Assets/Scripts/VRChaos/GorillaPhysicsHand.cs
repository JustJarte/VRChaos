using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GorillaPhysicsHand : MonoBehaviour
{
    [Header("PID")]
    [SerializeField] private float frequency = 50.0f;
    [SerializeField] private float damping = 1.0f;
    [SerializeField] private float rotFrequency = 100.0f;
    [SerializeField] private float rotDamping = 0.9f;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private Transform target;

    [Header("Vertical Variables")]
    [SerializeField] private float climbForce = 1000.0f;
    [SerializeField] private float climbDrag = 500.0f;

    private Rigidbody _rigidbody;
    private Vector3 _previousPosition;
    private bool _isColliding = false;

    private void Start()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;

        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.maxAngularVelocity = float.PositiveInfinity;
        _previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        PIDMovement();
        PIDRotation();

        if (_isColliding)
        {
            HookesLaw();
        }
    }

    private void PIDMovement()
    {
        float kp = (6.0f * frequency) * (6.0f * frequency) * 0.25f;
        float kd = 4.5f * frequency * damping;
        float g = 1.0f / (1.0f + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;
        Vector3 force = (target.position - transform.position) * ksg + (playerRigidbody.velocity - _rigidbody.velocity) * kdg;

        _rigidbody.AddForce(force, ForceMode.Acceleration);
    }

    private void PIDRotation()
    {
        float kp = (6.0f * rotFrequency) * (6.0f * rotFrequency) * 0.25f;
        float kd = 4.5f * rotFrequency * rotDamping;
        float g = 1.0f / (1.0f + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;
        Quaternion q = target.rotation * Quaternion.Inverse(transform.rotation);

        if (q.w < 0)
        {
            q.x = -q.x;
            q.y = -q.y;
            q.z = -q.z;
            q.w = -q.w;
        }

        q.ToAngleAxis(out float angle, out Vector3 axis);
        axis.Normalize();
        axis *= Mathf.Deg2Rad;
        Vector3 torque = ksg * axis * angle + -_rigidbody.angularVelocity * kdg;

        _rigidbody.AddTorque(torque, ForceMode.Acceleration);
    }

    private void HookesLaw()
    {
        Vector3 displacementFromResting = transform.position - target.position;
        Vector3 force = displacementFromResting * climbForce;
        float drag = GetDrag();

        playerRigidbody.AddForce(force, ForceMode.Acceleration);
        playerRigidbody.AddForce(drag * -playerRigidbody.velocity * climbDrag, ForceMode.Acceleration);
    }

    private float GetDrag()
    {
        Vector3 handVelocity = (target.localPosition - _previousPosition) / Time.fixedDeltaTime;
        float drag = 1.0f / handVelocity.magnitude + 0.01f;

        drag = drag > 1 ? 1 : drag;
        drag = drag < 0.03f ? 0.03f : drag;

        _previousPosition = transform.position;

        return drag;
    }

    private void OnCollisionEnter(Collision collision)
    {
        _isColliding = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        _isColliding = false;
    }
}
