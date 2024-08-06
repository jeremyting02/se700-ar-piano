using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiJack;
using UnityEngine.UI;
using TMPro;

public class PianoReceiver : MonoBehaviour
{
    [SerializeField] FirebaseManager _firebaseManager;
    bool _takeInput;

    float _time;
    int _sessionIndex = -1;

    Dictionary<int, float> _keyStarts = new Dictionary<int, float>();

    [SerializeField] Image _recordingImage;
    [SerializeField] TMP_Text _recordingText;

    private void Awake()
    {
        SaveAndLoad.Load();

        _recordingText.text = "ID: " + SaveAndLoad.data.GetNextSessionIndex();
    }

    void OnEnable()
    {
        MidiMaster.noteOnDelegate += NoteOn;
        MidiMaster.noteOffDelegate += NoteOff;

        Invoke("TakeInput", 1f);
    }

    void TakeInput() { _takeInput = true; }

    void OnDisable()
    {
        MidiMaster.noteOnDelegate -= NoteOn;
        MidiMaster.noteOffDelegate -= NoteOff;
    }

    void Update()
    {
        if (_sessionIndex != -1)
        {
            _time += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            SaveAndLoad.data.PrintSessionData(SaveAndLoad.data.GetNextSessionIndex() - 1);
        }
    }

    void NoteOn(MidiChannel channel, int note, float velocity)
    {
        if (_takeInput)
        {
            // Debug.Log("NoteOn: " + channel + "," + note + "," + velocity);

            if (note == 21)
            {
                _firebaseManager.Trigger("metronome");
            }
            if (note == 108)
            {
                _firebaseManager.SetRecordTime(_time);

                _firebaseManager.Trigger("start-stop");

                if (_sessionIndex == -1)
                {
                    _sessionIndex = SaveAndLoad.data.NewSession();
                    _firebaseManager.SetSessionIndex(_sessionIndex);
                    _recordingImage.color = Color.green;
                }
            }
            if (note == 22)
            {
                _firebaseManager.Trigger("calibration");
            }
            if (note == 106)
            {
                _firebaseManager.NextSong();
            }

            if (_sessionIndex != -1)
            {
                _keyStarts[note] = _time;
            }
        }
    }

    void NoteOff(MidiChannel channel, int note)
    {
        if (_takeInput)
        {
            // Debug.Log("NoteOff: " + channel + "," + note);

            if (_sessionIndex != -1)
            {
                SaveAndLoad.data.GetSession(_sessionIndex).AddKeyPress(note, _keyStarts[note], _time);
            }
        }
    }

    public float GetTime()
    {
        return _time;
    }
}
