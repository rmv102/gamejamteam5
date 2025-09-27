using UnityEngine;
using UnityEngine.UI;

public class ScoreTracker : MonoBehaviour
{
    [SerializeField] Image progressBar;      // Image Type = Filled
    [SerializeField] Text feedbackText;
    public string particleTag = "InfectedParticle";
    public int maxParticles = 300; // your cap

    void Update()
    {
        int current = GameObject.FindGameObjectsWithTag(particleTag).Length;
        float percentGenerated = Mathf.Clamp01((float)current / Mathf.Max(1, maxParticles));
        progressBar.fillAmount = percentGenerated;

        int percent = Mathf.RoundToInt(percentGenerated * 100f);

        if (percent <= 20)
            feedbackText.text = "Status: Healthy";
        else if (percent <= 40)
            feedbackText.text = "Status: Minor Infection";
        else if (percent <= 60)
            feedbackText.text = "Status: Spreading Infection";
        else if (percent <= 80)
            feedbackText.text = "Status: Severe Infection";
        else
            feedbackText.text = "Status: Fully Infected!";
    }

}