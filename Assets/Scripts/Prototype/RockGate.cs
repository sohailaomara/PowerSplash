using System.Collections;
using UnityEngine;

/// <summary>
/// Ancient Ruins gate. Opens (scripted event) once the player holds every Ancient Pearl:
/// the slab sinks into the seabed, the rune light turns green and a Cinemachine
/// cinematic camera pans around the gate.
/// </summary>
public class RockGate : MonoBehaviour
{
    public Transform slab;
    public Light runeLight;
    public float openDistance = 11f;
    public float openSeconds = 4f;

    private bool opened;

    private void Start()
    {
        if (runeLight != null) runeLight.color = new Color(1f, 0.25f, 0.15f);
    }

    private void Update()
    {
        if (!opened && PlayerInventory.Pearls >= PlayerInventory.PearlsRequired)
            StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        opened = true;
        GameHUD.Message("All pearls found - the ancient gate rumbles open!", 4f);
        if (CameraDirector.Instance != null && slab != null)
            CameraDirector.Instance.PlayCinematic(slab, 5.5f, new Vector3(-22f, 6f, -20f), 12f);

        yield return new WaitForSeconds(0.8f);

        Vector3 start = slab.position;
        float t = 0f;
        while (t < openSeconds)
        {
            t += Time.deltaTime;
            float e = Mathf.SmoothStep(0f, 1f, t / openSeconds);
            float shake = Mathf.Sin(Time.time * 40f) * 0.15f * (1f - e);
            slab.position = start + Vector3.down * (openDistance * e) + new Vector3(0f, 0f, shake);
            if (runeLight != null) runeLight.color = Color.Lerp(new Color(1f, 0.25f, 0.15f), new Color(0.3f, 1f, 0.4f), e);
            yield return null;
        }

        Fx.Burst(start + Vector3.down * 4f, new Color(0.7f, 0.65f, 0.55f, 0.8f), 40, 1.2f, 4f, 2f);
        GameHUD.Message("The Ancient Ruins are open - something golden glows inside...", 4f);
    }
}
