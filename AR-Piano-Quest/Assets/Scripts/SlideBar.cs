using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideBar : MonoBehaviour
{
    int _maxBarCount;
    float _depth;
    float _beatLength;
    float _barThickness;

    [SerializeField] GameObject _exampleVisual;

    public class Bar
    {
        float _startTime;
        public float StartTime { get { return _startTime; } }
        float _length;
        public float Length { get { return _length; } }

        GameObject _visual;

        public Bar(float startTime, float length)
        {
            _startTime = startTime;
            _length = length;
        }

        public void Show(GameObject exampleVisual, Transform parent, bool metronomeMode)
        {
            _visual = Instantiate(exampleVisual, parent);
            _visual.SetActive(metronomeMode);
        }

        public void NewMetronomeMode(bool metronomeMode)
        {
            if (_visual.activeInHierarchy != metronomeMode)
            {
                _visual.SetActive(metronomeMode);
            }
        }

        public bool MoveUntilGone(float time, float depth, float beatLength, int maxBarCount)
        {
            // Update: Now we use (_startTime + time) to make the bars move forwards
            float visualStart = Mathf.Max(0, (_startTime + time) * beatLength);
            float visualEnd = Mathf.Min(depth, ((_startTime + time) * beatLength) + _length);
            float visualLength = Mathf.Max(visualEnd - visualStart, 0);

            if (visualLength > 0)
            {
                // Set the new position and scale based on forward movement
                _visual.transform.localPosition = new Vector3(0, 0, Mathf.Lerp(visualStart, visualEnd, 0.5f));
                _visual.transform.localScale = new Vector3(1, 1, visualLength);

                return true;
            }
            else
            {
                Destroy(_visual);
                _startTime -= maxBarCount;  // Move start time backwards when the bar goes off the screen
                return false;
            }
        }

        public void DestroyVisual()
        {
            Destroy(_visual);
        }
    }

    List<Bar> _barsToShow = new List<Bar>();
    List<Bar> _barsShowing = new List<Bar>();


    public void Initialise(float time, float width, float depth, float beatLength, float barThickness, float barHover)
    {
        transform.localPosition += new Vector3(0, barHover, 0);
        transform.localScale = new Vector3(width, 1, 1);
        _depth = depth;
        _beatLength = beatLength;
        _barThickness = barThickness;

        DoReset(time);
    }

    public void Elapse(float time)
    {
        // Check to show next bar on the back of the piano roll
        while (_barsToShow.Count > 0 && _barsToShow[0].StartTime * _beatLength < (time * _beatLength) + _depth)
        {
            Bar showNote = _barsToShow[0];
            _barsShowing.Add(showNote);
            _barsToShow.RemoveAt(0);

            showNote.Show(_exampleVisual, transform, true);
        }

        // Move all notes on the piano roll until they're gone
        int i = 0;
        while (i < _barsShowing.Count)
        {
            bool gone = !_barsShowing[i].MoveUntilGone(time, _depth, _beatLength, _maxBarCount);

            if (gone)
            {
                _barsToShow.Add(_barsShowing[i]);
                _barsShowing.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    public void SetMetronomeMode()
    {
        foreach (Bar bar in _barsShowing)
        {
            bar.NewMetronomeMode(true);
        }
    }

    public void DoReset(float time)
    {
        foreach (Bar bar in _barsToShow) bar.DestroyVisual();
        foreach (Bar bar in _barsShowing) bar.DestroyVisual();

        _barsToShow.Clear();
        _barsShowing.Clear();

        _maxBarCount = (int)(_depth / _beatLength);
        for (int i = -_maxBarCount; i < 0; i++)
        {
            _barsToShow.Add(new Bar(i, _barThickness));
        }

        Elapse(time);
        SetMetronomeMode();
    }
}
