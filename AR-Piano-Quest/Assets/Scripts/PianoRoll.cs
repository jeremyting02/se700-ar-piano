using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoRoll : MonoBehaviour
{
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


    [SerializeField] BeatBar _beatBar;
    [SerializeField] float _barLength = 0.001f;
    [SerializeField] float _barHover = 0.0003f;

    static bool _calibrationMode = false;
    [SerializeField] Color _calibrationKeyColour = Color.blue;
    [SerializeField] int[] _calibrationKeys = new int[] { 37, 40, 42, 45 };


    private void Awake()
    {
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


        // Initialse beat bars
        _beatBar.Initialise(SongController._time, width, _depth, _unitLength, _barLength, _barHover);

        // Load song into key columns
        LoadSong();
    }

    private void Update()
    {
        // Elapse time
        if (!SongController._paused)
        {
            foreach (KeyColumn keyColumn in _keyColumns)
            {
                keyColumn.Elapse(SongController._time);
            }
            _beatBar.Elapse(SongController._time);
        }

    }

    public void ToggleCalibration()
    {
        _calibrationMode = !_calibrationMode;

        foreach (KeyColumn keyColumn in _keyColumns)
        {
            keyColumn.EnableCalibration(_calibrationMode);
        }
    }

    void LoadSong()
    {
        foreach (int key in SongController.GetSong().KeyPresses.Keys)
        {
            foreach (SongController.Song.RollPressInfo pressInfo in SongController.GetSong().KeyPresses[key])
            {
                _keyColumns[key].InputNote(pressInfo.Start, pressInfo.Length);
            }
        }
    }

    public void DoReset()
    {
        foreach (KeyColumn keyColumn in _keyColumns)
        {
            keyColumn.DoReset();
        }
        _beatBar.DoReset(SongController._time);

        LoadSong();
    }

}
