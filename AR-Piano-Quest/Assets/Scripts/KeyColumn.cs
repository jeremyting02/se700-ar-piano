using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class KeyColumn : MonoBehaviour
{
    float _depth;
    float _beatLength;

    [SerializeField] GameObject _exampleVisual;

    GameObject _calibrationVisual;

    [Serializable]
    public class Note
    {
        float _startTime;
        public float StartTime { get { return _startTime; } }
        float _length;
        public float Length { get { return _length; } }

        GameObject _visual;

        public Note(float startTime, float length)
        {
            _startTime = startTime;
            _length = length;
        }

        public void Show(GameObject exampleVisual, Transform parent)
        {
            _visual = Instantiate(exampleVisual, parent);
            _visual.SetActive(true);
        }

        public bool MoveUntilGone(float time, float depth, float beatLength)
        {
            float visualStart = Mathf.Max(0, (_startTime - time) * beatLength);
            float visualEnd = Mathf.Min(depth, (_startTime + _length - time) * beatLength);
            float visualLength = Mathf.Max(visualEnd - visualStart, 0);

            if (visualLength > 0)
            {
                _visual.transform.localPosition = new Vector3(0, 0, Mathf.Lerp(visualStart, visualEnd, 0.5f));
                _visual.transform.localScale = new Vector3(1, 1, visualLength);
                return true;
            }
            else
            {
                Destroy(_visual);
                return false;
            }
        }

        public void DestroyVisual()
        {
            Destroy(_visual);
        }
    }

    List<Note> _notesToShow = new List<Note>();
    List<Note> _notesShowing = new List<Note>();
    List<Note> _notesShown = new List<Note>();

    public void Initialise(Vector3 localPosition, float width, float depth, float beatLength, Color colour)
    {
        transform.localPosition = localPosition;
        transform.localScale = new Vector3(width, 1, 1);
        _depth = depth;
        _beatLength = beatLength;
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) renderer.material.color = colour;

        gameObject.SetActive(true);
    }

    public void CreateCalibration(Color colour)
    {
        _calibrationVisual = Instantiate(_exampleVisual, transform);
        _calibrationVisual.transform.localPosition = new Vector3(0, 0, _depth / 2);
        _calibrationVisual.transform.localScale = new Vector3(1, 1, _depth);
        foreach (MeshRenderer renderer in _calibrationVisual.GetComponentsInChildren<MeshRenderer>(true)) renderer.material.color = colour;
    }

    public void EnableCalibration(bool enabled)
    {
        if (_calibrationVisual)
        {
            _calibrationVisual.SetActive(enabled);
        }
    }

    public void Elapse(float time)
    {
        // Check to show next note on the back of the piano roll
        while (_notesToShow.Count > 0 && _notesToShow[0].StartTime * _beatLength < (time * _beatLength) + _depth)
        {
            Note showNote = _notesToShow[0];
            _notesShowing.Add(showNote);
            _notesToShow.RemoveAt(0);

            showNote.Show(_exampleVisual, transform);
        }

        // Move all notes on the piano roll until they're gone
        int i = 0;
        while (i < _notesShowing.Count)
        {
            bool gone = !_notesShowing[i].MoveUntilGone(time, _depth, _beatLength);

            if (gone)
            {
                _notesShown.Add(_notesShowing[i]);
                _notesShowing.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }

    public void DoReset()
    {
        _notesToShow.Clear();
        foreach (Note note in _notesShowing) note.DestroyVisual();
        _notesShowing.Clear();
        _notesShown.Clear();
    }

    public void InputNote(float start, float length)
    {
        // Shorten notes
        float shortenLength = 0.1f;

        _notesToShow.Add(new Note(start, length - shortenLength));
    }
}
