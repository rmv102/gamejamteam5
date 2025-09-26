using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteSkin))]
public class softbody : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float slideSpeed = 18f;
    [SerializeField] LayerMask obstacleMask = ~0;
    [SerializeField] float maxSlideDistance = 50f;
    [SerializeField] float skin = 0.02f;
    [SerializeField] bool invertControls = true;

        [Header("Soft Body Physics")]
        [SerializeField] float jointStiffness = 0.3f;
        [SerializeField] float jointDamping = 0.8f;
        [SerializeField] float boneMass = 0.05f;
        [SerializeField] float boneDrag = 5f;
        [SerializeField] float boneAngularDrag = 10f;
        [SerializeField] float maxBoneDistance = 0.5f;

    [Header("Virus Trail FX")]
    [SerializeField] ParticleSystem trailPrefab;
    [SerializeField] bool trailEnabled = true;
    [SerializeField] int startBurstParticles = 3;
    [SerializeField] int stopBurstParticles = 2;
    [SerializeField] float trailEmissionRate = 8f;

    // Core components
    private Rigidbody2D mainRigidbody;
    private SpriteSkin spriteSkin;
    private CircleCollider2D mainCollider;
    private ParticleSystem trailInstance;

    // Movement state
    private bool isSliding;
    private Vector2 slideTarget;

    // Soft body bones
    private Transform[] bones;
    private Rigidbody2D[] boneRigidbodies;
    private SpringJoint2D[] boneJoints;

    void Awake()
    {
        mainRigidbody = GetComponent<Rigidbody2D>();
        spriteSkin = GetComponent<SpriteSkin>();
        mainCollider = GetComponent<CircleCollider2D>();

        // Configure main rigidbody for top-down movement
        if (mainRigidbody != null)
        {
            mainRigidbody.gravityScale = 0f;
            mainRigidbody.linearDamping = 0f;
            mainRigidbody.freezeRotation = true;
        }

        // Setup soft body bones
        SetupSoftBodyBones();
        
        // Force SpriteSkin to rebind
        if (spriteSkin != null)
        {
            spriteSkin.enabled = false;
            spriteSkin.enabled = true;
        }

        // Setup particle trail
        if (trailEnabled && trailPrefab != null)
        {
            trailInstance = Instantiate(trailPrefab, transform.position, Quaternion.identity, transform);
            ConfigureVirusParticles();
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (isSliding) return;

        Vector2 inputDir = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) inputDir.y += 1f;
        if (Keyboard.current.sKey.isPressed) inputDir.y -= 1f;
        if (Keyboard.current.aKey.isPressed) inputDir.x -= 1f;
        if (Keyboard.current.dKey.isPressed) inputDir.x += 1f;

        bool pressedThisFrame = (
            Keyboard.current.wKey.wasPressedThisFrame ||
            Keyboard.current.aKey.wasPressedThisFrame ||
            Keyboard.current.sKey.wasPressedThisFrame ||
            Keyboard.current.dKey.wasPressedThisFrame
        );

        if (invertControls)
        {
            inputDir = -inputDir;
        }

        if (pressedThisFrame && inputDir != Vector2.zero)
        {
            BeginSlide(inputDir.normalized);
        }
    }

    void FixedUpdate()
    {
        if (!isSliding || mainRigidbody == null) return;

        Vector2 current = mainRigidbody.position;
        Vector2 toTarget = slideTarget - current;
        float step = slideSpeed * Time.fixedDeltaTime;

        if (toTarget.magnitude <= step)
        {
            mainRigidbody.MovePosition(slideTarget);
            mainRigidbody.linearVelocity = Vector2.zero;
            isSliding = false;

            // Stop particle trail
            if (trailInstance != null)
            {
                var emission = trailInstance.emission;
                emission.rateOverTime = 0f;
                var burst = new ParticleSystem.Burst(0f, (short)stopBurstParticles);
                emission.SetBursts(new ParticleSystem.Burst[] { burst });
                trailInstance.Play();
                emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            }
            return;
        }

        Vector2 next = current + toTarget.normalized * step;
        mainRigidbody.MovePosition(next);

        // Update particle trail position
        if (trailInstance != null)
        {
            trailInstance.transform.position = mainRigidbody.position;
            var emission = trailInstance.emission;
            emission.rateOverTime = trailEmissionRate;
            if (!trailInstance.isPlaying)
            {
                trailInstance.Play();
            }
        }
    }

    void LateUpdate()
    {
        // Constrain bones to prevent excessive movement
        if (bones != null && mainRigidbody != null)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    Vector2 bonePos = bones[i].position;
                    Vector2 centerPos = mainRigidbody.position;
                    float distance = Vector2.Distance(bonePos, centerPos);
                    
                    // If bone is too far from center, pull it back
                    if (distance > maxBoneDistance)
                    {
                        Vector2 direction = (centerPos - bonePos).normalized;
                        bones[i].position = centerPos + direction * maxBoneDistance;
                    }
                }
            }
        }
    }

    void BeginSlide(Vector2 direction)
    {
        if (direction == Vector2.zero) return;

        float radius = mainCollider != null ? mainCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) : 0.5f;
        float stopOffset = radius + skin;

        RaycastHit2D hit = Physics2D.CircleCast(mainRigidbody.position, radius, direction, maxSlideDistance, obstacleMask);

        if (hit.collider != null)
        {
            slideTarget = hit.point - direction.normalized * stopOffset;
        }
        else
        {
            slideTarget = mainRigidbody.position + direction.normalized * maxSlideDistance;
        }

        isSliding = true;

        // Start particle trail
        if (trailInstance != null)
        {
            trailInstance.transform.position = mainRigidbody.position;
            var emission = trailInstance.emission;
            emission.rateOverTime = trailEmissionRate;
            var burst = new ParticleSystem.Burst(0f, (short)startBurstParticles);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            trailInstance.Play();
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
        }
    }

    void SetupSoftBodyBones()
    {
        if (spriteSkin == null) return;

        // Get all bones from SpriteSkin
        bones = new Transform[spriteSkin.boneTransforms.Length];
        boneRigidbodies = new Rigidbody2D[spriteSkin.boneTransforms.Length];
        boneJoints = new SpringJoint2D[spriteSkin.boneTransforms.Length];

        for (int i = 0; i < spriteSkin.boneTransforms.Length; i++)
        {
            Transform bone = spriteSkin.boneTransforms[i];
            bones[i] = bone;

            // Add Rigidbody2D to each bone
            Rigidbody2D boneRb = bone.GetComponent<Rigidbody2D>();
            if (boneRb == null)
            {
                boneRb = bone.gameObject.AddComponent<Rigidbody2D>();
            }

            // Configure bone physics
            boneRb.gravityScale = 0f;
            boneRb.mass = boneMass;
            boneRb.linearDamping = boneDrag;
            boneRb.angularDamping = boneAngularDrag;
            boneRb.freezeRotation = true;

            boneRigidbodies[i] = boneRb;

            // Add collider to bone
            CircleCollider2D boneCollider = bone.GetComponent<CircleCollider2D>();
            if (boneCollider == null)
            {
                boneCollider = bone.gameObject.AddComponent<CircleCollider2D>();
                boneCollider.radius = 0.1f; // Small collider for bones
            }

            // Connect bone to main body with SpringJoint2D
            SpringJoint2D joint = bone.GetComponent<SpringJoint2D>();
            if (joint == null)
            {
                joint = bone.gameObject.AddComponent<SpringJoint2D>();
            }

            joint.connectedBody = mainRigidbody;
            joint.autoConfigureDistance = false;
            joint.distance = Vector2.Distance(bone.position, transform.position);
            joint.dampingRatio = jointDamping;
            joint.frequency = jointStiffness;
            joint.enableCollision = false; // Prevent bones from colliding with main body

            boneJoints[i] = joint;
        }
    }

    void ConfigureVirusParticles()
    {
        if (trailInstance == null) return;

        var main = trailInstance.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 0.4f;
        main.startSpeed = 0.1f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
        main.gravityModifier = 0f;
        main.maxParticles = 50;

        var emission = trailInstance.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = trailInstance.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var velocityOverLifetime = trailInstance.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.1f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

        var colorOverLifetime = trailInstance.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.black, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = trailInstance.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 1.0f);
        sizeCurve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        trailInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnDrawGizmosSelected()
    {
        if (mainRigidbody != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, mainCollider != null ? mainCollider.radius : 0.5f);
        }
    }
}