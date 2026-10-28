using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
public class CutScenePlayer : MonoBehaviour
{
    [SerializeField] private VideoPlayer video;
    [SerializeField] private string fileName = "intro.mp4";       
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private string nextScene = "MainScene";       // loaded when finished, if On Finished is empty
    [SerializeField] private InputActionReference skipAction;
    [SerializeField] private GameObject screen;                    
    [SerializeField] private AudioSource musicToPause;             
    [SerializeField] private UnityEvent onFinished;

    private bool playing;
    private System.Action then;   // set from code: Play(ContinueAfterBoss)

    void Start()
    {
        video.loopPointReached += OnVideoEnd;
        video.errorReceived += (vp, message) => { Debug.LogWarning($"Cutscene {fileName}: {message}"); Finish(); };
        if (playOnStart) Play();
        else if (screen != null) screen.SetActive(false);
    }

    public void Play(System.Action whenDone)
    {
        then = whenDone;
        Play();
    }

    public void Play()
    {
        if (playing) return;
        playing = true;
        video.url = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        video.SetDirectAudioVolume(0, GameSettings.Master * GameSettings.Music);
        if (screen != null) screen.SetActive(true);
        if (musicToPause != null) musicToPause.Pause();
        GamePause.Add(PauseReason.Cutscene);
        startedAt = Time.unscaledTime;
        video.Play();
    }

    private float startedAt;
    const float startTimeout = 3f;   // a missing or empty file never starts and never ends: don't strand the player

    void OnVideoEnd(VideoPlayer vp) => Finish();
    public void Skip() => Finish();                    // for a Skip button

    void Update()
    {
        if (!playing) return;
        if (skipAction != null && skipAction.action.WasPressedThisFrame()) Finish();
        else if (!video.isPlaying && Time.unscaledTime - startedAt > startTimeout) Finish();
    }

    void Finish()
    {
        if (!playing) return;                          // video end + skip in the same frame: only once
        playing = false;
        video.Stop();
        if (screen != null) screen.SetActive(false);
        if (musicToPause != null) musicToPause.UnPause();
        GamePause.Remove(PauseReason.Cutscene);

        if (then != null) { var t = then; then = null; t(); }
        else if (onFinished.GetPersistentEventCount() > 0) onFinished.Invoke();
        else if (!string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(nextScene);
    }

    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);   // usable from On Finished
}
