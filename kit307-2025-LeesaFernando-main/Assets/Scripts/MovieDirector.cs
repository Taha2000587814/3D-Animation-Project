using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class MovieDirector : MonoBehaviour
{
    [Header("Character Settings")]
    public Transform character;
    public Transform gate;
    public Animator characterAnimator;
    private NavMeshAgent agent;
    private bool gateTriggered = false;
    private bool impactTriggered = false;

    [Header("Gate Material Animation")]
    public Material gateMaterial;
    public string heightProperty = "_HeightStrength";
    public float heightMin = 0f;
    public float heightMax = 1f;
    public float heightSpeed = 2f;
    private bool animateHeight = false;
    private float heightDirection = 1f;

    [Header("Gate Lights")]
    public Light pointLightA;
    public Light pointLightB;
    public float maxIntensity = 100f;
    public float activationDistance = 10f;

    [Header("Camera Settings")]
    public Rigidbody cameraRb;
    public Transform cameraTarget;
    public float cameraFollowSpeed = 5f;
    public float cameraRandomForce = 2f;
    public float minY = 1f;
    public float maxY = 5f;
    private Vector3 initialCameraOffset;

    [Header("Camera Shake & Orbit")]
    public float shakeIntensity = 0.2f;
    public float shakeFrequency = 0.5f;
    public float orbitRadius = 2f;
    public float orbitSpeed = 1f;
    private float orbitAngle = 0f;
    private float shakeTimer = 0f;

    [Header("Gate Zoom")]
    public float zoomInterval = 8f;
    public float zoomDuration = 2f;
    public float zoomStrength = 3f;
    private float zoomTimer = 0f;
    private bool isZooming = false;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeSpeed = 1f;
    private bool startFade = false;

    void Start()
    {
        agent = character.GetComponent<NavMeshAgent>();
        agent.SetDestination(gate.position);
        characterAnimator?.SetBool("isWalking", true);

        initialCameraOffset = cameraRb.transform.position - cameraTarget.position;
        RandomizeCameraVelocity();

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void Update()
    {
        HandleCharacterMovement();
        HandleGateMaterial();
        HandleGateLights();
        HandleCameraTracking();
        HandleCameraShake();
        HandleCameraOrbit();
        HandleGateZoom();
        HandleFadeToBlack();
    }

    void HandleCharacterMovement()
    {
        if (gateTriggered || agent == null) return;

        float distance = Vector3.Distance(character.position, gate.position);

        if (distance <= agent.stoppingDistance)
        {
            gateTriggered = true;
            agent.isStopped = true;
            characterAnimator?.SetBool("isWalking", false);
            animateHeight = true;
        }
    }

    void HandleGateMaterial()
    {
        if (!animateHeight || gateMaterial == null) return;

        float current = gateMaterial.GetFloat(heightProperty);
        current += heightDirection * heightSpeed * Time.deltaTime;

        if (current >= heightMax || current <= heightMin)
        {
            heightDirection *= -1f;
            current = Mathf.Clamp(current, heightMin, heightMax);
        }

        gateMaterial.SetFloat(heightProperty, current);
    }

    void HandleGateLights()
    {
        if (pointLightA == null || pointLightB == null) return;

        float distance = Vector3.Distance(character.position, gate.position);
        float t = Mathf.InverseLerp(activationDistance, 0f, distance);
        float intensity = Mathf.Lerp(0f, maxIntensity, t);

        pointLightA.intensity = intensity;
        pointLightB.intensity = intensity;

        if (intensity >= maxIntensity && !impactTriggered)
        {
            impactTriggered = true;
            characterAnimator?.SetTrigger("impactFall");
            startFade = true;
        }
    }

    void HandleCameraTracking()
    {
        if (cameraRb == null || cameraTarget == null) return;

        Vector3 desiredPosition = cameraTarget.position + initialCameraOffset;
        Vector3 direction = desiredPosition - cameraRb.transform.position;

        cameraRb.AddForce(direction * cameraFollowSpeed);

        Vector3 clampedPosition = cameraRb.transform.position;
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minY, maxY);
        cameraRb.transform.position = clampedPosition;

        cameraRb.transform.LookAt(cameraTarget);
    }

    void HandleCameraShake()
    {
        shakeTimer += Time.deltaTime;
        if (shakeTimer >= shakeFrequency)
        {
            shakeTimer = 0f;
            Vector3 shake = Random.insideUnitSphere * shakeIntensity;
            cameraRb.AddForce(shake, ForceMode.Impulse);
        }
    }

    void HandleCameraOrbit()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;
        Vector3 offset = new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * orbitRadius;
        Vector3 orbitPosition = cameraTarget.position + offset + initialCameraOffset;
        cameraRb.MovePosition(Vector3.Lerp(cameraRb.position, orbitPosition, Time.deltaTime));
    }

    void HandleGateZoom()
    {
        zoomTimer += Time.deltaTime;

        if (zoomTimer >= zoomInterval && !isZooming)
        {
            isZooming = true;
            zoomTimer = 0f;
        }

        if (isZooming)
        {
            Vector3 zoomDirection = (gate.position - cameraRb.transform.position).normalized;
            cameraRb.AddForce(zoomDirection * zoomStrength, ForceMode.Impulse);

            if (zoomTimer >= zoomDuration)
            {
                isZooming = false;
                zoomTimer = 0f;
            }
        }
    }

    void HandleFadeToBlack()
    {
        if (!startFade || fadeImage == null) return;

        Color c = fadeImage.color;
        c.a += fadeSpeed * Time.deltaTime;
        fadeImage.color = c;
    }

    void RandomizeCameraVelocity()
    {
        if (cameraRb == null) return;

        Vector3 randomVelocity = new Vector3(
            Random.Range(-cameraRandomForce, cameraRandomForce),
            Random.Range(-cameraRandomForce, cameraRandomForce),
            Random.Range(-cameraRandomForce, cameraRandomForce)
        );

        cameraRb.linearVelocity = randomVelocity;
    }
}
