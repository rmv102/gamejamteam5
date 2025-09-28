using UnityEngine;
using UnityEngine.InputSystem; // new input system
using UnityEngine.SceneManagement;

public class ReloadToMenu : MonoBehaviour
{
    void Update()
    {
        // Check if the R key was pressed this frame
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Menu_v3");
        }
    }
}
