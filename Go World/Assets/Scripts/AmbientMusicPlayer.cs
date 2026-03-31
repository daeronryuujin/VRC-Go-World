// AmbientMusicPlayer.cs
// Background ambient music controller for the Go lounge.
// Uses a Unity AudioSource with a pre-assigned AudioClip (inspector).
// Volume can be adjusted by players via an in-world slider interaction.
//
// Volume is NOT synced — each client controls their own local volume.
// Play/pause state is NOT synced — music plays locally for atmosphere.
//
// No List<T>, no LINQ, no try/catch, no generics.

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AmbientMusicPlayer : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector references
    // -----------------------------------------------------------------------
    [Header("Audio")]
    [Tooltip("AudioSource component with ambient music clip assigned.")]
    public AudioSource audioSource;

    [Header("Volume Settings")]
    [Tooltip("Initial volume (0..1).")]
    [Range(0f, 1f)]
    public float defaultVolume = 0.3f;

    [Tooltip("Volume change step when using the volume up/down interact buttons.")]
    [Range(0.01f, 0.25f)]
    public float volumeStep = 0.1f;

    // -----------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------
    private void Start()
    {
        if (audioSource == null) return;
        audioSource.volume = defaultVolume;
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    // -----------------------------------------------------------------------
    // Public API — call from UI buttons or SendCustomEvent
    // -----------------------------------------------------------------------

    /// <summary>Increase volume by one step (clamped to 1).</summary>
    public void VolumeUp()
    {
        if (audioSource == null) return;
        audioSource.volume = Mathf.Clamp01(audioSource.volume + volumeStep);
    }

    /// <summary>Decrease volume by one step (clamped to 0).</summary>
    public void VolumeDown()
    {
        if (audioSource == null) return;
        audioSource.volume = Mathf.Clamp01(audioSource.volume - volumeStep);
    }

    /// <summary>Mute / unmute toggle.</summary>
    public void ToggleMute()
    {
        if (audioSource == null) return;
        audioSource.mute = !audioSource.mute;
    }

    /// <summary>Pause music.</summary>
    public void Pause()
    {
        if (audioSource == null) return;
        audioSource.Pause();
    }

    /// <summary>Resume music.</summary>
    public void Resume()
    {
        if (audioSource == null) return;
        audioSource.UnPause();
    }

    // -----------------------------------------------------------------------
    // Interact — cycles through volume presets if placed on an interact object
    // -----------------------------------------------------------------------
    public override void Interact()
    {
        ToggleMute();
    }
}
