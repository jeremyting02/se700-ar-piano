using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // For using the OrderBy method
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.TextCore.Text;

public class PianoSlide : MonoBehaviour
{


    class NoteDisplay
    {
        public float centreX;
        public float centreZ;
        public float startX;
        public float startZ;
        public float length;
        public float angle;

        public NoteDisplay(float centreX, float centreZ, float startX, float startZ, float length, float angle)
        {
            this.centreX = centreX;
            this.centreZ = centreZ;
            this.startX = startX;
            this.startZ = startZ;
            this.length = length;
            this.angle = angle;
        }
    }

    float _depth = 0.4f; // aka length, calculated from lengthPerBeat

    [SerializeField] Transform _background;
    [SerializeField] Transform _gridLineParent;
    [SerializeField] GameObject _gridWhiteLineExample;
    [SerializeField] GameObject _gridBlackLineExample;
    [SerializeField] GameObject _gridBeatLineExample;

    [SerializeField] Transform _noteLineParent;
    [SerializeField] GameObject _noteLineExample;
    [SerializeField] GameObject _noteStartExample;

    [SerializeField] float _whiteKeyWidth = 0.022f;
    [SerializeField] float _whiteKeySpacing = 0.0015f;
    [SerializeField] int _whiteKeyCount = 52;
    [SerializeField] float _whiteKeyHover = 0.0001f;

    [SerializeField] float _blackKeyWidth = 0.0125f;
    [SerializeField] float[] _blackToWhiteKeyOffsets = new float[7] { 0.013f, -1, 0.010f, 0.012f, -1, 0.009f, 0.011f }; // offset after a, none after b, after c, after d, none after e, after f, after g
    [SerializeField] float _blackKeyHover = 0.0002f;

    [SerializeField] SlideBar _slideBar;
    float _barThickness = 0.005f;
    [SerializeField] float _barHover = 0.0003f;

    Color _colorA = new Color(0.5f, 0, 1);
    Color _colorA1 = new Color(0.25f, 0, 0.5f);
    Color _colorB = new Color(1, 0, 1);
    Color _colorC = new Color(1, 0, 0);
    Color _colorC1 = new Color(0.5f, 0, 0);
    Color _colorD = new Color(1, 0.5f, 0);
    Color _colorD1 = new Color(0.5f, 0.25f, 0);
    Color _colorE = new Color(1, 1, 0);
    Color _colorF = new Color(0, 1, 0);
    Color _colorF1 = new Color(0, 0.5f, 0);
    Color _colorG = new Color(0, 1, 1);
    Color _colorG1 = new Color(0, 0.25f, 1);

    int _beatsPerBar = 4;
    int _barsToShowPerSlide = 4;
    int _barsToPlayPerSlide = 2;
    int _currentBar = 0;

    float _lengthPerBeat = 0.025f;

    float _backgroundWidth;

    private void Awake()
    {
        // Calculate total width
        _backgroundWidth = _whiteKeyWidth * _whiteKeyCount + _whiteKeySpacing * (_whiteKeyCount - 1);
        _depth = _lengthPerBeat * (_beatsPerBar * _barsToShowPerSlide);

        // Set background size, position and material
        _background.localScale = new Vector3(_backgroundWidth, _depth, 1);
        _background.localPosition += new Vector3(0, 0, _depth / 2);
        _background.GetComponent<Renderer>().material.mainTextureScale = new Vector2(_whiteKeyCount, 1);

        CreateGridLines();

        // Initialse slide bar
        _slideBar.Initialise(SongController._time, _backgroundWidth, _lengthPerBeat * (_beatsPerBar * _barsToPlayPerSlide), _lengthPerBeat, _barThickness, _barHover);

        DrawNoteLines(SongController.GetSong(), 0);
    }

    private void Update()
    {
        // Elapse time and move the slide bar
        if (!SongController._paused)
        {
            _slideBar.Elapse(SongController._time);
        }
    }

    public void DrawNextSlide()
    {
        // Move to the next set of bars
        _currentBar += _barsToPlayPerSlide;

        // Update the note lines for the next bars
        DrawNoteLines(SongController.GetSong(), _currentBar);
    }

