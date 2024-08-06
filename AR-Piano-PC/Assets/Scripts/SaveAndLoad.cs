using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;
using System.Linq;

public class SaveAndLoad
{
    public static LogData data;

    static string saveToFilename = "logs.dat";

    public static void Load(string filename = "logs.dat")
    {
        if (File.Exists(Application.persistentDataPath + "/" + filename))
        {
            try
            {
                using (Stream stream = File.OpenRead(Application.persistentDataPath + "/" + filename))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    data = (LogData)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
        else
        {
            data = new LogData();
        }

        saveToFilename = filename;

        Debug.Log("Loaded " + filename);
    }

    public static void Save()
    {
        using (Stream stream = File.OpenWrite(Application.persistentDataPath + "/" + saveToFilename))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);

            Debug.Log("Saved " + saveToFilename);
        }
    }
}


[Serializable]
public class LogData
{
    List<Session> _sessions = new List<Session>();

    public int NewSession()
    {
        Session newSession = new Session();
        _sessions.Add(newSession);
        return _sessions.Count - 1;
    }

    public Session GetSession(int index)
    {
        return _sessions[index];
    }

    public int GetNextSessionIndex()
    {
        return _sessions.Count;
    }

    public void PrintSessionData(int index)
    {
        _sessions[index].PrintKeyData();
        _sessions[index].PlayEyeData();
    }
}

[Serializable]
public class Session
{
    #region Press Data

    [Serializable]
    public class PressInfo
    {
        float _start;
        public float Start { get { return _start; } }
        float _length;
        public float Length { get { return _length; } }

        public PressInfo(float start, float end)
        {
            _start = start;
            _length = end - start;
        }
    }

    Dictionary<int, List<PressInfo>> _keyPresses = new Dictionary<int, List<PressInfo>>();
    public Dictionary<int, PressInfo[]> KeyPresses { get { return _keyPresses.ToDictionary(entry => entry.Key,
                                               entry => entry.Value.ToArray()); ; } }

    public void AddKeyPress(int key, float start, float end)
    {
        if (!_keyPresses.ContainsKey(key))
        {
            _keyPresses[key] = new List<PressInfo>();
        }
        _keyPresses[key].Add(new PressInfo(start, end));
    }

    public void PrintKeyData()
    {
        float cutoff = _keyPresses[108][_keyPresses[108].Count - 1].Start;
        float beginning = 99999;

        // Find beginning
        foreach (KeyValuePair<int, List<PressInfo>> press in _keyPresses)
        {
            if (press.Key >= 23 && press.Key <= 105)
            {
                foreach (PressInfo timing in press.Value)
                {
                    if (timing.Start > cutoff && timing.Start < beginning)
                    {
                        beginning = timing.Start;
                    }
                }
            }
        }

        Debug.Log("Info for song creation:");

        string log = "";

        foreach (KeyValuePair<int, List<PressInfo>> press in _keyPresses)
        {
            if (press.Key >= 23 && press.Key <= 105)
            {
                string line = "newSong.InputNotes(" + (press.Key - 21) + ", #";
                bool afterCutOff = false;

                foreach (PressInfo timing in press.Value)
                {
                    if (timing.Start > cutoff)
                    {
                        line += ", " + Mathf.Round((timing.Start - beginning) * 2) + ", " + (Mathf.Round(timing.Length * 2));
                        afterCutOff = true;
                    }
                }

                line += ");";

                if (afterCutOff)
                {
                    log += line + "\n";
                }
            }
        }

        Debug.Log(log);
    }

    #endregion

    #region Eye Data

    [Serializable]
    public class EyeInfo
    {
        float _time;
        public float Time { get { return _time; } }
        float[] _position;
        public Vector3 Position { get { return new Vector3(_position[0], _position[1], _position[2]); } }
        float[] _rotation;
        public Quaternion Rotation { get { return new Quaternion(_rotation[0], _rotation[1], _rotation[2], _rotation[3]); } }

        public EyeInfo(string value)
        {
            string[] valueArray = value.Split(",");

            _time = float.Parse(valueArray[0]);

            _position = new float[3] { float.Parse(valueArray[1]), float.Parse(valueArray[2]), float.Parse(valueArray[3]) };

            _rotation = new float[4] { float.Parse(valueArray[4]), float.Parse(valueArray[5]), float.Parse(valueArray[6]), float.Parse(valueArray[7]) };
        }

        public EyeInfo(EyeInfo brokenInfo, float offset)
        {
            _time = brokenInfo.Time + offset;

            _position = new float[3] { brokenInfo.Position.x, brokenInfo.Position.y, brokenInfo.Position.z };
            _rotation = new float[4] { brokenInfo.Rotation.x, brokenInfo.Rotation.y, brokenInfo.Rotation.z, brokenInfo.Rotation.w };

        }
    }

