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

    private int sessionIndex = -1;
    private float recordingStartTime = 0f; // Time recording started, reset for each recording session

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

    private void Start()
    {
        sessionIndex = SaveAndLoad.data.GetNextSessionIndex(); // Retrieve the session index from SaveAndLoad
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
            if (!recording)
            {
                StartRecording(); // Start a new recording
            }
            else
            {
                StopRecording(); // Stop the current recording and save the data
            }
        }
    }

    // Start a new recording session
    private void StartRecording()
    {
        recording = true;

        // Clear previous recording data
        activeKeys.Clear();
        keyPressDataList.Clear();

        // Reset the recording start time
        recordingStartTime = Time.time;

        Debug.Log("Recording started...");
    }

    // Stop the recording and save the data
    private void StopRecording()
    {
        recording = false;

        // Save the recorded data when recording stops
        SaveToCsv();
        Debug.Log("Recording stopped and saved.");
    }

    // MIDI key press event handler
    void NoteOn(MidiChannel channel, int note, float velocity)
    {
        if (recording && !activeKeys.ContainsKey(note))
        {
            // Calculate the key press start time relative to the recording start time
            KeyPressData keyPress = new KeyPressData(note, Time.time - recordingStartTime);
            activeKeys[note] = keyPress;
            Debug.Log($"Key {note} pressed at {keyPress.startTime}");
        }
    }

    // MIDI key release event handler
    void NoteOff(MidiChannel channel, int note)
    {
        if (recording && activeKeys.ContainsKey(note))
        {
            KeyPressData keyPress = activeKeys[note];
            // Calculate the duration of the key press relative to the recording start time
            keyPress.SetLength(Time.time - recordingStartTime);
            keyPressDataList.Add(keyPress);
            activeKeys.Remove(note);
            Debug.Log($"Key {note} released, duration {keyPress.lengthPressed}");
        }
    }

    // Save the recorded key press data to a CSV file
    void SaveToCsv()
    {
        // Generate the base filename using the session index
        string baseFileName = $"piano_recording_session_{sessionIndex}.csv";
        string filePath = Path.Combine(Application.persistentDataPath, baseFileName);

        // Check if the file exists, and append a number to avoid overwriting
        int fileSuffix = 1;
        while (File.Exists(filePath))
        {
            filePath = Path.Combine(Application.persistentDataPath, $"piano_recording_session_{sessionIndex}_{fileSuffix}.csv");
            fileSuffix++;
        }

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
