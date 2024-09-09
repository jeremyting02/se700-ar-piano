using System.Collections;
using UnityEngine;

public class SlideBar : MonoBehaviour
{
    float _barMoveDepth;
    float _beatLength;
    float _barThickness;
    float _beatsPerSecond;

    [SerializeField] GameObject _exampleVisual;
    [SerializeField] PianoSlide _pianoSlide;

    GameObject _barVisual;
    float _barStartTime;
    bool _transitionStarted;

    public void Initialise(float time, float width, float barMoveDepth, float beatLength, float barThickness, float barHover, float beatsPerSecond)
    {
        transform.localPosition += new Vector3(0, barHover, 0);
        transform.localScale = new Vector3(width, 1, 1);

        _barMoveDepth = barMoveDepth;
        _beatLength = beatLength;
        _barThickness = barThickness;
        _beatsPerSecond = beatsPerSecond;

        DoReset(time);
    }

    public void Elapse(float time)
    {
        if (_barVisual == null)
        {
            _barVisual = Instantiate(_exampleVisual, transform);
            _barVisual.SetActive(true);
            _barStartTime = time;
        }

        float elapsedTime = time - _barStartTime;
        float zPos = elapsedTime * _beatLength;

        if (zPos >= _barMoveDepth - _beatLength && !_transitionStarted)
        {
            // Start transition on the last beat
            _pianoSlide.MoveNoteLinesForTransition();
            StartCoroutine(LerpBarPosition(1 / SongController.GetSong().getBPS, time + 1 / SongController.GetSong().getBPS)); // Transition duration based on beats per second
            _transitionStarted = true;
        }
        else if (!_transitionStarted)
        {
            _barVisual.transform.localPosition = new Vector3(0, 0, zPos);
            _barVisual.transform.localScale = new Vector3(1, 1, _barThickness);
        }
    }

    IEnumerator LerpBarPosition(float duration, float time)
    {
        float startZ = _barVisual.transform.localPosition.z;
        float targetZ = 0f;  // Reset to start position
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float zPos = Mathf.Lerp(startZ, targetZ, elapsed / duration);
            _barVisual.transform.localPosition = new Vector3(0, 0, zPos);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _barVisual.transform.localPosition = new Vector3(0, 0, targetZ);
        _barStartTime = time;  // Reset the start time
        _transitionStarted = false;
    }

    public void DoReset(float time)
    {
        if (_barVisual != null)
        {
            Destroy(_barVisual);
            _barVisual = null;
        }

        _barVisual = Instantiate(_exampleVisual, transform);
        _barVisual.SetActive(true);
        _barStartTime = time;

        Elapse(time);
    }
}
