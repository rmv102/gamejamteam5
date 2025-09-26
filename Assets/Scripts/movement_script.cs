using UnityEngine;                                                  
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class movement_script : MonoBehaviour
{
    [SerializeField] float slideSpeed = 18f;
    [SerializeField] LayerMask obstacleMask = ~0; // default: collide with everything
    [SerializeField] float maxSlideDistance = 50f;
    [SerializeField] float skin = 0.02f; // small inset to avoid clipping
    [SerializeField] bool invertControls = true; // fixes current inverted world orientation
    [Header("Virus Trail FX")]
    [SerializeField] ParticleSystem trailPrefab; // optional - assign a prefab to enable
    [SerializeField] bool trailEnabled = true;              
    [SerializeField] int startBurstParticles = 3; // small dots oozing out
    [SerializeField] int stopBurstParticles = 2;
    [SerializeField] float trailEmissionRate = 8f; // continuous oozing while moving
    
    Rigidbody2D rb; // Rigidbody2D component
    CircleCollider2D circle;
    ParticleSystem trailInstance;

    bool isSliding;
    Vector2 slideTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.freezeRotation = true;
        }

        if (trailEnabled && trailPrefab != null)
        {
            trailInstance = Instantiate(trailPrefab, transform.position, Quaternion.identity, transform);
            ConfigureVirusParticles();
        }
    }

    void Update()
    {
		if (Keyboard.current == null)
		{
			return;
		}

		if (isSliding)
		{
			return;
		}

		Vector2 inputDir = Vector2.zero;
		if (Keyboard.current.wKey.isPressed) inputDir.y += 1f;
		if (Keyboard.current.sKey.isPressed) inputDir.y -= 1f;
		if (Keyboard.current.aKey.isPressed) inputDir.x -= 1f;
		if (Keyboard.current.dKey.isPressed) inputDir.x += 1f;

		// Only trigger a new slide on fresh press of any movement key
		bool pressedThisFrame =
			(Keyboard.current.wKey.wasPressedThisFrame ||
			 Keyboard.current.aKey.wasPressedThisFrame ||
			 Keyboard.current.sKey.wasPressedThisFrame ||
			 Keyboard.current.dKey.wasPressedThisFrame);

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
        if (!isSliding || rb == null)
        {
            return;
        }

        Vector2 current = rb.position;
        Vector2 toTarget = slideTarget - current;
        float step = slideSpeed * Time.fixedDeltaTime;

        if (toTarget.magnitude <= step)
        {
            rb.MovePosition(slideTarget);
            rb.linearVelocity = Vector2.zero;
            isSliding = false;

            if (trailInstance != null)
            {
                // Stop continuous oozing when stopped
                var emission = trailInstance.emission;
                emission.rateOverTime = 0f;
                // Small burst on stop
                var burst = new ParticleSystem.Burst(0f, (short)stopBurstParticles);
                emission.SetBursts(new ParticleSystem.Burst[] { burst });
                trailInstance.Play();
                emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            }
            return;
        }

        Vector2 next = current + toTarget.normalized * step;
        rb.MovePosition(next);

        if (trailInstance != null)
        {
            trailInstance.transform.position = rb.position;
            // Continuous oozing while moving
            var emission = trailInstance.emission;
            emission.rateOverTime = trailEmissionRate;
            if (!trailInstance.isPlaying)
            {
                trailInstance.Play();
            }
        }
    }

    void BeginSlide(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            return;
        }

        float radius = circle != null ? circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) : 0f;
        float stopOffset = radius + skin;

        // Use a CircleCast so our circle fits through tight spaces without clipping
        RaycastHit2D hit = Physics2D.CircleCast(rb.position, radius, direction, maxSlideDistance, obstacleMask);

        if (hit.collider != null)
        {
            slideTarget = hit.point - direction.normalized * stopOffset;
        }
        else
        {
            slideTarget = rb.position + direction.normalized * maxSlideDistance;
        }

        isSliding = true;

        if (trailInstance != null)
        {
            trailInstance.transform.position = rb.position;
            // Start continuous oozing
            var emission = trailInstance.emission;
            emission.rateOverTime = trailEmissionRate;
            // Small burst on start
            var burst = new ParticleSystem.Burst(0f, (short)startBurstParticles);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            trailInstance.Play();
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
        }
    }

    void ConfigureVirusParticles()
    {
        if (trailInstance == null) return;

        var main = trailInstance.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 0.4f; // short lifetime for small dots
        main.startSpeed = 0.1f; // very slow movement
        main.startSize = 0.08f; // tiny dots
        main.startColor = new Color(0.8f, 0.2f, 0.2f, 0.7f); // dark red virus color
        main.gravityModifier = 0f;
        main.maxParticles = 50; // limit for performance

        var emission = trailInstance.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f; // controlled by script

        var shape = trailInstance.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f; // very small emission area

        var velocityOverLifetime = trailInstance.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.1f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f); // slight downward drift

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
}
