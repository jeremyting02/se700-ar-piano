using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoSlide : MonoBehaviour
{

    [SerializeField] float _depth = 0.8f; // aka length

    [SerializeField] Transform _background;
    [SerializeField] Transform _gridLineParent;
    [SerializeField] GameObject _gridLineExample;

    [SerializeField] Color _whiteKeyColour = Color.white;
    [SerializeField] float _whiteKeyWidth = 0.022f;
    [SerializeField] float _whiteKeySpacing = 0.0015f;
    [SerializeField] int _whiteKeyCount = 52;
    [SerializeField] float _whiteKeyHover = 0.0001f;

    [SerializeField] Color _blackKeyColour = Color.blue;
    [SerializeField] float _blackKeyWidth = 0.0125f;
    [SerializeField] float[] _blackToWhiteKeyOffsets = new float[7] { 0.013f, -1, 0.010f, 0.012f, -1, 0.009f, 0.011f };
    [SerializeField] float _blackKeyHover = 0.0002f;

    KeyColumn[] _keyColumns;

    [SerializeField] float _unitLength = 0.1f;

    bool _metronomeMode = false;
    AudioSource _metronome;
    float _last_tick_time;
    [SerializeField] int _metronomePerBeat = 1;
    [SerializeField] float _metronomeOffset = -0.2f;

    [SerializeField] BeatBar _beatBar;
    [SerializeField] float _barLength = 0.001f;
    [SerializeField] float _barHover = 0.0003f;

    bool _calibrationMode = false;
    [SerializeField] Color _calibrationKeyColour = Color.blue;
    [SerializeField] int[] _calibrationKeys = new int[] { 37, 40, 42, 45 };

    float width;

    private void Awake()
    {
        // Calculate total width
        width = _whiteKeyWidth * _whiteKeyCount + _whiteKeySpacing * (_whiteKeyCount - 1);

        // Set background size, position and material
        _background.localScale = new Vector3(width, _depth, 1);
        _background.localPosition += new Vector3(0, 0, _depth / 2);
        _background.GetComponent<Renderer>().material.mainTextureScale = new Vector2(_whiteKeyCount, 1);

        CreateGridLines();
    }

    void CreateGridLines()
    {
        for (int i = 0; i < _whiteKeyCount; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Calculate the x position based on the interval and index
            float xPos = i * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2;

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, _barHover, _depth / 2);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_whiteKeyWidth / 10, 1, _depth);

            // Optionally, you can customize each clone (e.g., change color)
            // clone.GetComponent<Renderer>().material.color = Color.Lerp(Color.white, Color.black, (float)i / _whiteKeyCount);
        }
    }

}
