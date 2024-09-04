using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // For using the OrderBy method
using UnityEngine;
using UnityEngine.Assertions.Must;

public class PianoSlide : MonoBehaviour
{

    class Note
    {
        public float startBeat;   // The beat at which the note starts
        public int note;          // The MIDI note value
        public bool disconnect;   // Whether the note disconnects from the previous note

        public Note(float startBeat, int note, bool disconnect)
        {
            this.startBeat = startBeat;
            this.note = note;
            this.disconnect = disconnect;
        }
    }

    class NoteDisplay
    {
        public float x;
        public float z;
        public float length;
        public float angle;

        public NoteDisplay(float x, float z, float length, float angle)
        {
            this.x = x;
            this.z = z;
            this.length = length;
            this.angle = angle;
        }
    }

    class Song
    {
        float _bpm;
        public float getBPM { get { return _bpm; } }
        float _nBeats; // nBeats
        public float getBeats { get { return _nBeats; } }

        int beatsPerBar = 4;
        int barsPerSlide = 2;

        public List<Note> notes = new List<Note>();

        public Song(float bpm, float totalLength)
        {
            _bpm = bpm;
            _nBeats = totalLength;
        }

        public void InputNotes(string note, float timeOffset, params float[] info)
        {
            List<string> keys = new List<string>() { "a", "a#", "b", "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#" };
            InputNotes(keys.IndexOf(note) + 36, timeOffset, info);
        }

        public void InputNotes(int octaveOffset, string note, float timeOffset, params float[] info)
        {
            List<string> keys = new List<string>() { "a", "a#", "b", "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#" };
            InputNotes(keys.IndexOf(note) + 36 + (12 * octaveOffset), timeOffset, info);
        }

        public void InputNotes(int note, float timeOffset, params float[] info)
        {
            for (int i = 0; i < info.Length; i += 2)
            {
                notes.Add(new Note(info[i] + timeOffset, note, false));
            }
            notes = notes.OrderBy(n => n.startBeat).ToList();
        }
    }
    List<Song> _songs = new List<Song>();

    [SerializeField] float _depth = 0.8f; // aka length

    [SerializeField] Transform _background;
    [SerializeField] Transform _gridLineParent;
    [SerializeField] GameObject _gridWhiteLineExample;
    [SerializeField] GameObject _gridBlackLineExample;
    [SerializeField] GameObject _gridBeatLineExample;

    [SerializeField] Transform _noteLineParent;
    [SerializeField] GameObject _noteLineExample;

    [SerializeField] Color _whiteKeyColour = Color.white;
    [SerializeField] float _whiteKeyWidth = 0.022f;
    [SerializeField] float _whiteKeySpacing = 0.0015f;
    [SerializeField] int _whiteKeyCount = 52;
    [SerializeField] float _whiteKeyHover = 0.0001f;

    [SerializeField] Color _blackKeyColour = Color.blue;
    [SerializeField] float _blackKeyWidth = 0.0125f;
    [SerializeField] float[] _blackToWhiteKeyOffsets = new float[7] { 0.013f, -1, 0.010f, 0.012f, -1, 0.009f, 0.011f }; // offset after a, none after b, after c, after d, none after e, after f, after g
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
        // Generate song info
        GenerateSongs();

        // Calculate total width
        width = _whiteKeyWidth * _whiteKeyCount + _whiteKeySpacing * (_whiteKeyCount - 1);

        // Set background size, position and material
        _background.localScale = new Vector3(width, _depth, 1);
        _background.localPosition += new Vector3(0, 0, _depth / 2);
        _background.GetComponent<Renderer>().material.mainTextureScale = new Vector2(_whiteKeyCount, 1);

        CreateGridLines();

        DrawNoteLines(_songs[0], 0);
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

