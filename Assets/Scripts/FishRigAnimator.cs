using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Plays the swim clip that comes with a rigged fish model (the skeleton in fish.fbx or Fish 3_2.fbx).
/// The clip loops, and plays faster when the fish swims faster. It only moves the fish's bones, never the fish itself,
/// so FishSwimAnimator and the AI keep control of where the fish goes.
/// Fish without a skeleton use FishPartAnimator (tail fin) instead.
/// </summary>
[RequireComponent(typeof(Animator))]
public class FishRigAnimator : MonoBehaviour
{
    public AnimationClip clip;
    public float baseSpeed = 1f;

    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private FishSwimAnimator swim;

    private void OnEnable()
    {
        if (clip == null) return;
        Animator animator = GetComponent<Animator>();
        animator.applyRootMotion = false;

        graph = PlayableGraph.Create("Fish Rig " + name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Swim", animator);
        playable = AnimationClipPlayable.Create(graph, clip);
        output.SetSourcePlayable(playable);
        graph.Play();
        playable.SetTime(Random.value * clip.length);   // fish do not all beat in step
        swim = GetComponent<FishSwimAnimator>();
    }

    private void Update()
    {
        if (!graph.IsValid()) return;
        if (playable.GetTime() >= clip.length) playable.SetTime(0.0);
        float speed = swim != null ? 0.7f + 1.0f * swim.Speed01 : 1f;
        playable.SetSpeed(speed * baseSpeed);
    }

    private void OnDisable()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
