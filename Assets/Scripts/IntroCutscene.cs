using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroCutscene : MonoBehaviour
{
    [SerializeField] private VideoPlayer video;
    [SerializeField] private string fileName = "intro.mp4";    // inside Assets/StreamingAssets
    [SerializeField] private string nextScene = "MainScene";
    [SerializeField] private InputActionReference skipAction;
    private bool done;

    void Start()
    {
        video.url = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        video.loopPointReached += OnVideoEnd;
        video.SetDirectAudioVolume(0, GameSettings.Master * GameSettings.Music);
        video.Play();
    }

    void OnVideoEnd(VideoPlayer vp) => Finish();
    public void Skip() => Finish();             // hook the Skip button's On Click to this

    void Update()
    {
        if (skipAction.action.WasPressedThisFrame()) Finish();
    }

    void Finish()
    {
        if (done) return;                       
        done = true;
        SceneManager.LoadScene(nextScene);
    }
}