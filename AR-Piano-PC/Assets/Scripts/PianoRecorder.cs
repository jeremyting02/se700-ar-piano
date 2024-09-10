using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MidiJack;

public class PianoRecorder : MonoBehaviour
{
    public bool recording = false;
    private Dictionary<int, KeyPressData> activeKeys = new Dictionary<int, KeyPressData>(); // Tracks active keys and their start times
    private List<KeyPressData> keyPressDataList = new List<KeyPressData>();

    [Serializable]
    public class KeyPressData
    {
        public int key;
        public float startTime;
        public float lengthPressed;

        public KeyPressData(int key, float startTime)
        {
            this.key = key;
            this.startTime = startTime;
            this.lengthPressed = 0f; // Initialize with 0, will be updated when key is released
        }

        public void SetLength(float endTime)
        {
            this.lengthPressed = endTime - this.startTime;
        }
    }

    private void OnEnable()
    {
        MidiMaster.noteOnDelegate += NoteOn;
        MidiMaster.noteOffDelegate += NoteOff;
    }

    private void OnDisable()
    {
        MidiMaster.noteOnDelegate -= NoteOn;
        MidiMaster.noteOffDelegate -= NoteOff;
    }

    private void Update()
    {
        // Toggle recording with the 'R' key
        if (Input.GetKeyDown(KeyCode.R))
        {
            recording = !recording;

            if (!recording)
            {
                // Save recorded data when recording stops
                SaveToCsv();
            }
        }
    }

    // MIDI key press event handler
    void NoteOn(MidiChannel channel, int note, float velocity)
    {
        if (recording && !activeKeys.ContainsKey(note))
        {
            KeyPressData keyPress = new KeyPressData(note, Time.time);
            activeKeys[note] = keyPress;
            // Debug.Log($"Key {note} pressed at {keyPress.startTime}");
        }
    }

    // MIDI key release event handler
    void NoteOff(MidiChannel channel, int note)
    {
        if (recording && activeKeys.ContainsKey(note))
        {
            KeyPressData keyPress = activeKeys[note];
            keyPress.SetLength(Time.time); // Calculate duration of key press
            keyPressDataList.Add(keyPress);
            activeKeys.Remove(note);
            // Debug.Log($"Key {note} released at {Time.time}, duration {keyPress.lengthPressed}");
        }
    }

    // Save the recorded key press data to a CSV file
    void SaveToCsv()
    {
        string filePath = Application.persistentDataPath + "/piano_recording.csv";

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Key,Start Time,Length Pressed");
            foreach (KeyPressData keyPress in keyPressDataList)
            {
                writer.WriteLine($"{keyPress.key},{keyPress.startTime},{keyPress.lengthPressed}");
            }
        }

        Debug.Log($"Recording saved to {filePath}");
    }
}
