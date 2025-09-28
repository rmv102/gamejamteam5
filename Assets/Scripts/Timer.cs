using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine.U2D;

public class Timer : MonoBehaviour
{
    public ScoreTracker scoreTracker; // Assign in Inspector
    public Text gameOverText; // Assign in Inspector (the overlay message)
    private bool gameOverShown = false;
    public Text timerText; // Assign this in the Inspector
    public float remainingTime = 20f; // 2 minutes in seconds
    private bool timerActive = true;
    private int level = 1;
    public GameObject playerSpawner; 
    public GameObject arm;
    public GameObject chest;
    public GameObject head;
    public CinemachineCamera camOut; // Assign in Inspector


    public Color targetColor = new Color32(0x11, 0x11, 0x11, 255); // #111111

    private SpriteShapeRenderer[] shapeRenderers;
    private Color[] originalColors;
    private GameObject[] areas;
    private int currentAreaIndex = 0;


    void Start()
    {
        areas = new GameObject[] { arm, chest, head };
        shapeRenderers = areas[currentAreaIndex].GetComponentsInChildren<SpriteShapeRenderer>();

        // Store original colors
        originalColors = new Color[shapeRenderers.Length];
        for (int i = 0; i < shapeRenderers.Length; i++)
        {
            originalColors[i] = shapeRenderers[i].color;
        }
    }

    void ChangeArea()
    {
        shapeRenderers = areas[currentAreaIndex].GetComponentsInChildren<SpriteShapeRenderer>();

        // Store original colors
        for (int i = 0; i < shapeRenderers.Length; i++)
        {
            shapeRenderers[i].color = targetColor;
        }

        currentAreaIndex++;
        if (currentAreaIndex < areas.Length)
        {
            shapeRenderers = areas[currentAreaIndex].GetComponentsInChildren<SpriteShapeRenderer>();

            // Store original colors
            originalColors = new Color[shapeRenderers.Length];
            for (int i = 0; i < shapeRenderers.Length; i++)
            {
                originalColors[i] = shapeRenderers[i].color;
            }
        }
    }

    private bool levelTriggered = false; // prevents multiple triggers per level

void Update()
{
    int currentInfected = GameObject.FindGameObjectsWithTag(scoreTracker.particleTag).Length;
    float percent = (float)currentInfected / Mathf.Max(1, scoreTracker.maxParticles);
    float percentColor = Mathf.Clamp01(percent);

    // Fade current area
    for (int i = 0; i < shapeRenderers.Length; i++)
    {
        shapeRenderers[i].color = Color.Lerp(originalColors[i], targetColor, percentColor);
    }

    // Level completion
    if (percent >= 0.8f && !levelTriggered)
    {
        levelTriggered = true; // lock for this level

        // Clear particle objects
        GameObject[] particles = GameObject.FindGameObjectsWithTag(scoreTracker.particleTag);
        foreach (GameObject obj in particles)
            Destroy(obj);

        // Move player
        playerSpawner.GetComponent<PlayerSpawner>().MovePlayerToNextSpawn();

        // Move to next area
        currentAreaIndex++;
        if (currentAreaIndex < areas.Length)
        {
            // Setup new area
            shapeRenderers = areas[currentAreaIndex].GetComponentsInChildren<SpriteShapeRenderer>();
            originalColors = new Color[shapeRenderers.Length];
            for (int i = 0; i < shapeRenderers.Length; i++)
                originalColors[i] = shapeRenderers[i].color;

            level++;
            remainingTime += 60f;
            scoreTracker.setPercentage(0);
            levelTriggered = false; // unlock for next level
        }
        else
        {
            // No more areas → win
            ShowGameOver(true);
        }
    }

    // Timer logic
    if (timerActive)
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            timerActive = false;
            ShowGameOver(false);
        }
        timerText.text = "TIME LEFT: " + FormatTime(remainingTime);
    }
}


    private void ShowGameOver(bool win)
    {
        if (gameOverShown) return;
        gameOverShown = true;

        gameOverText.gameObject.SetActive(true); // <-- Ensure the text is visible

        if (win) {
            CameraManager.SwitchCamera(camOut);
            gameOverText.text = "INFECTED.";
        } else {
            gameOverText.text = "INFECTION INCOMPLETE!";
        }
        //Time.timeScale = 0f; // Pause all gameplay
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        int milliseconds = Mathf.FloorToInt((time * 100F) % 100F);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    void ChangeColor()
    {
        shapeRenderers = areas[currentAreaIndex].GetComponentsInChildren<SpriteShapeRenderer>();

        // Store original colors
        for (int i = 0; i < shapeRenderers.Length; i++)
        {
            shapeRenderers[i].color = targetColor;
        }
    }
}