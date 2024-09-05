using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;

public class FirebaseManager : MonoBehaviour
{
    [SerializeField] SongController _songController;
    [SerializeField] GameObject _pianoRollObject;
    [SerializeField] GameObject _pianoSlideObject;
    [SerializeField] Calibration _calibration;

    DatabaseReference reference;

    float _recordTimeOffset;
    public float RecordTimeOffset { get { return _recordTimeOffset; } }

    bool slideActive = true;

    bool _sendEyeData;
    List<string> _leftEyeData = new List<string>();
    List<string> _rightEyeData = new List<string>();

    private void Start()
    {
        reference = FirebaseDatabase.DefaultInstance.RootReference;

        FirebaseDatabase.DefaultInstance.GetReference("trigger/start-stop").ValueChanged += HandleUpdateStartStop;
        FirebaseDatabase.DefaultInstance.GetReference("trigger/metronome").ValueChanged += HandleUpdateMetronome;
        FirebaseDatabase.DefaultInstance.GetReference("trigger/calibration").ValueChanged += HandleUpdateCalibration;

        FirebaseDatabase.DefaultInstance.GetReference("songIndex").ValueChanged += HandleUpdateSongIndex;
        FirebaseDatabase.DefaultInstance.GetReference("eyeData").ValueChanged += HandleUpdateEyeData;
        FirebaseDatabase.DefaultInstance.GetReference("recordTime").ValueChanged += HandleUpdateRecordTime;

        RequestRecordTime();

        // slide is active by default
        _pianoRollObject.transform.localScale = new Vector3(0, 0, 0);
        _pianoSlideObject.transform.localScale = new Vector3(1, 1, 1);
    }

    private void Update()
    {
        if (_sendEyeData)
        {
            StartCoroutine(SendEyeData());
            _sendEyeData = false;
        }
    }

    #region Triggers

    public void HandleUpdateStartStop(object sender, ValueChangedEventArgs args)
    {
        DataSnapshot snapshop = args.Snapshot;
        if ((bool)snapshop.Value)
        {
            _songController.TogglePause();
            EndTrigger("start-stop");
        }
    }

    public void HandleUpdateMetronome(object sender, ValueChangedEventArgs args)
    {
        DataSnapshot snapshop = args.Snapshot;
        if ((bool)snapshop.Value)
        {
            HandleSlideRollChange();
            EndTrigger("metronome");
        }
    }

    public void HandleUpdateCalibration(object sender, ValueChangedEventArgs args)
    {
        DataSnapshot snapshop = args.Snapshot;
        if ((bool)snapshop.Value)
        {
            _songController.ToggleCalibration();
            EndTrigger("calibration");
        }
    }

    public void EndTrigger(string trigger)
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
                reference.Child("trigger/" + trigger).SetValueAsync(false);
            }
        });
    }

    #endregion

    public void HandleUpdateSongIndex(object sender, ValueChangedEventArgs args)
    {
        DataSnapshot snapshop = args.Snapshot;

        _songController.DoReset(int.Parse(snapshop.Value.ToString()));
    }

    public void HandleUpdateEyeData(object sender, ValueChangedEventArgs args)
    {
        FirebaseDatabase.DefaultInstance.GetReference("eyeData").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to read eyeData: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                if (task.Result.Value == null)
                {
                    _sendEyeData = true;
                }
            }
        });
    }

    public void CollectEyeData(EyeTracking.Side side, string value)
    {
        if (side == EyeTracking.Side.Left) _leftEyeData.Add(value);
        if (side == EyeTracking.Side.Right) _rightEyeData.Add(value);
    }

    IEnumerator SendEyeData()
    {
        yield return new WaitForSeconds(3);
        yield return new WaitUntil(() => _leftEyeData.Count > 0 || _rightEyeData.Count > 0);

        Dictionary<string, object> updates = new Dictionary<string, object>();
        for (int i = 0; i < _leftEyeData.Count; i++) updates["L" + i] = _leftEyeData[i];
        for (int i = 0; i < _rightEyeData.Count; i++) updates["R" + i] = _rightEyeData[i];

        _leftEyeData.Clear();
        _rightEyeData.Clear();

        FirebaseDatabase.DefaultInstance.GetReference("eyeData").UpdateChildrenAsync(updates).ContinueWith(updateTask =>
        {
            if (updateTask.IsFaulted)
            {
                Debug.LogError("Failed to update eyeData: " + updateTask.Exception);
            }
            else if (updateTask.IsCompleted)
            {
                Debug.Log("eyeData updated successfully!");
            }
        });
    }

    public void RequestRecordTime()
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
                reference.Child("recordTime").SetValueAsync(-1f);
            }
        });
    }

    public void HandleUpdateRecordTime(object sender, ValueChangedEventArgs args)
    {
        DataSnapshot snapshop = args.Snapshot;
        _recordTimeOffset = float.Parse(snapshop.Value.ToString());
    }

    public void HandleSlideRollChange()
    {
        // if (_pianoRollObject.activeSelf)
        // {
        //     _pianoRollObject.SetActive(false);
        //     _pianoSlideObject.SetActive(true);
        // }
        // else
        // {
        //     _pianoRollObject.SetActive(true);
        //     _pianoSlideObject.SetActive(false);
        // }

        if (slideActive)
        {
            _pianoRollObject.transform.localScale = new Vector3(1, 1, 1);
            _pianoSlideObject.transform.localScale = new Vector3(0, 0, 0);
        }
        else
        {
            _pianoRollObject.transform.localScale = new Vector3(0, 0, 0);
            _pianoSlideObject.transform.localScale = new Vector3(1, 1, 1);
        }

        slideActive = !slideActive;

    }
}
