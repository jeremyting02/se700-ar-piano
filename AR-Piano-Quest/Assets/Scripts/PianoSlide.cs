using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoSlide : MonoBehaviour
{

    [SerializeField] float _depth = 0.8f; // aka length

    [SerializeField] Transform _background;
    [SerializeField] Transform _gridLineParent;
    [SerializeField] GameObject _gridWhiteLineExample;
    [SerializeField] GameObject _gridBlackLineExample;
    [SerializeField] GameObject _gridBeatLineExample;

    [SerializeField] Color _whiteKeyColour = Color.white;
    [SerializeField] float _whiteKeyWidth = 0.022f;
    [SerializeField] float _whiteKeySpacing = 0.0015f;
    [SerializeField] int _whiteKeyCount = 52;
    [SerializeField] float _whiteKeyHover = 0.0001f;

    [SerializeField] Color _blackKeyColour = Color.blue;
    [SerializeField] float _blackKeyWidth = 0.0125f;
    [SerializeField] float[] _blackToWhiteKeyOffsets = new float[7] { 0.013f, -1, 0.010f, 0.012f, -1, 0.009f, 0.011f };
    [SerializeField] float _blackKeyHover = 0.0002f;

    [SerializeField] float _unitLength = 0.1f;
    [SerializeField] int _metronomePerBeat = 1;
    [SerializeField] float _metronomeOffset = -0.2f;

    [SerializeField] BeatBar _beatBar;
    [SerializeField] float _barHover = 0.0003f;

    int beatsPerBar = 4;
    int barsPerSlide = 2;

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
        // White Lines
        for (int i = 0; i < _whiteKeyCount; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridWhiteLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Calculate the x position based on the interval and index
            float xPos = i * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2;

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, _barHover, _depth / 2);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_whiteKeyWidth / 10, 1, _depth);
        }

        // Black Lines
        for (int i = 0; i < _whiteKeyCount; i++)
        {
            if (_blackToWhiteKeyOffsets[i % 7] < 0)
                continue;

            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridBlackLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Calculate the x position based on the interval and index
            float xPos = i * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[i % 7];

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, _barHover, _depth / 2);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_blackKeyWidth / 10, 1, _depth);
        }

        // Beat Lines
        for (int i = 0; i < beatsPerBar * barsPerSlide + 1; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridBeatLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(0, _barHover * 2, _depth * i / (beatsPerBar * barsPerSlide));

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(width, 1, _whiteKeyWidth / 10);
        }

        // Bar Lines
        for (int i = 0; i < barsPerSlide + 1; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridBeatLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(0, _barHover * 2, _depth * i / barsPerSlide);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(width, 1, _whiteKeyWidth / 5);
        }
    }

}
