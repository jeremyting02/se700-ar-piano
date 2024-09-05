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


    List<SongController.Song> _songs = new List<SongController.Song>();

    float _depth = 0.2f; // aka length

    [SerializeField] Transform _background;
    [SerializeField] Transform _gridLineParent;
    [SerializeField] GameObject _gridWhiteLineExample;
    [SerializeField] GameObject _gridBlackLineExample;
    [SerializeField] GameObject _gridBeatLineExample;

    [SerializeField] Transform _noteLineParent;
    [SerializeField] GameObject _noteLineExample;

    [SerializeField] float _whiteKeyWidth = 0.022f;
    [SerializeField] float _whiteKeySpacing = 0.0015f;
    [SerializeField] int _whiteKeyCount = 52;
    [SerializeField] float _whiteKeyHover = 0.0001f;

    [SerializeField] float _blackKeyWidth = 0.0125f;
    [SerializeField] float[] _blackToWhiteKeyOffsets = new float[7] { 0.013f, -1, 0.010f, 0.012f, -1, 0.009f, 0.011f }; // offset after a, none after b, after c, after d, none after e, after f, after g
    [SerializeField] float _blackKeyHover = 0.0002f;

    [SerializeField] SlideBar _slideBar;
    [SerializeField] float _barThickness = 0.001f;
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

    int beatsPerBar = 4;
    int barsPerSlide = 2;

    float _beatLength = 0.1f;

    float _backgroundWidth;

    private void Awake()
    {
        // Calculate total width
        _backgroundWidth = _whiteKeyWidth * _whiteKeyCount + _whiteKeySpacing * (_whiteKeyCount - 1);
        _beatLength = _depth / (beatsPerBar * barsPerSlide);

        // Set background size, position and material
        _background.localScale = new Vector3(_backgroundWidth, _depth, 1);
        _background.localPosition += new Vector3(0, 0, _depth / 2);
        _background.GetComponent<Renderer>().material.mainTextureScale = new Vector2(_whiteKeyCount, 1);

        CreateGridLines();

        // Initialse slide bar
        _slideBar.Initialise(SongController._time, _backgroundWidth, _depth, _beatLength, _barThickness, _barHover);

        // DrawNoteLines(_songs[0], 0);
    }

    private void Update()
    {
        // Elapse time
        if (!SongController._paused)
        {
            _slideBar.Elapse(SongController._time);
        }

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
        for (int i = 0; i < beatsPerBar * barsPerSlide + 1; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_gridBeatLineExample, _gridLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(0, _barHover * 2, _depth * i / (beatsPerBar * barsPerSlide));

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_backgroundWidth, 1, _whiteKeyWidth / 10);
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
            clone.transform.localScale = new Vector3(_backgroundWidth, 1, _whiteKeyWidth / 5);
        }
    }

    void DrawNoteLines(SongController.Song song, int bar)
    {
        // Loop through each note in the song
        for (int i = 0; i < song._slideInfo.Count - 1; i++)
        {
            SongController.Song.SlideNote currentNote = song._slideInfo[i];
            SongController.Song.SlideNote nextNote = song._slideInfo[i + 1];

            // Calculate positions for the current and next notes
            NoteDisplay noteDisplay = CalculateNoteDisplay(currentNote, nextNote);

            // Create a new line between the current and next note
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_noteLineExample, _noteLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(noteDisplay.centreX, _barHover * 2, noteDisplay.centreZ);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_whiteKeyWidth / 5, 1, noteDisplay.length);

            // Set the rotation of the clone
            clone.transform.rotation = Quaternion.Euler(0, noteDisplay.angle, 0);

            switch (currentNote.note % 12)
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
        z = (currentNote.startBeat - (/*bar*/ 0 * beatsPerBar)) * _depth / (beatsPerBar * barsPerSlide);

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
        nextz = (nextNote.startBeat - (/*bar*/ 0 * beatsPerBar)) * _depth / (beatsPerBar * barsPerSlide);

        // length
        length = Mathf.Sqrt(Mathf.Pow(nextx - x, 2) + Mathf.Pow(nextz - z, 2));

        // angle
        angle = Mathf.Atan2(nextx - x, nextz - z) * Mathf.Rad2Deg;

        return new NoteDisplay((nextx + x) / 2, (nextz + z) / 2, x, z, length, angle);
    }

    public void DoReset()
    {

    }

}
