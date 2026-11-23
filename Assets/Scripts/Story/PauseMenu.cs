using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// In-game pause menu (ESC): resume, restart the story from Level 1, return to the main menu, or quit.
/// It only opens while the game is actually running, so it never fights with dialogue, the lose/win screens or loading.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    private GameObject canvasGO;
    private MenuList menu;
    private bool open;
    private float prevTimeScale = 1f;
    private CursorLockMode prevLock;
    private bool prevCursorVisible;

    private void Awake() { Build(); }

    private void Build()
    {
        Canvas canvas = StoryUI.MakeCanvas("Pause Canvas", 170, transform);
        canvasGO = canvas.gameObject;
        Transform root = canvasGO.transform;

        StoryUI.MakeFullscreenImage("Dim", root, new Color(0f, 0.04f, 0.08f, 0.8f));
        Text title = StoryUI.MakeText("Title", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(900f, 140f), 100, TextAnchor.MiddleCenter);
        title.text = "PAUSED";
        title.color = new Color(1f, 0.9f, 0.4f);

        string[] names = { "RESUME", "RESTART FROM LEVEL 1", "MAIN MENU", "QUIT GAME" };
        System.Action[] actions = { Resume, RestartGame, GoToMainMenu, QuitGame };
        menu = new MenuList(root, names, actions, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), 82f, new Vector2(900f, 70f), 46);

        canvasGO.SetActive(false);
    }

    private void Update()
    {
        if (open)
        {
            if (StoryUI.EscapePressed()) Resume();
            else menu.Tick();
            return;
        }

        if (!StoryStateManager.InLevel) return;
        if (StoryUI.EscapePressed() && Time.timeScale > 0.01f && !DialogueManager.IsBusy && !CinematicManager.Busy && !LevelTransitionManager.Loading)
            Open();
    }

    private void Open()
    {
        open = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        prevLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menu.Reset();
        canvasGO.SetActive(true);
    }

    private void Resume()
    {
        open = false;
        canvasGO.SetActive(false);
        Time.timeScale = prevTimeScale <= 0.01f ? 1f : prevTimeScale;
        Cursor.lockState = prevLock;
        Cursor.visible = prevCursorVisible;
    }

    private void RestartGame()
    {
        Resume();
        PlayerProgress.Clear();   // no saved progress = story flags are cleared when Level 1 opens
        PlayerInventory.Reset();
        if (AbilitySystem.Instance != null) AbilitySystem.Instance.ResetAll();
        SceneManager.LoadScene(PlayerProgress.SceneIndexForTier(1));
    }

    private void GoToMainMenu()
    {
        Resume();
        PlayerProgress.Clear();
        PlayerInventory.Reset();
        SceneManager.LoadScene(0);
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
