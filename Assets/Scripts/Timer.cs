using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public ScoreTracker scoreTracker; // Assign in Inspector
    public Text gameOverText; // Assign in Inspector (the overlay message)
    private bool gameOverShown = false;
    public Text timerText; // Assign this in the Inspector
    private float remainingTime = 20f; // 2 minutes in seconds
    private bool timerActive = true;

    void Update()
    {

        // Get infection percent from ScoreTracker
        int current = GameObject.FindGameObjectsWithTag(scoreTracker.particleTag).Length;
        float percent = (float)current / Mathf.Max(1, scoreTracker.maxParticles);

        if (percent >= 0.8f)
        {
            ShowGameOver(true);
            return;
        }

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

        if (win)
            gameOverText.text = "YOU WIN! INFECTED!";
        else
            gameOverText.text = "YOU LOSE! INFECTION INCOMPLETE!";
        Time.timeScale = 0f; // Pause all gameplay
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);
        int milliseconds = Mathf.FloorToInt((time * 100F) % 100F);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }
}