    List<EyeInfo> _leftEyeData = new List<EyeInfo>();
    public EyeInfo[] LeftEyeData { get { return GetFixedEyeData(_leftEyeData.ToArray()); } }

    List<EyeInfo> _rightEyeData = new List<EyeInfo>();
    public EyeInfo[] RightEyeData { get { return GetFixedEyeData(_rightEyeData.ToArray()); } }

    public void AddEyeData(string key, string value)
    {
        List<EyeInfo> eyeData = new List<EyeInfo>();
        if (key[0] == 'L') eyeData = _leftEyeData;
        else if (key[0] == 'R') eyeData = _rightEyeData;
        else Debug.LogError("Incorrect key format: " + key);

        EyeInfo newInfo = new EyeInfo(value);
        int index = 0;
        for (int i = 0; i < eyeData.Count; i++)
        {
            if (newInfo.Time > eyeData[i].Time)
            {
                index = i;
            }
        }
        if (index < eyeData.Count) eyeData.Insert(index + 1, newInfo);
        else eyeData.Add(newInfo);
    }

    public EyeInfo[] GetFixedEyeData(EyeInfo[] eyeData)
    {
        List<EyeInfo> fixedEyeData = new List<EyeInfo>();

        // Get times when "Play/Pause button" was pressed
        Dictionary<int, PressInfo[]> pressInfo = KeyPresses;
        float[] playPauseTimes = new float[pressInfo[108].Length];
        for (int i = 0; i < pressInfo[108].Length; i++)
        {
            playPauseTimes[i] = pressInfo[108][i].Start;
        }

        // Group eye data by seperated gaps
        List<Vector3> groups = new List<Vector3>();
        int prevEnd = -1;
        for (int i = 1; i < eyeData.Length; i++)
        {
            float corruptTimeThreshold = 5;
            int playIndex = groups.Count;
            if (playIndex + 1 < playPauseTimes.Length)
            {
                corruptTimeThreshold = Mathf.Min(corruptTimeThreshold, (playPauseTimes[playIndex + 1] - playPauseTimes[playIndex]) / 2);
            }

            float distanceToPrev = eyeData[i].Time - eyeData[i - 1].Time;

            if (distanceToPrev > corruptTimeThreshold)
            {
                groups.Add(new Vector3(prevEnd + 1, i - 1, distanceToPrev));
                prevEnd = i - 1;
            }
        }
        // Catch last group
        groups.Add(new Vector3(prevEnd + 1, eyeData.Length - 1, 99999));

        // Foreach play/pause
        for (int i = 0; i < playPauseTimes.Length; i++)
        {
            if (groups.Count == 1 && i < playPauseTimes.Length - 1)
                break;

            float offset = playPauseTimes[i] - eyeData[(int)groups[0].x].Time;

            // Foreach eyeData in matched group
            for (int j = (int)groups[0].x; j <= groups[0].y; j++)
            {
                fixedEyeData.Add(new EyeInfo(eyeData[j], offset));
            }

            groups.RemoveAt(0);
        }

        #region Legacy
        // Correct corruption
        /*for (int i = 1; i < eyeData.Length; i++)
        {
            float currentTime = eyeData[i].Time; // DEBUG
            float prevTime = eyeData[i - 1].Time; // DEBUG
            float pauseTime = playPauseTimes[checkIndex]; // DEBUG

            float distanceToPrev = eyeData[i].Time - eyeData[i - 1].Time;

            // Corruption detected at play/pause boundary
            if (distanceToPrev > corruptionThreshold && checkIndex < playPauseTimes.Length)
            {
                if (Mathf.Abs(playPauseTimes[checkIndex] - (eyeData[i - 1].Time + timeOffset)) < corruptionThreshold)
                {
                    timeOffset -= playPauseTimes[checkIndex] - playPauseTimes[checkIndex - 1]; // Difference of starts
                    //timeOffset -= distanceToPrev + (eyeData[i - 1].Time - eyeData[i - 2].Time); // Close gap
                    checkIndex++;
                }
            }

            // Add eye data with fixed timing
            fixedEyeData.Add(new EyeInfo(eyeData[i], timeOffset));
        }*/
        #endregion

        return fixedEyeData.ToArray();
    }

    public void PlayEyeData(bool logToConsole = true, float start = 0, float end = 99999)
    {
        if (logToConsole)
        {
            Debug.Log("Eye Data:");

            foreach (EyeInfo eyeInfo in LeftEyeData) Debug.Log("L: " + eyeInfo.Time + " " + eyeInfo.Position + " " + eyeInfo.Rotation);
            foreach (EyeInfo eyeInfo in RightEyeData) Debug.Log("R: " + eyeInfo.Time + " " + eyeInfo.Position + " " + eyeInfo.Rotation);
        }

        EyeDataPlayer.Left.Play(LeftEyeData, start, end);
        EyeDataPlayer.Right.Play(RightEyeData, start, end);
    }

    #endregion
}