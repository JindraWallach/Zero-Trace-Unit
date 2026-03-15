using UnityEngine;
using ZeroTrace.Audio;

public class MenuMusicController : MonoBehaviour
{
    [Header("Audio ID")]
    [SerializeField] private string menuMusicId = "menu_soundtrack";

    private PlaySoundCommand _musicHandle;
    private bool _started;

    private void Start()
    {
        _started = true;
        Play();
    }

    private void OnEnable()
    {
        // Start ještě neproběhl — AudioManager.Instance nemusí existovat, přeskočíme
        // Při návratu do menu (_started = true) už AudioManager existuje
        if (_started)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        _musicHandle = null; // při destruction scény jen vyčisti referenci, nevolej Stop
    }

    private void Stop()
    {
        if (_musicHandle == null) return;
        if (AudioManager.Instance == null) { _musicHandle = null; return; }

        AudioManager.Instance.Stop(_musicHandle);
        _musicHandle = null;
    }

    private void Play()
    {
        if (_musicHandle != null) return; // už hraje, nespouštět znovu

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[MenuMusicController] AudioManager.Instance is null!");
            return;
        }

        _musicHandle = AudioManager.Instance.Play(menuMusicId, Vector3.zero);
    }
}