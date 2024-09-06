using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SongController : MonoBehaviour
{
    static float globalTime;

    public static int _songIndex;

    public static float _time;
    public static float _last_tick_time;
    public static bool _paused = true;

    AudioSource _metronome;
    [SerializeField] int _metronomePerBeat = 1;
    [SerializeField] float _metronomeOffset = -0.2f;
    [SerializeField] PianoRoll _pianoRoll;
    [SerializeField] PianoSlide _pianoSlide;

    public static List<Song> _songs = new List<Song>();

    public class Song
    {
        float _bps;
        public float getBPS { get { return _bps; } }
        float _nBeats; // nBeats
        public float getNBeats { get { return _nBeats; } }

        int _nStartBeats = 8;

        int _beatsPerBar = 4;
        int _barsPerSlide = 2;

        Dictionary<int, List<RollPressInfo>> _rollInfo = new Dictionary<int, List<RollPressInfo>>();
        public Dictionary<int, RollPressInfo[]> KeyPresses
        {
            get
            {
                Dictionary<int, RollPressInfo[]> newInfo = new Dictionary<int, RollPressInfo[]>();
                foreach (int key in _rollInfo.Keys)
                {
                    newInfo[key] = _rollInfo[key].ToArray();
                }
                return newInfo;
            }
        }

        public List<SlideNote> _slideInfo = new List<SlideNote>();

        public Song(float bps, float nBeats)
        {
            _bps = bps;
            _nBeats = nBeats + _nStartBeats;
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
            if (!_rollInfo.ContainsKey(note)) _rollInfo[note] = new List<RollPressInfo>();

            for (int i = 0; i < info.Length; i += 2)
            {
                _rollInfo[note].Add(new RollPressInfo(info[i] + timeOffset + _nStartBeats, info[i + 1]));
                _slideInfo.Add(new SlideNote(info[i] + timeOffset + _nStartBeats, note, false));
            }
            _slideInfo = _slideInfo.OrderBy(n => n.startBeat).ToList();
        }

        public class SlideNote
        {
            public float startBeat;   // The beat at which the note starts
            public int note;          // The MIDI note value
            public bool disconnect;   // Whether the note disconnects from the previous note

            public SlideNote(float startBeat, int note, bool disconnect)
            {
                this.startBeat = startBeat;
                this.note = note;
                this.disconnect = disconnect;
            }

            public void setDisconnect(bool disconnect)
            {
                this.disconnect = disconnect;
            }
        }

        public class RollPressInfo
        {
            float _start;
            public float Start { get { return _start; } }
            float _length;
            public float Length { get { return _length; } }

            public RollPressInfo(float start, float length)
            {
                _start = start;
                _length = length;
            }
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        // Generate song info
        GenerateSongs();

        // Setup time
        _time = 0; // -8; // -_depth / _beatLength; 8 beats?
        _last_tick_time = (int)_time - 0.2f;

        // Setup metronome
        _metronome = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // Elapse global time (for eye tracking)
        if (!_paused || globalTime > 0)
        {
            globalTime += Time.deltaTime;
        }

        // Elapse time
        if (!_paused)
        {
            _time += Time.deltaTime * GetSong().getBPS;
        }

        // Tick metronome
        if (_time > _last_tick_time + _metronomePerBeat)
        {
            _metronome.PlayOneShot(_metronome.clip);
            _last_tick_time += _metronomePerBeat;
        }

        // Reset
        if (_time > GetSong().getNBeats)
        {
            DoReset();
        }
    }

    public void DoReset() { DoReset(_songIndex); }

    public void DoReset(int songIndex)
    {
        _songIndex = songIndex;

        _time = 0;
        _last_tick_time = (int)(-_time / _metronomePerBeat) * -_metronomePerBeat + (_metronomeOffset * GetSong().getBPS);
        _paused = true;

        _pianoRoll.DoReset();
        _pianoSlide.DoReset();
    }

    public void TogglePause()
    {
        _paused = !_paused;
    }

    public void ToggleCalibration()
    {
        _pianoRoll.ToggleCalibration();
    }

    public static float GetGlobalTime()
    {
        return globalTime;
    }

    public static Song GetSong()
    {
        return _songs[_songIndex];
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

        newSong._slideInfo[8].setDisconnect(true);
        newSong._slideInfo[13].setDisconnect(true);
        newSong._slideInfo[22].setDisconnect(true);

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
}
