using TMPro;
using UnityEngine;
using System.Collections;

public class FloorTitle : MonoBehaviour
{
    [SerializeField] private GameController game;
    [SerializeField] private TMP_Text text;
    [SerializeField] private TMP_Text smallText;
    [SerializeField] private float holdTime = 1.5f, fadeTime = 0.5f;

    void OnEnable() => game.FloorChanged += Show;
    void OnDisable() => game.FloorChanged -= Show;
    void Start() => Show(game.Floor);          // floor 1 at the start of the run

    void Show(int floor)
    {
        text.text = $"FLOOR {floor}";
        StopAllCoroutines();
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        text.alpha = 1f;
        smallText.alpha = 1f;
        yield return new WaitForSecondsRealtime(holdTime);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / fadeTime)
        {
            text.alpha = Mathf.Lerp(1f, 0f, t);
            smallText.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;          
        }
        text.alpha = 0f;        
        smallText.alpha = 0f;
    }
}