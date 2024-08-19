using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using TMPro;

public class FirebaseManager : MonoBehaviour
{
    DatabaseReference reference;

    int _sessionIndex = -1;

    [SerializeField] PianoReceiver _pianoReciever;
    [SerializeField] TMP_Text _songIndexText;

    private void Start()
    {
        reference = FirebaseDatabase.DefaultInstance.RootReference;

        FirebaseDatabase.DefaultInstance.GetReference("songIndex").ValueChanged += HandleUpdateSongIndex;
        FirebaseDatabase.DefaultInstance.GetReference("recordTime").ValueChanged += HandleUpdateRecordTime;

        StartCoroutine(ReadEyeData());
    }

    private void Update()
    {
        // Controls for use without MIDI keyboard
        // N        for next song
        // M        for metronome
        // Space    for start/stop
        // C        for calibration
        if (Input.GetKeyDown(KeyCode.M))
        {
            Trigger("metronome");
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetRecordTime(_pianoReciever.GetTime());
            Trigger("start-stop");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Trigger("calibration");
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            NextSong();
        }
    }

    public void Trigger(string trigger)
    {
        FirebaseDatabase.DefaultInstance.GetReference("trigger/" + trigger).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshop = task.Result;
                reference.Child("trigger/" + trigger).SetValueAsync(true);
            }
        });
    }

    public void NextSong()
    {
        FirebaseDatabase.DefaultInstance.GetReference("songIndex").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshop = task.Result;
                reference.Child("songIndex").SetValueAsync((int.Parse(snapshop.Value.ToString()) + 1) % 7);
            }
        });
    }

    public void HandleUpdateSongIndex(object sender, ValueChangedEventArgs args)
    {
        DataSnapshot snapshop = args.Snapshot;
        _songIndexText.text = "Song Index: " + snapshop.Value;
    }

    IEnumerator ReadEyeData()
    {
        bool readingData = false;
        bool clearingData = false;

        DatabaseReference eyeDataReference = FirebaseDatabase.DefaultInstance.GetReference("eyeData");

        while (true)
        {
            yield return new WaitForSeconds(2f);

            readingData = true;

            eyeDataReference.GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to read eyeData: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (task.Result.Value != null && snapshot.HasChildren)
                    {
                        foreach (DataSnapshot child in snapshot.Children)
                        {
                            string childKey = child.Key;
                            string childValue = child.Value.ToString();

                            if (_sessionIndex != -1) SaveAndLoad.data.GetSession(_sessionIndex).AddEyeData(childKey, childValue);
                        }

                        clearingData = true;
                    }

                    readingData = false;
                }
            });
            yield return new WaitUntil(() => !readingData);

            if (clearingData)
            {
                eyeDataReference.RemoveValueAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Failed to delete eyeData node: " + task.Exception);
                    }
                    else if (task.IsCompleted)
                    {
                        clearingData = false;
                    }
                });

                yield return new WaitUntil(() => !clearingData);
            }

            yield return new WaitForSeconds(1.5f);
        }
    }

    public void HandleUpdateRecordTime(object sender, ValueChangedEventArgs args)
    {
        DataSnapshot snapshop = args.Snapshot;
        float value = float.Parse(snapshop.Value.ToString());

        // Record Time Requested
        if (value == -1)
        {
            SetRecordTime(_pianoReciever.GetTime());
        }
    }

    public void SetRecordTime(float newRecordTime)
    {
        FirebaseDatabase.DefaultInstance.GetReference("recordTime").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshop = task.Result;
                reference.Child("recordTime").SetValueAsync(newRecordTime);
            }
        });
    }

    public void SetSessionIndex(int newIndex)
    {
        _sessionIndex = newIndex;
    }
}