    void DrawNoteLines(Song song, int bar)
    {
        // Loop through each note in the song
        for (int i = 0; i < song.notes.Count - 1; i++)
        {
            Note currentNote = song.notes[i];
            Note nextNote = song.notes[i + 1];

            // Calculate positions for the current and next notes
            NoteDisplay noteDisplay = CalculateNoteDisplay(currentNote, nextNote);

            // Create a new line between the current and next note
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_noteLineExample, _noteLineParent);

            // Enable the clone (in case the example is disabled)
            clone.SetActive(true);

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(noteDisplay.x, _barHover * 2, noteDisplay.z);

            // Set the scale of the clone
            clone.transform.localScale = new Vector3(_whiteKeyWidth / 5, 1, noteDisplay.length);

            // Set the rotation of the clone
            clone.transform.rotation = Quaternion.Euler(0, noteDisplay.angle, 0);
        }
    }

    NoteDisplay CalculateNoteDisplay(Note currentNote, Note nextNote)
    {
        float x, z, nextx, nextz, length, angle;
        int noteNote = currentNote.note, nextNoteNote = nextNote.note, keyNote;

        // note x
        float calcNote = (float)noteNote / 12 * 7;
        if (noteNote % 12 == 1 || noteNote % 12 == 4 || noteNote % 12 == 6 || noteNote % 12 == 9 || noteNote % 12 == 11)
        { // if black key
            keyNote = Convert.ToInt32(Math.Floor(calcNote));
            x = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[keyNote % 7];
        }
        else
        { // if white key
            keyNote = Convert.ToInt32(Math.Round(calcNote));
            x = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2;
        }

        // z
        z = (currentNote.startBeat - (/*bar*/ 0 * beatsPerBar)) * _depth / (beatsPerBar * barsPerSlide);

        // note x
        calcNote = (float)nextNoteNote / 12 * 7;
        if (nextNoteNote % 12 == 1 || nextNoteNote % 12 == 4 || nextNoteNote % 12 == 6 || nextNoteNote % 12 == 9 || nextNoteNote % 12 == 11)
        { // if black key
            keyNote = Convert.ToInt32(Math.Floor(calcNote));
            nextx = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[keyNote % 7];
        }
        else
        { // if white key
            keyNote = Convert.ToInt32(Math.Round(calcNote));
            nextx = keyNote * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2;
        }

        // z
        nextz = (nextNote.startBeat - (/*bar*/ 0 * beatsPerBar)) * _depth / (beatsPerBar * barsPerSlide);

        // length
        length = Mathf.Sqrt(Mathf.Pow(nextx - x, 2) + Mathf.Pow(nextz - z, 2));

        // angle
        angle = Mathf.Atan2(nextx - x, nextz - z) * Mathf.Rad2Deg;

        return new NoteDisplay((nextx + x) / 2, (nextz + z) / 2, length, angle);
    }

    // I need to position the line on the note(?) then rotate it towards the next note (calculation time) and scale it to the length of the note (calculation time)

    // if (note % 12 == 1 || note % 12 == 4 || note % 12 == 6 || note % 12 == 9 || note % 12 == 11) { // Black key}

    // White X
    // x = (note - lowestNote) * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2; (TODO sort out lowest note) (lowest note is 0)

    // Black X
    // if (_blackToWhiteKeyOffsets[i % 7] < 0)
    //         continue;


    //     // Calculate the x position based on the interval and index
    //     x = (note - lowestNote) * (_whiteKeyWidth + _whiteKeySpacing) - width / 2 + _whiteKeyWidth / 2 + _blackToWhiteKeyOffsets[i % 7]; (TODO figure out how this works)
    // 

    // MIDI a = 0, 12, 24 ... 

    // z = (note.startBeat - (bar * beatsPerBar)) * _depth / (beatsPerBar * barsPerSlide)

    // we start at bar 0

    // angle = Mathf.Atan2(nextNote - currentNote, 1) * Mathf.Rad2Deg (Generated by copilot)

    #region Songs

    void GenerateSongs()
    {
        float slow = 1.25f;
        // float fast = 1.66667f;

        Tutorial(slow);
    }

    void Tutorial(float tempo)
    {
        Song newSong = new Song(tempo, 56);

        // note, time offset, time 1, length 1, time 2, length 2, ...
        newSong.InputNotes(39, 0, 0, 1, 8, 1);
        newSong.InputNotes(41, 0, 1, 1, 7, 1);
        newSong.InputNotes(43, 0, 2, 1, 6, 1);
        newSong.InputNotes(44, 0, 3, 1, 5, 1);
        newSong.InputNotes(46, 0, 4, 1);

        newSong.InputNotes(39, 16, 0, 2, 8, 2);
        newSong.InputNotes(43, 16, 2, 2, 6, 2);
        newSong.InputNotes(46, 16, 4, 2);

        newSong.InputNotes(43, 32, 0, 1, 8, 4);
        newSong.InputNotes(45, 32, 1, 1, 7, 1);
        newSong.InputNotes(47, 32, 2, 1, 6, 1);
        newSong.InputNotes(48, 32, 3, 1, 5, 1);
        newSong.InputNotes(50, 32, 4, 1);


        newSong.InputNotes(41, 48, 2, 1);
        newSong.InputNotes(46, 48, 3, 4);
        newSong.InputNotes(34, 48, 0, 1);
        newSong.InputNotes(38, 48, 1, 1);

        _songs.Add(newSong);
    }

    #endregion
}
