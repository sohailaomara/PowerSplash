using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Starting menu scene. START GAME begins the story at Level 1; the level buttons let the player open any level
/// in narrative order (the story up to that level is assumed, see StoryStateManager); QUIT exits.
/// The UI is built at runtime, like the in-game HUD.
/// </summary>
public class MainMenu : MonoBehaviour
{
    private MenuList menu;
    private RectTransform[] shafts;
    private float[] shaftSpeeds;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Build();
    }

    private void Build()
    {
        Canvas canvas = StoryUI.MakeCanvas("Main Menu Canvas", 10, transform);
        Transform root = canvas.transform;
        Vector2 mid = new Vector2(0.5f, 0.5f);

        StoryUI.MakeFullscreenImage("Water", root, new Color(0.01f, 0.12f, 0.22f, 1f));
        for (int g = 0; g < 6; g++)
            StoryUI.MakeImage("Surface Glow " + g, root, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(2400f, 1000f - g * 150f), new Color(0.15f, 0.55f, 0.7f, 0.07f));

        // Soft light shafts drifting through the water
        shafts = new RectTransform[4];
        shaftSpeeds = new float[4];
        for (int i = 0; i < shafts.Length; i++)
        {
            Image s = StoryUI.MakeImage("Light Shaft " + i, root, mid, new Vector2(-700f + i * 480f, 100f), new Vector2(140f + i * 30f, 1700f), new Color(0.7f, 0.95f, 1f, 0.03f));
            s.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -14f);
            shafts[i] = s.rectTransform;
            shaftSpeeds[i] = 6f + i * 3f;
        }

        Text title = StoryUI.MakeText("Title", root, mid, new Vector2(0f, 330f), new Vector2(1600f, 180f), 150, TextAnchor.MiddleCenter);
        title.text = "POWER SPLASH";
        title.color = new Color(1f, 0.87f, 0.3f);

        Text subtitle = StoryUI.MakeText("Subtitle", root, mid, new Vector2(0f, 215f), new Vector2(1600f, 70f), 52, TextAnchor.MiddleCenter);
        subtitle.text = "The Lost Little Fish";
        subtitle.color = new Color(0.6f, 0.95f, 1f);

        Text tagline = StoryUI.MakeText("Tagline", root, mid, new Vector2(0f, 150f), new Vector2(1600f, 50f), 30, TextAnchor.MiddleCenter);
        tagline.text = "Eat smaller fish. Grow. Find your way home.";
        tagline.color = new Color(1f, 1f, 1f, 0.75f);

        string[] names =
        {
            "START GAME",
            "LEVEL 1  -  " + GameHUD.LocationName(1),
            "LEVEL 2  -  " + GameHUD.LocationName(2),
            "LEVEL 3  -  " + GameHUD.LocationName(3),
            "QUIT"
        };
        System.Action[] actions = { StartGame, delegate { OpenLevel(1); }, delegate { OpenLevel(2); }, delegate { OpenLevel(3); }, QuitGame };
        menu = new MenuList(root, names, actions, mid, new Vector2(0f, 40f), 78f, new Vector2(1000f, 66f), 44);

        Text help = StoryUI.MakeText("Controls", root, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1700f, 40f), 24, TextAnchor.LowerCenter);
        help.text = "WASD swim   |   R talk / interact   |   J journal   |   ESC pause   |   Arrow keys + Enter or mouse in menus";
        help.color = new Color(1f, 1f, 1f, 0.6f);
    }

    private void Update()
    {
        if (menu != null) menu.Tick();
        for (int i = 0; shafts != null && i < shafts.Length; i++)
        {
            Vector2 p = shafts[i].anchoredPosition;
            p.x += shaftSpeeds[i] * Time.deltaTime;
            if (p.x > 1300f) p.x = -1300f;
            shafts[i].anchoredPosition = p;
        }
    }

    private void StartGame() { OpenLevel(1); }

    private void OpenLevel(int tier)
    {
        PlayerProgress.Clear();   // fresh story; the story flags for earlier levels are assumed by StoryStateManager
        PlayerInventory.Reset();
        SceneManager.LoadScene(PlayerProgress.SceneIndexForTier(tier));
    }

    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
