using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundPanel : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup _audioMixerGroup;
    [SerializeField] private Slider _musicVolume;
    [SerializeField] private Slider _soundVolume;

    public float Music => _musicVolume.value;
    public float Sound => _soundVolume.value;

    public Action Changed;

    private void OnEnable()
    {
        _musicVolume.onValueChanged.AddListener(MusicVolume);
        _soundVolume.onValueChanged.AddListener(SoundVolume);
    }

    private void OnDisable()
    {
        _musicVolume.onValueChanged.RemoveListener(MusicVolume);
        _soundVolume.onValueChanged.RemoveListener(SoundVolume);
    }

    public void MusicVolume(float volume)
    {
        _audioMixerGroup.audioMixer.SetFloat("Music", Mathf.Log10(volume) * 20);

        Changed?.Invoke();
    }

    public void SoundVolume(float volume)
    {
        _audioMixerGroup.audioMixer.SetFloat("UI", Mathf.Log10(volume) * 20);

        Changed?.Invoke();
    }

    public void Load(float music, float sound)
    {
        _musicVolume.value = music;
        _soundVolume.value = sound;
    }
}