using System.Collections;
using System.Collections.Generic;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(NavMeshAgent))]
public class CustomCharacterMovement3D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _speed = 3.5f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _turnSpeed = 10f;
    [SerializeField] private float _stoppingDistance = 0.5f;

    [Header("Grounding")]
    [SerializeField] private float _groundCheckOffset = 0.1f;
    [SerializeField] private float _groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask _groundMask = 1 << 0; // set to Default layer using bit shift

    [Header("Size")]
    [SerializeField] private float _height = 1.8f;
    [SerializeField] private float _radius = 0.3f;

    [Header("Airborne")]
    [SerializeField] private float _gravity = 20f;
    [SerializeField] private float _jumpHeight = 2.25f;
    [SerializeField] private float _airControl = 0.1f;

    [Header("Avoidance")]
    [SerializeField] private float _speedVariation = 0.5f;
    [SerializeField] private float _neighborDistance = 3f;
    [SerializeField] private float _cornerNeighborDistance = 1f;
    [SerializeField] private LayerMask _neighborMask;

    private float _variationNoiseOffset;
    private Collider[] _neighborHits;

    public float MoveSpeedMultiplier = 1f;
    public Vector3 MoveInput { get; private set; }
    public Vector3 LookDirection { get; private set; }
    public bool IsGrounded { get; private set; } = true;
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public bool CanMove { get; set; } = true;

    [Header("Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private CapsuleCollider _capsuleCollider;

    private void OnValidate()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _capsuleCollider = GetComponent<CapsuleCollider>();

        // configure rigidbody
        _rigidbody.useGravity = false;
        _rigidbody.freezeRotation = true;

        // configure collider
        _capsuleCollider.height = _height;
        _capsuleCollider.center = new Vector3(0f, _height * 0.5f, 0f);
        _capsuleCollider.radius = _radius;

        // configure navmeshagent
        _navMeshAgent.height = _height;
        _navMeshAgent.radius = _radius;
    }

    private void Awake()
    {
        // disable navmeshagent movement
        _navMeshAgent.updatePosition = false;
        _navMeshAgent.updateRotation = false;

        // assign frictionless material to collider
        
        _capsuleCollider.material = new PhysicMaterial("NoFriction")
        {
            staticFriction = 0f,
            dynamicFriction = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum
        };
        

        // set up avoidance values
        _neighborHits = new Collider[16];
        _variationNoiseOffset = Random.value * 10f;
    }

    public void SetMoveInput(Vector3 input)
    {
        input = Vector3.ClampMagnitude(input, 1f);
        if(input.magnitude > 0.1f)
        {
            input = new Vector3(input.x, 0f, input.z);
            MoveInput = input;
        }
        else
        {
            MoveInput = Vector3.zero;
        }
    }

    public void SetLookPosition(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        SetLookDirection(direction);
    }

    public void SetLookDirection(Vector3 direction)
    {
        LookDirection = new Vector3(direction.x, 0f, direction.z).normalized;
    }

    public void Jump()
    {
        // stop if not grounded
        if (!IsGrounded) return;

        float jumpSpeed = Mathf.Sqrt(2f * _gravity * _jumpHeight);

        // overwrite just the Y value of velocity
        Vector3 velocity = _rigidbody.velocity;
        velocity.y = jumpSpeed;
        _rigidbody.velocity = velocity;
    }

    public void MoveTo(Vector3 destination)
    {
        _navMeshAgent.SetDestination(destination);
    }

    public void Stop()
    {
        _navMeshAgent.ResetPath();
        SetMoveInput(Vector3.zero);
    }

    private void FixedUpdate()
    {
        // TODO: find grounded state
        IsGrounded = CheckGrounded();

        float speed = _speed;

        // TODO: move according to navigation
        if(_navMeshAgent.hasPath)
        {
            Vector3 nextPathPoint = _navMeshAgent.path.corners[1];
            Vector3 pathDir = (nextPathPoint - transform.position).normalized;

            // add in avoidance
            float neighborDistance = _neighborDistance;
            if (_navMeshAgent.path.corners.Length > 2) neighborDistance = _cornerNeighborDistance;
            pathDir = GetAvoidanceDirection(nextPathPoint, neighborDistance);

            SetMoveInput(pathDir);
            SetLookDirection(pathDir);

            // vary speed
            float noise = Mathf.PerlinNoise(Time.time, _variationNoiseOffset) * 2f - 1f;
            speed = _speed * (1f + noise * _speedVariation);
        }

        // sync navmeshagent position with character position
        _navMeshAgent.nextPosition = transform.position;

        // TODO: move character
        // find ground aligned forward direction
        Vector3 input = MoveInput;
        Vector3 right = Vector3.Cross(transform.up, input);
        Vector3 forward = Vector3.Cross(right, GroundNormal);

        // calculate move velocity
        Vector3 targetVelocity = forward * speed * MoveSpeedMultiplier;
        if (!CanMove) targetVelocity = Vector3.zero;
        Vector3 flattenedVelocity = new Vector3(_rigidbody.velocity.x, 0f, _rigidbody.velocity.z);
        Vector3 velocityDiff = targetVelocity - flattenedVelocity;
        // some people hate this convention (listen to your boss)
        float control = IsGrounded ? 1f : _airControl;
        Vector3 acceleration = velocityDiff * _acceleration * control;
        // add gravity (towards ground, not necessarily down)
        acceleration += GroundNormal * -_gravity;

        // accelerate incorporating mass (heavy characters move light characters)
        _rigidbody.AddForce(acceleration * _rigidbody.mass);

        // TODO: turn character
        // ensure look direction is correct
        if(LookDirection.magnitude > 0.1f)
        {
            // turn look direction (Vector3) into rotation (Quaternion)
            Quaternion targetRotation = Quaternion.LookRotation(LookDirection);
            Quaternion currentRotation = transform.rotation;
            Quaternion rotation = Quaternion.Slerp(currentRotation, targetRotation, _turnSpeed * Time.fixedDeltaTime);
            transform.rotation = rotation;
        }
    }

    private bool CheckGrounded()
    {
        Vector3 groundCheckStart = transform.position + transform.up * _groundCheckOffset;
        // raycast to find ground
        bool hit = Physics.Raycast(groundCheckStart, -transform.up, out RaycastHit hitInfo, _groundCheckDistance, _groundMask);

        // set default ground normal
        GroundNormal = Vector3.up;

        // if ground wasn't hit
        if (!hit) return false;

        // set ground normal from hit
        GroundNormal = hitInfo.normal;

        return true;
    }

    public void FootstepAnimEvent()
    {

    }

    private Vector3 GetAvoidanceDirection(Vector3 destination, float neighborDistance)
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        // initialize boid values
        Vector3 separation = Vector3.zero;
        Vector3 cohesion = destination;
        Vector3 alignment = transform.forward;

        // find neighbors
        int hitCount = Physics.OverlapSphereNonAlloc(currentPosition, neighborDistance, _neighborHits, _neighborMask);
        int neighborCount = 0;

        // iterate through neighbors
        for (int i = 0; i < hitCount; i++)
        {
            GameObject possibleBoid = _neighborHits[i].gameObject;
            // filter out our own boid
            if (possibleBoid == gameObject) continue;
            neighborCount++;
            Transform boidTransform = possibleBoid.transform;
            alignment += boidTransform.forward;     // average togther forward directions
            cohesion += boidTransform.position;     // average together positions
            separation += GetSeparationVector(boidTransform, neighborDistance);   // accumulate separation force (pushing apart)
        }

        // average boid values
        alignment /= (neighborCount + 1);
        cohesion /= (neighborCount + 1);
        // normalize cohesion so we can use as direction
        cohesion = (cohesion - currentPosition).normalized;

        // find target rotation by accumulating all vectors
        Vector3 direction = separation + alignment + cohesion;
        return Vector3.ClampMagnitude(direction, 1f);   // clamps a Vector3 to a certain length
    }

    private Vector3 GetSeparationVector(Transform target, float NeighborDistance)
    {
        Vector3 diff = transform.position - target.position;
        float magnitude = diff.magnitude;
        // find the inverse force pushing the transforms apart (clamped from 0 to 1)
        float scaler = Mathf.Clamp01(1f - magnitude / NeighborDistance);
        // re-incorporate direction (Vector3) into our separation force
        // the closer we are to the transform, the more we push away
        return diff * (scaler / magnitude);
    }
}