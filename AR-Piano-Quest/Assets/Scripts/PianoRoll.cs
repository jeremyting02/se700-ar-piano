using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoRoll : MonoBehaviour
{
    static float globalTime;

    int _songIndex;
    class Song
    {
        float _tempo;
        public float Tempo { get { return _tempo; } }
        float _totalLength;
        public float TotalLength { get { return _totalLength; } }

        Dictionary<int, List<PressInfo>> _keyPresses = new Dictionary<int, List<PressInfo>>();
        public Dictionary<int, PressInfo[]> KeyPresses
        {
            get
            {
                Dictionary<int, PressInfo[]> newInfo = new Dictionary<int, PressInfo[]>();
                foreach (int key in _keyPresses.Keys)
                {
                    newInfo[key] = _keyPresses[key].ToArray();
                }
                return newInfo;
            }
        }

        public Song(float tempo, float totalLength)
        {
            _tempo = tempo;
            _totalLength = totalLength;
        }

        public void InputNotes(string key, float timeOffset, params float[] info)
        {
            List<string> keys = new List<string>() { "a", "a#", "b", "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#" };
            InputNotes(keys.IndexOf(key) + 36, timeOffset, info);
        }

        public void InputNotes(int octiveOffset, string key, float timeOffset, params float[] info)
        {
            List<string> keys = new List<string>() { "a", "a#", "b", "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#" };
            InputNotes(keys.IndexOf(key) + 36 + (12 * octiveOffset), timeOffset, info);
        }

        public void InputNotes(int key, float timeOffset, params float[] info)
        {
            if (!_keyPresses.ContainsKey(key)) _keyPresses[key] = new List<PressInfo>();

            for (int i = 0; i < info.Length; i += 2)
            {
                _keyPresses[key].Add(new PressInfo(info[i] + timeOffset, info[i + 1]));
            }
        }

        public class PressInfo
        {
            float _start;
            public float Start { get { return _start; } }
            float _length;
            public float Length { get { return _length; } }

            public PressInfo(float start, float length)
            {
                _start = start;
                _length = length;
            }
        }
    }
    List<Song> _songs = new List<Song>();

    float _time;
    bool _paused = true;

    [SerializeField] float _depth = 0.8f;

    [SerializeField] Transform _background;
    [SerializeField] Transform _keyColumnsParent;

    [SerializeField] Color _whiteKeyColour = Color.white;
    [SerializeField] float _whiteKeyWidth = 0.022f;
    [SerializeField] float _whiteKeySpacing = 0.0015f;
    [SerializeField] int _whiteKeyCount = 52;
    [SerializeField] float _whiteKeyHover = 0.0001f;

    [SerializeField] Color _blackKeyColour = Color.blue;
    [SerializeField] float _blackKeyWidth = 0.0125f;
    [SerializeField] float[] _blackToWhiteKeyOffsets = new float[7] { 0.013f, -1, 0.010f, 0.012f, -1, 0.009f, 0.011f };
    [SerializeField] float _blackKeyHover = 0.0002f;

    [SerializeField] GameObject _keyColumnExample;
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


    private void Awake()
    {
        // Generate song info
        GenerateSongs();

        // Calculate total width
        float width = _whiteKeyWidth * _whiteKeyCount + _whiteKeySpacing * (_whiteKeyCount - 1);

        // Set background size, position and material
        _background.localScale = new Vector3(width, _depth, 1);
        _background.localPosition += new Vector3(0, 0, _depth / 2);
        _background.GetComponent<Renderer>().material.mainTextureScale = new Vector2(_whiteKeyCount, 1);

        // Generate white and black key columns
        List<KeyColumn> keyColumns = new List<KeyColumn>();
        int last_white_index = 0;
        for (int i = 0; i < _whiteKeyCount; i++)
        {
            Vector3 whitePosition;
            if (i == 0)
                whitePosition = new Vector3(-(width / 2) + (_whiteKeyWidth / 2), _whiteKeyHover, 0);
            else
                whitePosition = keyColumns[last_white_index].transform.localPosition + new Vector3(_whiteKeyWidth + _whiteKeySpacing, 0, 0);

            KeyColumn whiteColumn = Instantiate(_keyColumnExample, _keyColumnsParent).GetComponent<KeyColumn>();
            keyColumns.Add(whiteColumn);
            last_white_index = keyColumns.Count - 1;
            whiteColumn.Initialise(whitePosition, _whiteKeyWidth, _depth, _unitLength, _whiteKeyColour);

            // White key has neighbouring black key to its right
            if (i < _whiteKeyCount - 1 && _blackToWhiteKeyOffsets[i % 7] >= 0)
            {
                Vector3 blackPosition = new Vector3(keyColumns[last_white_index].transform.localPosition.x + _blackToWhiteKeyOffsets[i % 7], _blackKeyHover, 0);
                KeyColumn blackColumn = Instantiate(_keyColumnExample, _keyColumnsParent).GetComponent<KeyColumn>();
                keyColumns.Add(blackColumn);
                blackColumn.Initialise(blackPosition, _blackKeyWidth, _depth, _unitLength, _blackKeyColour);

                foreach (int calibrationKey in _calibrationKeys)
                {
                    if (calibrationKey == keyColumns.Count - 1)
                    {
                        blackColumn.CreateCalibration(_calibrationKeyColour);
                    }
                }
            }
        }
        _keyColumns = keyColumns.ToArray();

        // Setup time
        _time = -_depth / _unitLength;

        // Initialse beat bars
        _beatBar.Initialise(_time, width, _depth, _unitLength, _barLength, _barHover);

        // Setup metronome
        _metronome = GetComponent<AudioSource>();
        _last_tick_time = (int)(-_time / _metronomePerBeat) * -_metronomePerBeat + _metronomeOffset;

        // Load song into key columns
        LoadSong();
    }

    private void Update()
    {
        // Elapse time
        if (!_paused)
        {
            _time += Time.deltaTime * GetSong().Tempo;
            foreach (KeyColumn keyColumn in _keyColumns)
            {
                keyColumn.Elapse(_time);
            }
            _beatBar.Elapse(_time);
        }

        // Elapse global time (for eye tracking)
        if (!_paused || globalTime > 0)
        {
            globalTime += Time.deltaTime;
        }

        // Tick metronome
        if (_time > _last_tick_time + _metronomePerBeat)
        {
            if (_metronomeMode)
            {
                _metronome.PlayOneShot(_metronome.clip);
                //print("Tick");
            }
            _last_tick_time += _metronomePerBeat;
        }

        // Reset
        if (_time > GetSong().TotalLength)
        {
            DoReset();
        }

        // Debug
        if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
        if (Input.GetKeyDown(KeyCode.LeftControl)) ToggleMetronome();
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleCalibration();
        if (Input.GetKeyDown(KeyCode.Backspace)) DoReset();
    }

    public void DoReset() { DoReset(_songIndex); }

    public void DoReset(int songIndex)
    {
        _songIndex = songIndex;

        _time = -_depth / _unitLength;
        _last_tick_time = (int)(-_time / _metronomePerBeat) * -_metronomePerBeat + (_metronomeOffset * GetSong().Tempo);
        foreach (KeyColumn keyColumn in _keyColumns)
        {
            keyColumn.DoReset();
        }
        _beatBar.DoReset(_time);
        _paused = true;

        LoadSong();
    }

    public void TogglePause()
    {
        _paused = !_paused;
    }

    public void ToggleMetronome()
    {
        _metronomeMode = !_metronomeMode;

        _beatBar.SetMetronomeMode(_metronomeMode);
    }

    public void ToggleCalibration()
    {
        _calibrationMode = !_calibrationMode;

        foreach (KeyColumn keyColumn in _keyColumns)
        {
            keyColumn.EnableCalibration(_calibrationMode);
        }
    }

    Song GetSong()
    {
        return _songs[_songIndex];
    }

    void LoadSong()
    {
        foreach (int key in GetSong().KeyPresses.Keys)
        {
            foreach (Song.PressInfo pressInfo in GetSong().KeyPresses[key])
            {
                _keyColumns[key].InputNote(pressInfo.Start, pressInfo.Length);
            }
        }
    }

    #region Songs

    void GenerateSongs()
    {
        //JingleBells();

        float slow = 1.25f;
        float fast = 1.66667f;

        Tutorial(slow);

        Cuckoo(fast);
        AuClairDeLaLune(slow);
        ObLaDiLaDa(fast);

        AlleMeineEntchen(slow);
        BeutifulBrownEyes(fast);
        GilligansIsle(slow);
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

    void Cuckoo(float tempo)
    {
        Song newSong = new Song(tempo, 48);

        newSong.InputNotes("g", 0, 0, 2, 3, 2);
        newSong.InputNotes("e", 0, 2, 1, 5, 1, 9, 2);
        newSong.InputNotes("d", 0, 6, 1, 8, 1);
        newSong.InputNotes("c", 0, 7, 1, 11, 1);

        newSong.InputNotes("g", 12, 0, 2, 3, 2);
        newSong.InputNotes("e", 12, 2, 1, 5, 1, 7, 1);
        newSong.InputNotes("d", 12, 6, 1, 8, 1);
        newSong.InputNotes("c", 12, 9, 3);

        newSong.InputNotes("d", 24, 0, 1, 1, 1, 5, 1);
        newSong.InputNotes("e", 24, 2, 1, 6, 1, 7, 1, 11, 1);
        newSong.InputNotes("f", 24, 3, 2, 8, 1);
        newSong.InputNotes("g", 24, 9, 2);

        newSong.InputNotes("g", 36, 0, 2, 3, 2);
        newSong.InputNotes("e", 36, 2, 1, 5, 1, 7, 1);
        newSong.InputNotes("f", 36, 6, 1);
        newSong.InputNotes("d", 36, 8, 1);
        newSong.InputNotes("c", 36, 9, 3);

        _songs.Add(newSong);
    }

    void AuClairDeLaLune(float tempo)
    {
        Song newSong = new Song(tempo, 64);

        newSong.InputNotes(34 + 12, 0, 0, 1, 1, 1, 2, 1, 8, 1, 12, 4);
        newSong.InputNotes(36 + 12, 0, 3, 1, 6, 2, 10, 1, 11, 1);
        newSong.InputNotes(38 + 12, 0, 4, 2, 9, 1);

        newSong.InputNotes(34 + 12, 16, 0, 1, 1, 1, 2, 1, 8, 1, 12, 4);
        newSong.InputNotes(36 + 12, 16, 3, 1, 6, 2, 10, 1, 11, 1);
        newSong.InputNotes(38 + 12, 16, 4, 2, 9, 1);

        newSong.InputNotes(36 + 12, 32, 0, 1, 1, 1, 2, 1, 3, 1, 8, 1);
        newSong.InputNotes(31 + 12, 32, 4, 2, 6, 2, 11, 1);
        newSong.InputNotes(34 + 12, 32, 9, 1);
        newSong.InputNotes(33 + 12, 32, 10, 1);
        newSong.InputNotes(29 + 12, 32, 12, 4);

        newSong.InputNotes(34 + 12, 48, 0, 1, 1, 1, 2, 1, 8, 1, 12, 4);
        newSong.InputNotes(36 + 12, 48, 3, 1, 6, 2, 10, 1, 11, 1);
        newSong.InputNotes(38 + 12, 48, 4, 2, 9, 1);

        _songs.Add(newSong);
    }

    void ObLaDiLaDa(float tempo)
    {
        Song newSong = new Song(tempo, 64);

        newSong.InputNotes(48, 0, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1);
        newSong.InputNotes(46, 0, 6, 1, 9, 2, 11, 5);
        newSong.InputNotes(44, 0, 7, 1);
        newSong.InputNotes(43, 0, 8, 1);

        newSong.InputNotes(49, 16, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1);
        newSong.InputNotes(48, 16, 6, 1);
        newSong.InputNotes(46, 16, 7, 1);
        newSong.InputNotes(44, 16, 8, 8);

        newSong.InputNotes(49, 32, 6, 1, 8, 1, 15, 1, 18, 1, 21, 1);
        newSong.InputNotes(48, 32, 7, 1, 16, 1, 17, 1, 19, 1, 22, 1);
        newSong.InputNotes(46, 32, 20, 1, 23, 1);
        newSong.InputNotes(44, 32, 24, 8);
        newSong.InputNotes(51, 32, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 9, 2, 14, 1);
        newSong.InputNotes(53, 32, 11, 2, 13, 1);

        _songs.Add(newSong);
    }

    void AlleMeineEntchen(float tempo)
    {
        Song newSong = new Song(tempo, 64);

        newSong.InputNotes(39, 0, 0, 1);
        newSong.InputNotes(41, 0, 1, 1);
        newSong.InputNotes(43, 0, 2, 1);
        newSong.InputNotes(44, 0, 3, 1);
        newSong.InputNotes(46, 0, 4, 2, 6, 2, 12, 4);
        newSong.InputNotes(48, 0, 8, 1, 9, 1, 10, 1, 11, 1);

        newSong.InputNotes(41, 16, 8, 1, 9, 1, 10, 1, 11, 1);
        newSong.InputNotes(43, 16, 4, 2, 6, 2);
        newSong.InputNotes(44, 16, 0, 1, 1, 1, 2, 1, 3, 1);
        newSong.InputNotes(46, 16, 12, 4);

        newSong.InputNotes(39, 32, 0, 1);
        newSong.InputNotes(41, 32, 1, 1);
        newSong.InputNotes(43, 32, 2, 1);
        newSong.InputNotes(44, 32, 3, 1);
        newSong.InputNotes(46, 32, 4, 2, 6, 2, 12, 4);
        newSong.InputNotes(48, 32, 8, 1, 9, 1, 10, 1, 11, 1);

        newSong.InputNotes(39, 48, 12, 4);
        newSong.InputNotes(41, 48, 8, 1, 9, 1, 10, 1, 11, 1);
        newSong.InputNotes(43, 48, 4, 2, 6, 2);
        newSong.InputNotes(44, 48, 0, 1, 1, 1, 2, 1, 3, 1);

        _songs.Add(newSong);
    }

    void BeutifulBrownEyes(float tempo)
    {
        Song newSong = new Song(tempo, 48);

        newSong.InputNotes(50, 0, 0, 1, 1, 1, 2, 1, 5, 1);
        newSong.InputNotes(46, 0, 3, 1);
        newSong.InputNotes(48, 0, 4, 1);
        newSong.InputNotes(51, 0, 6, 1, 7, 2, 9, 3);

        newSong.InputNotes(50, 12, 0, 1, 1, 1, 2, 1, 5, 1);
        newSong.InputNotes(46, 12, 3, 1);
        newSong.InputNotes(48, 12, 4, 1, 6, 3);
        newSong.InputNotes(53, 12, 9, 3);

        newSong.InputNotes(50, 24, 0, 1, 1, 1, 2, 1, 5, 1);
        newSong.InputNotes(46, 24, 3, 1);
        newSong.InputNotes(48, 24, 4, 1);
        newSong.InputNotes(51, 24, 6, 1, 7, 2, 9, 3);

        newSong.InputNotes(50, 36, 2, 1, 4, 1);
        newSong.InputNotes(46, 36, 6, 6);
        newSong.InputNotes(48, 36, 1, 1, 5, 1);
        newSong.InputNotes(51, 36, 3, 1);
        newSong.InputNotes(53, 36, 0, 1);

        _songs.Add(newSong);
    }


    void GilligansIsle(float tempo)
    {
        Song newSong = new Song(tempo, 64);

        newSong.InputNotes(41, 0, 0, 1, 8, 1);
        newSong.InputNotes(48, 0, 1, 1, 2, 1, 3, 1, 9, 1, 10, 1, 11, 1);
        newSong.InputNotes(46, 0, 4, 1, 12, 3);
        newSong.InputNotes(43, 0, 5, 1);
        newSong.InputNotes(39, 0, 6, 1, 7, 1, 15, 1);

        newSong.InputNotes(41, 16, 0, 1, 12, 3);
        newSong.InputNotes(48, 16, 1, 1, 2, 1, 3, 1, 15, 1);
        newSong.InputNotes(46, 16, 4, 1, 7, 1);
        newSong.InputNotes(44, 16, 8, 1, 9, 1);
        newSong.InputNotes(43, 16, 10, 1);
        newSong.InputNotes(39, 16, 11, 1);
        newSong.InputNotes(51, 16, 5, 1, 6, 1);

        newSong.InputNotes(41, 32, 0, 1, 8, 1);
        newSong.InputNotes(48, 32, 1, 1, 2, 1, 3, 1, 9, 1, 10, 1, 11, 1);
        newSong.InputNotes(46, 32, 4, 1, 12, 3);
        newSong.InputNotes(43, 32, 5, 1);
        newSong.InputNotes(39, 32, 6, 1, 7, 1, 15, 1);

        newSong.InputNotes(41, 48, 0, 1, 12, 4);
        newSong.InputNotes(48, 48, 1, 1, 2, 1, 3, 1);
        newSong.InputNotes(46, 48, 4, 1, 7, 1);
        newSong.InputNotes(44, 48, 8, 2);
        newSong.InputNotes(43, 48, 10, 1);
        newSong.InputNotes(39, 48, 11, 1);
        newSong.InputNotes(51, 48, 5, 1, 6, 1);

        _songs.Add(newSong);
    }


    void JingleBells(float tempo)
    {
        Song newSong = new Song(tempo, 64);

        newSong.InputNotes("e", 0, 0, 1, 1, 1, 2, 2, 4, 1, 5, 1, 6, 2, 8, 1, 12, 4);
        newSong.InputNotes("g", 0, 9, 1);
        newSong.InputNotes("c", 0, 10, 1);
        newSong.InputNotes("d", 0, 11, 1);

        newSong.InputNotes("f", 16, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1);
        newSong.InputNotes("e", 16, 5, 1, 6, 1, 7, 1, 8, 1, 11, 1);
        newSong.InputNotes("d", 16, 9, 1, 10, 1, 12, 2);
        newSong.InputNotes("g", 16, 14, 2);

        newSong.InputNotes("e", 32, 0, 1, 1, 1, 2, 2, 4, 1, 5, 1, 6, 2, 8, 1, 12, 4);
        newSong.InputNotes("g", 32, 9, 1);
        newSong.InputNotes("c", 32, 10, 1);
        newSong.InputNotes("d", 32, 11, 1);

        newSong.InputNotes("f", 48, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 10, 1);
        newSong.InputNotes("e", 48, 5, 1, 6, 1, 7, 1);
        newSong.InputNotes("g", 48, 8, 1, 9, 1);
        newSong.InputNotes("d", 48, 11, 1);
        newSong.InputNotes("c", 48, 12, 4);

        _songs.Add(newSong);
    }

    void TriumphantCitadel(float tempo)
    {
        Song newSong = new Song(tempo, 64);

        newSong.InputNotes(36, 0, 0, 1, 4, 1, 6, 2, 13, 1);
        newSong.InputNotes(38, 0, 1, 1, 3, 1, 8, 3, 12, 1);
        newSong.InputNotes(39, 0, 2, 1, 11, 1);
        newSong.InputNotes(34, 0, 5, 1);
        newSong.InputNotes(32, 0, 14, 2);

        newSong.InputNotes(43, 16, 0, 3);
        newSong.InputNotes(41, 16, 3, 1);
        newSong.InputNotes(39, 16, 4, 3, 8, 8);
        newSong.InputNotes(38, 16, 7, 1);

        newSong.InputNotes(36, 32, 0, 1, 4, 1, 6, 2, 13, 1);
        newSong.InputNotes(38, 32, 1, 1, 3, 1, 8, 3, 12, 1);
        newSong.InputNotes(39, 32, 2, 1, 11, 1);
        newSong.InputNotes(34, 32, 5, 1);
        newSong.InputNotes(32, 32, 14, 2);

        newSong.InputNotes(43, 48, 0, 3);
        newSong.InputNotes(41, 48, 3, 1);
        newSong.InputNotes(39, 48, 4, 3);
        newSong.InputNotes(38, 48, 7, 1);
        newSong.InputNotes(36, 48, 8, 8);

        _songs.Add(newSong);
    }

    void BowsersCastleWii(float tempo)
    {
        Song newSong = new Song(tempo, 64);

        newSong.InputNotes(34, 0, 1, 1);
        newSong.InputNotes(39, 0, 2, 1, 8, 1);
        newSong.InputNotes(42, 0, 3, 1, 7, 1, 10, 1);
        newSong.InputNotes(47, 0, 4, 1);
        newSong.InputNotes(45, 0, 5, 1);
        newSong.InputNotes(44, 0, 6, 1);
        newSong.InputNotes(41, 0, 9, 1);
        newSong.InputNotes(38, 0, 11, 2);
        newSong.InputNotes(35, 0, 13, 1);
        newSong.InputNotes(33, 0, 14, 1);
        newSong.InputNotes(32, 0, 15, 1);

        newSong.InputNotes(34, 16, 1, 1);
        newSong.InputNotes(39, 16, 2, 1);
        newSong.InputNotes(42, 16, 3, 1, 7, 1);
        newSong.InputNotes(47, 16, 4, 1);
        newSong.InputNotes(45, 16, 5, 1);
        newSong.InputNotes(44, 16, 6, 1, 8, 3);

        newSong.InputNotes(34, 32, 1, 1);
        newSong.InputNotes(39, 32, 2, 1, 8, 1);
        newSong.InputNotes(42, 32, 3, 1, 7, 1, 10, 1);
        newSong.InputNotes(47, 32, 4, 1);
        newSong.InputNotes(45, 32, 5, 1);
        newSong.InputNotes(44, 32, 6, 1);
        newSong.InputNotes(41, 32, 9, 1);
        newSong.InputNotes(38, 32, 11, 2);
        newSong.InputNotes(35, 32, 13, 1);
        newSong.InputNotes(33, 32, 14, 1);
        newSong.InputNotes(32, 32, 15, 1);

        newSong.InputNotes(34, 48, 1, 1);
        newSong.InputNotes(39, 48, 2, 1);
        newSong.InputNotes(42, 48, 3, 1);
        newSong.InputNotes(47, 48, 4, 1, 6, 1);
        newSong.InputNotes(45, 48, 5, 1);
        newSong.InputNotes(50, 48, 7, 1, 12, 4);
        newSong.InputNotes(49, 48, 8, 4);


        _songs.Add(newSong);
    }

    #endregion

    public static float GetGlobalTime()
    {
        return globalTime;
    }
}
