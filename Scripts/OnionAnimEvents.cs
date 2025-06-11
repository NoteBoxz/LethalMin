using System.Collections;
using System.Collections.Generic;
using LethalMin;
using TMPro;
using UnityEngine;

public class OnionAnimEvents : MonoBehaviour
{
    public AudioSource audio = null!;
    public AudioSource audioLooped = null!;
    public OnionSoundPack pack = null!;

    public void OnEnable()
    {
        audioLooped.mute = true;
    }
    public void PlayAudioLooped()
    {
        audioLooped.mute = false;
        audioLooped.Play();
        LethalMin.LethalMin.Logger.LogInfo($"Playing looped audio: {audioLooped.clip.name}");
    }
    public void StopAudioLooped()
    {
        audioLooped.Stop();
        audioLooped.mute = true;
        LethalMin.LethalMin.Logger.LogInfo($"Stopping looped audio: {audioLooped.clip.name}");
    }
    public void PlayLandingAudio()
    {
        audio.PlayOneShot(pack.LandSound);
        LethalMin.LethalMin.Logger.LogInfo($"Playing landing audio: {pack.LandSound.name}");
    }
    public void PlayExtendLegsAudio()
    {
        audio.PlayOneShot(pack.LegsPopOutSound);
        LethalMin.LethalMin.Logger.LogInfo($"Playing extend legs audio: {pack.LegsPopOutSound.name}");
    }
    public void PlayRetractLegsAudio()
    {
        audio.PlayOneShot(pack.LegsPopInSound);
        LethalMin.LethalMin.Logger.LogInfo($"Playing retract legs audio: {pack.LegsPopInSound.name}");
    }
    public void ShowOnionMesh()
    {
        foreach (Renderer render in GetComponentsInChildren<Renderer>())
        {
            render.enabled = true;
        }
    }
    public void HideOnionMesh()
    {
        foreach (Renderer render in GetComponentsInChildren<Renderer>())
        {
            render.enabled = false;
        }
    }
}
