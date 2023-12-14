using Core.Services.Audio;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using AudioType = Core.Services.Audio.AudioType;

[RequireComponent(typeof(Button))]
public class ClickSound : MonoBehaviour
{
    [SerializeField] private AudioBase sound;
    [SerializeField] [Range(0, 1)] private float volume = 1;
    private AudioSystem _audioSystem;
    private Button _button;

    private void Start()
    {
        if (sound == null)
        {
            return;
        }

        _button = GetComponent<Button>();
        InjectSound();
    }

    [Inject]
    private void Construct(AudioSystem audioSystem)
    {
        _audioSystem = audioSystem;
    }

    private void InjectSound()
    {
        _button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        _audioSystem.PlayClip(sound, AudioType.SFX, volume);
    }
}