    void CreateGridLines()
    {
        // Clear any existing note lines first
        foreach (Transform child in _gridLineParent)
        {
            Destroy(child.gameObject);
        }

        // White Lines
        for (int i = 0; i < _whiteKeyCount; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridWhiteLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Calculate the x position based on the interval and index
            float xPos = i * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2;

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, _barHover, _depth / 2);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_whiteKeyWidth / 10, 1, _depth);
        }

        // Colours on white lines
        for (int i = 0; i < _whiteKeyCount; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_noteLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Calculate the x position based on the interval and index
            float xPos = i * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2;

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, _barHover, -0.015f);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_whiteKeyWidth, 1, _whiteKeyWidth / 2);

            switch (i % 7)
            {
                case 0:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorA;
                    break;
                case 1:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorB;
                    break;
                case 2:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorC;
                    break;
                case 3:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorD;
                    break;
                case 4:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorE;
                    break;
                case 5:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorF;
                    break;
                case 6:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorG;
                    break;
                default:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.white;
                    break;
            }
        }

        // Black Lines
        for (int i = 0; i < _whiteKeyCount - 1; i++)
        {
            if (_blackToWhiteKeyOffsets[i % 7] < 0)
                continue;

            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridBlackLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Calculate the x position based on the interval and index
            float xPos = i * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[i % 7];

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, _barHover, _depth / 2);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_blackKeyWidth / 10, 1, _depth);
        }

        // Colours on black lines
        for (int i = 0; i < _whiteKeyCount - 1; i++)
        {
            if (_blackToWhiteKeyOffsets[i % 7] < 0)
                continue;

            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_noteLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Calculate the x position based on the interval and index
            float xPos = i * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[i % 7];

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, _barHover, -0.005f);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_whiteKeyWidth / 2, 1, _whiteKeyWidth / 2);

            switch (i % 7)
            {
                case 0:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorA1;
                    break;
                case 2:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorC1;
                    break;
                case 3:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorD1;
                    break;
                case 5:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorF1;
                    break;
                case 6:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorG1;
                    break;
                default:
                    clone.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.white;
                    break;
            }
        }

        // Beat Lines
        for (int i = 0; i < _beatsPerBar * _barsToShowPerSlide + 1; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridBeatLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(0, _barHover * 2, _depth * i / (_beatsPerBar * _barsToShowPerSlide));

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_backgroundWidth, 1, _whiteKeyWidth / 10);
        }

        // Bar Lines
        for (int i = 0; i < _barsToShowPerSlide + 1; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridBeatLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(0, _barHover * 2, _depth * i / _barsToShowPerSlide);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_backgroundWidth, 1, _whiteKeyWidth / 5);
        }

        // Preview Bar Cover
        GameObject previewBarCover = Instantiate(_gridBeatLineExample, _gridLineParent);

        previewBarCover.SetActive(true);

        previewBarCover.transform.localPosition = new Vector3(0, _barHover * 5, _depth - _depth * (_barsToShowPerSlide - _barsToPlayPerSlide) / _barsToShowPerSlide / 2);

        previewBarCover.transform.localScale = new Vector3(_backgroundWidth, 1, _lengthPerBeat * _beatsPerBar * (_barsToShowPerSlide - _barsToPlayPerSlide));

        previewBarCover.transform.GetChild(0).GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f);
    }

    void DrawNoteLines(SongController.Song song, int startBar)
    {
        // Clear any existing note lines first
        foreach (Transform child in _noteLineParent)
        {
            Destroy(child.gameObject);
        }

        // Calculate the start and end beat for the current set of bars
        int startBeat = startBar * _beatsPerBar;
        int endBeat = (startBar + _barsToShowPerSlide) * _beatsPerBar;

        // Loop through each note in the song that falls within the current bar range
        foreach (SongController.Song.SlideNote currentNote in song._slideInfo)
        {
            // Only process notes that start within the current bar range
            if (currentNote.startBeat >= startBeat && currentNote.startBeat < endBeat)
            {
                // Find the next note for drawing the line between current and next notes
                var nextNote = song._slideInfo.FirstOrDefault(n => n.startBeat > currentNote.startBeat);

                NoteDisplay noteDisplay;

                if (nextNote == null)
                {
                    noteDisplay = CalculateNoteDisplay(currentNote, currentNote);
                }
                else
                {
                    noteDisplay = CalculateNoteDisplay(currentNote, nextNote);

                    if (!currentNote.disconnect)
                    {
                        // Instantiate and set up note line visuals
                        GameObject clone = Instantiate(_noteLineExample, _noteLineParent);
                        clone.SetActive(true);
                        clone.transform.localPosition = new Vector3(noteDisplay.centreX, _barHover * 2, noteDisplay.centreZ - _depth * startBar / _barsToShowPerSlide);
                        clone.transform.localScale = new Vector3(_whiteKeyWidth / 5, 1, noteDisplay.length);
                        clone.transform.rotation = Quaternion.Euler(0, noteDisplay.angle, 0);
                        SetNoteColor(clone, currentNote.note);
                    }
                }

                // Instantiate and set up note start visuals
                GameObject startClone = Instantiate(_noteStartExample, _noteLineParent);
                startClone.SetActive(true);
                startClone.transform.localPosition = new Vector3(noteDisplay.startX, _barHover * 2, noteDisplay.startZ - _depth * startBar / _barsToShowPerSlide);
                startClone.transform.localScale = new Vector3(_whiteKeyWidth / 2, _whiteKeyWidth / 2, _whiteKeyWidth / 2);
                SetNoteColor(startClone, currentNote.note);

            }
        }
    }


    void SetNoteColor(GameObject clone, int note)
    {
        // Set the color based on the note value
        switch (note % 12)
        {
            case 0:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorA;
                break;
            case 1:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorA1;
                break;
            case 2:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorB;
                break;
            case 3:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorC;
                break;
            case 4:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorC1;
                break;
            case 5:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorD;
                break;
            case 6:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorD1;
                break;
            case 7:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorE;
                break;
            case 8:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorF;
                break;
            case 9:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorF1;
                break;
            case 10:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorG;
                break;
            case 11:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = _colorG1;
                break;
            default:
                clone.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.white;
                break;
        }
    }

    NoteDisplay CalculateNoteDisplay(SongController.Song.SlideNote currentNote, SongController.Song.SlideNote nextNote)
    {
        float x, z, nextx, nextz, length, angle;
        int noteNote = currentNote.note, nextNoteNote = nextNote.note, keyNote;

        // note x
        float calcNote = (float)noteNote / 12 * 7;
        if (noteNote % 12 == 1 || noteNote % 12 == 4 || noteNote % 12 == 6 || noteNote % 12 == 9 || noteNote % 12 == 11)
        { // if black key
            keyNote = Convert.ToInt32(Math.Floor(calcNote));
            x = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[keyNote % 7];
        }
        else
        { // if white key
            keyNote = Convert.ToInt32(Math.Round(calcNote));
            x = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2;
        }

        // z
        z = (currentNote.startBeat - (/*bar*/ 0 * _beatsPerBar)) * _depth / (_beatsPerBar * _barsToShowPerSlide);

        // note x
        calcNote = (float)nextNoteNote / 12 * 7;
        if (nextNoteNote % 12 == 1 || nextNoteNote % 12 == 4 || nextNoteNote % 12 == 6 || nextNoteNote % 12 == 9 || nextNoteNote % 12 == 11)
        { // if black key
            keyNote = Convert.ToInt32(Math.Floor(calcNote));
            nextx = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[keyNote % 7];
        }
        else
        { // if white key
            keyNote = Convert.ToInt32(Math.Round(calcNote));
            nextx = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - _backgroundWidth / 2 + _whiteKeyWidth / 2;
        }

        // z
        nextz = (nextNote.startBeat - (/*bar*/ 0 * _beatsPerBar)) * _depth / (_beatsPerBar * _barsToShowPerSlide);

        // length
        length = Mathf.Sqrt(Mathf.Pow(nextx - x, 2) + Mathf.Pow(nextz - z, 2));

        // angle
        angle = Mathf.Atan2(nextx - x, nextz - z) * Mathf.Rad2Deg;

        return new NoteDisplay((nextx + x) / 2, (nextz + z) / 2, x, z, length, angle);
    }

    public void DoReset()
    {
        _slideBar.DoReset(SongController._time);
        _currentBar = 0;
        CreateGridLines();
        DrawNoteLines(SongController.GetSong(), _currentBar);
    }

}
