using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Analyser : MonoBehaviour
{
    [Header("### INPUT ###")]
    [SerializeField] int _userId;
    int _prevUserId = -1;
    [SerializeField] int _viewPractice;
    int _prevViewedPractice = -1;
    [SerializeField] int _songIndex;
    int _prevSongIndex = -1;
    [SerializeField] List<int> _analysisRange = new List<int>();
    int[] _prevRangeAnalysed = new int[] { -1 };

    [SerializeField] float _predictionThreshold = 0.5f;
    [SerializeField] float _corruptTimeThreshold = 3f;
    [SerializeField] float _corruptFocusThreshold = 3f;

    [Header("### - ###")]
    [Space]

    float _viewPredictedStart = 0;
    float _viewPredictedEnd = 99999;

    [SerializeField] GameObject _indicatorTemplate;
    [SerializeField] GameObject _noteTemplate;
    [SerializeField] GameObject _keyPressTemplate;

    [SerializeField] Transform _generationParent;

    class Song
    {
        float _tempo;
        public float Tempo { get { return _tempo; } }
        float _totalLength;
        public float TotalLength { get { return _totalLength; } }
        int _totalNotes;
        public float TotalNotes { get { return _totalNotes; } }

        Dictionary<int, List<Session.PressInfo>> _keyPresses = new Dictionary<int, List<Session.PressInfo>>();
        public Dictionary<int, Session.PressInfo[]> KeyPresses
        {
            get
            {
                Dictionary<int, Session.PressInfo[]> newInfo = new Dictionary<int, Session.PressInfo[]>();
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

        public void InputNotes(int key, float timeOffset, params float[] info)
        {
            key += 21; // Adjusted for keyboard

            if (!_keyPresses.ContainsKey(key)) _keyPresses[key] = new List<Session.PressInfo>();

            for (int i = 0; i < info.Length; i += 2)
            {
                float start = info[i] + timeOffset;
                _keyPresses[key].Add(new Session.PressInfo(start / _tempo, (start + info[i + 1]) / _tempo));
            }

            // Add to total notes
            _totalNotes += info.Length / 2;
        }
    }

    List<Song> _songs = new List<Song>();

    [Space]

    [Header("### OUTPUT ###")]
    [SerializeField] float _STE_avg = -1;
    [SerializeField] float _ETE_avg = -1;

    [SerializeField] float _MSC_avg = -1;
    [SerializeField] float _MEC_avg = -1;

    [SerializeField] float _ESC_avg = -1;
    [SerializeField] float _EEC_avg = -1;

    [Space]

    [SerializeField] float _eyeMovement_avg = -1;
    [SerializeField] float _focusSwitches_avg = -1;
    [SerializeField] float _PR_focusTime_avg = -1;
    [SerializeField] float _meanFocusDepth_avg = -1;

    [Space]

    [SerializeField] string _copyPaste;

    private void Start()
    {
        SaveAndLoad.Load();

        // Generate song info
        GenerateSongs();
    }

    void Update()
    {
        if (_prevUserId != _userId || _prevViewedPractice != _viewPractice || _prevSongIndex != _songIndex)
        {
            ViewPractice();
        }

        if (_prevUserId != _userId || !SameRange() || _prevSongIndex != _songIndex)
        {
            CalculateDependents();
        }

        if (Input.GetKeyDown("e"))
        {
            PlayEyeData();
        }

        if (Input.GetKeyDown("t"))
        {
            PrintTimeCorruption();
        }

        _prevViewedPractice = _viewPractice;
        _prevUserId = _userId;
        _prevRangeAnalysed = _analysisRange.ToArray();
        _prevSongIndex = _songIndex;
    }


    void ViewPractice()
    {
        Song song = _songs[_songIndex];

        foreach (Transform t in _generationParent.GetComponentsInChildren<Transform>())
        {
            if (t == _generationParent)
                continue;

            Destroy(t.gameObject);
        }

        Session session = SaveAndLoad.data.GetSession(_userId);
        Dictionary<int, Session.PressInfo[]> pressInfo = session.KeyPresses;

        // Raw timeframe
        float rollStart = pressInfo[108][_viewPractice].Start;
        float rollEnd = 99999;
        if (_viewPractice + 1 < pressInfo[108].Length)
            rollEnd = pressInfo[108][_viewPractice + 1].Start;

        // Visualise raw end
        GameObject endIndicator = Instantiate(_indicatorTemplate, _generationParent);
        endIndicator.transform.localPosition = new Vector3(rollEnd - rollStart, _indicatorTemplate.transform.localPosition.y, 0);
        endIndicator.transform.localScale = _indicatorTemplate.transform.localScale;
        endIndicator.name = "End Indicator";

        // Soft timeframe
        float softStart = rollEnd;
        foreach (KeyValuePair<int, Session.PressInfo[]> keyPresses in pressInfo)
        {
            if (!(keyPresses.Key >= 23 && keyPresses.Key <= 105))
                continue;

            foreach (Session.PressInfo timing in keyPresses.Value)
            {
                if (timing.Start > rollStart && timing.Start < softStart)
                {
                    softStart = timing.Start;
                }
            }
        }

        // Predicted timeframe
        float predictedStart = 0;
        int offsets = 0;
        foreach (KeyValuePair<int, Session.PressInfo[]> note in song.KeyPresses)
        {
            foreach (Session.PressInfo noteTiming in note.Value)
            {
                if (!pressInfo.ContainsKey(note.Key))
                    continue;

                // Relative to softstart (inital predictedStart)
                float noteStart = noteTiming.Start;

                float closestOffset = 99999;
                float closestDistance = 99999;

                foreach (Session.PressInfo keyTiming in pressInfo[note.Key])
                {
                    // Relative to softstart
                    float keyStart = keyTiming.Start - softStart;

                    float offset = keyStart - noteStart;

                    if (Mathf.Abs(offset) > _predictionThreshold || Mathf.Abs(offset) > closestDistance)
                        continue;

                    closestOffset = offset;
                    closestDistance = Mathf.Abs(offset);
                }

                if (closestOffset == 99999)
                    continue;

                predictedStart += closestOffset;
                offsets++;
            }
        }
        if (offsets > 0)
            predictedStart /= offsets;
        predictedStart += softStart;

        // Visualise predicted start
        GameObject predictedStartIndicator = Instantiate(_indicatorTemplate, _generationParent);
        predictedStartIndicator.transform.localPosition = new Vector3(softStart - rollStart, _indicatorTemplate.transform.localPosition.y, 0);
        predictedStartIndicator.transform.localScale = _indicatorTemplate.transform.localScale;
        predictedStartIndicator.name = "Predicted Start Indicator";

        // Visualise predicted end
        float predictedEnd = predictedStart + (song.TotalLength / song.Tempo);
        GameObject predictedEndIndicator = Instantiate(_indicatorTemplate, _generationParent);
        predictedEndIndicator.transform.localPosition = new Vector3(predictedEnd - rollStart, _indicatorTemplate.transform.localPosition.y, 0);
        predictedEndIndicator.transform.localScale = _indicatorTemplate.transform.localScale;
        predictedEndIndicator.name = "Predicted End Indicator";

        // Visualise pressed keys
        foreach (KeyValuePair<int, Session.PressInfo[]> keyPresses in pressInfo)
        {
            if (!(keyPresses.Key >= 23 && keyPresses.Key <= 105))
                continue;

            foreach (Session.PressInfo timing in keyPresses.Value)
            {
                if (timing.Start < rollStart || timing.Start > rollEnd)
                    continue;

                GameObject newKeyPress = Instantiate(_keyPressTemplate, _generationParent);
                newKeyPress.SetActive(true);
                newKeyPress.transform.localPosition = new Vector3(timing.Start + (timing.Length / 2) - rollStart, keyPresses.Key, 0);
                newKeyPress.transform.localScale = new Vector3(timing.Length, 1, 1);
            }
        }

        // Visualise notes from song
        foreach (KeyValuePair<int, Session.PressInfo[]> note in song.KeyPresses)
        {
            foreach (Session.PressInfo timing in note.Value)
            {
                GameObject newNote = Instantiate(_noteTemplate, _generationParent);
                newNote.SetActive(true);
                newNote.transform.localPosition = new Vector3(timing.Start + (timing.Length / 2) + predictedStart - rollStart, note.Key, 0);
                newNote.transform.localScale = new Vector3(timing.Length, 1, 1);
            }
        }

        _viewPredictedStart = predictedStart;
        _viewPredictedEnd = predictedEnd;
    }

    void CalculateDependents()
    {
        int[] practiceIndices = _analysisRange.ToArray();

        float STE_total = 0;
        float ETE_total = 0;
        float MSC_total = 0;
        float MEC_total = 0;
        float ESC_total = 0;
        float EEC_total = 0;

        float eyeMovementTotal = 0;
        float focusSwitchesTotal = 0;
        float PR_focusTimeTotal = 0;
        float meanFocusDepthTotal = 0;

        int eyeDataCount = practiceIndices.Length;

        foreach (int practiceIndex in practiceIndices)
        {
            Song song = _songs[_songIndex];
            float beatLength = 1f / song.Tempo;

            Session session = SaveAndLoad.data.GetSession(_userId);
            Dictionary<int, Session.PressInfo[]> pressInfo = session.KeyPresses;

            #region Press data

            float STE = 0;
            float ETE = 0;
            float MSC = 0;
            float MEC = 0;
            float ESC = 0;
            float EEC = 0;

            // Raw timeframe
            float rollStart = pressInfo[108][practiceIndex].Start;
            float rollEnd = 99999;
            if (practiceIndex + 1 < pressInfo[108].Length)
                rollEnd = pressInfo[108][practiceIndex + 1].Start;

            // Soft timeframe
            float softStart = rollEnd;
            foreach (KeyValuePair<int, Session.PressInfo[]> keyPresses in pressInfo)
            {
                if (!(keyPresses.Key >= 23 && keyPresses.Key <= 105))
                    continue;

                foreach (Session.PressInfo timing in keyPresses.Value)
                {
                    if (timing.Start > rollStart && timing.Start < softStart)
                    {
                        softStart = timing.Start;
                    }
                }
            }
            // Predicted timeframe
            float predictedStart = 0;
            int offsets = 0;

            // DEBUGGING
            foreach (KeyValuePair<int, Session.PressInfo[]> kvp in pressInfo)
            {
                print("KEY " + kvp.Key + " VALUE " + kvp.Value);
            }

            foreach (KeyValuePair<int, Session.PressInfo[]> note in song.KeyPresses)
            {
                print("note: " + note);
                print("pressInfo: " + pressInfo);

                foreach (Session.PressInfo noteTiming in note.Value)
                {
                    // Relative to softstart (inital predictedStart)
                    float noteStart = noteTiming.Start;

                    float closestOffset = 99999;
                    float closestDistance = 99999;

                    if (pressInfo.ContainsKey(note.Key))
                    {
                        foreach (Session.PressInfo keyTiming in pressInfo[note.Key])
                        {
                            // Relative to softstart
                            float keyStart = keyTiming.Start - softStart;

                            float offset = keyStart - noteStart;

                            if (Mathf.Abs(offset) > _predictionThreshold || Mathf.Abs(offset) > closestDistance)
                                continue;

                            closestOffset = offset;
                            closestDistance = Mathf.Abs(offset);
                        }
                    }
                    else
                    {
                        // Handle the case where the key does not exist
                        Debug.LogWarning($"Key {note.Key} not found in pressInfo.");
                    }

                    if (closestOffset == 99999)
                        continue;

                    predictedStart += closestOffset;
                    offsets++;
                }
            }
            if (offsets > 0)
                predictedStart /= offsets;
            predictedStart += softStart;

            float predictedEnd = predictedStart + (song.TotalLength / song.Tempo);

            foreach (KeyValuePair<int, Session.PressInfo[]> note in song.KeyPresses)
            {
                List<int> mappedStarts = new List<int>();
                List<int> mappedEnds = new List<int>();

                foreach (Session.PressInfo timing in note.Value)
                {
                    // Relative to rollStart
                    float noteStart = timing.Start + (predictedStart - rollStart);
                    float noteEnd = noteStart + timing.Length;

                    float bestStartDistance = 99999;
                    int bestStartIndex = -1;
                    float bestEndDistance = 99999;
                    int bestEndIndex = -1;

                    for (int i = 0; i < pressInfo[note.Key].Length; i++)
                    {
                        Session.PressInfo keyTiming = pressInfo[note.Key][i];

                        // Relative to rollStart
                        float keyStart = keyTiming.Start - rollStart;
                        float keyEnd = keyStart + keyTiming.Length;

                        float startDistance = Mathf.Abs(keyStart - noteStart);
                        float endDistance = Mathf.Abs(keyEnd - noteEnd);

                        if (startDistance < bestStartDistance && !mappedStarts.Contains(i) &&
                            keyStart > noteStart - (beatLength / 2) && keyStart < noteEnd)
                        {
                            bestStartDistance = startDistance;
                            bestStartIndex = i;
                        }

                        if (endDistance < bestEndDistance && !mappedEnds.Contains(i) &&
                            keyEnd < noteEnd + (beatLength / 2) && keyEnd > noteStart)
                        {
                            bestEndDistance = endDistance;
                            bestEndIndex = i;
                        }
                    }

                    // Add Start Time Error (STR)
                    if (bestStartIndex != -1)
                    {
                        STE += bestStartDistance;
                        mappedStarts.Add(bestStartIndex);
                    }
                    // Add End Time Error (ETE)
                    if (bestEndIndex != -1)
                    {
                        ETE += bestEndDistance;
                        mappedEnds.Add(bestEndIndex);
                    }
                }

                // Tally Mapped Starts for MSC
                MSC += mappedStarts.Count;
                // Tally Mapped Starts for MEC
                MEC += mappedEnds.Count;

                for (int i = 0; i < pressInfo[note.Key].Length; i++)
                {
                    Session.PressInfo keyTiming = pressInfo[note.Key][i];

                    if (keyTiming.Start < rollStart || keyTiming.Start > predictedEnd)
                        continue;

                    // Add Extra Start Count (ESC)
                    if (!mappedStarts.Contains(i)) ESC++;
                    // Add Extra End Count (EEC)
                    if (!mappedEnds.Contains(i)) EEC++;
                }
            }
            foreach (KeyValuePair<int, Session.PressInfo[]> keyPresses in pressInfo)
            {
                if (song.KeyPresses.ContainsKey(keyPresses.Key) || !(keyPresses.Key >= 23 && keyPresses.Key <= 105))
                    continue;

                foreach (Session.PressInfo timing in keyPresses.Value)
                {
                    if (timing.Start < rollStart || timing.Start > predictedEnd)
                        continue;

                    // Add Extra Start Count (ESC) and Extra End Count (EEC)
                    ESC++;
                    EEC++;
                }
            }

            // Calculate Missed Start Counts (MSC)
            MSC = song.TotalNotes - MSC;
            // Calcualte Missed End Counts (MEC)
            MEC = song.TotalNotes - MEC;

            // Add errors to total errors
            STE_total += STE;
            ETE_total += ETE;
            MSC_total += MSC;
            MEC_total += MEC;
            ESC_total += ESC;
            EEC_total += EEC;

            #endregion

            #region Eye data

            float eyeMovement = 0;
            float focusSwitches = 0;
            float PR_focusTime = 0;
            float focusDepth = 0;
            int focusDepthMeasures = 0;

            Session.EyeInfo[] leftEyeData = session.LeftEyeData;
            Session.EyeInfo[] rightEyeData = session.RightEyeData;

            bool corrupted = false;
            float prevEyeTime = predictedStart;
            float focusAwayTime = 0;

            // Left eye
            for (int l = 0; l < leftEyeData.Length; l++)
            {
                if (leftEyeData[l].Time < predictedStart)
                    continue;

                if (leftEyeData[l].Time > predictedEnd)
                    break;

                if (l + 1 < leftEyeData.Length)
                {
                    float distanceToPrev = leftEyeData[l].Time - prevEyeTime;
                    prevEyeTime = leftEyeData[l].Time;

                    // Detect time corruption
                    if (distanceToPrev >= _corruptTimeThreshold)
                    {
                        Debug.LogError("Corrupted time in left eye in #" + practiceIndex);
                        corrupted = true;
                        break;
                    }

                    // Add Eye Movement
                    eyeMovement += Quaternion.Angle(leftEyeData[l].Rotation, leftEyeData[l + 1].Rotation);

                    Plane plane = new Plane(Vector3.up, 0);

                    Ray currentRay = new Ray(leftEyeData[l].Position, leftEyeData[l].Rotation * Vector3.forward);
                    float currentDistance = -1;
                    plane.Raycast(currentRay, out currentDistance);
                    Vector3 currentPoint = currentRay.GetPoint(currentDistance);
                    bool currentPianoRollFocus = currentPoint.z > 0;

                    // Add to Cumulative focusDepth
                    focusDepth += currentPoint.z;
                    focusDepthMeasures++;

                    Ray nextRay = new Ray(leftEyeData[l + 1].Position, leftEyeData[l + 1].Rotation * Vector3.forward);
                    float nextDistance = -1;
                    plane.Raycast(nextRay, out nextDistance);
                    Vector3 nextPoint = nextRay.GetPoint(nextDistance);
                    bool nextPianoRollFocus = nextPoint.z > 0;

                    // Add Piano Roll Focus Time (PR Focus Time)
                    if (currentPianoRollFocus && nextPianoRollFocus)
                    {
                        PR_focusTime += leftEyeData[l + 1].Time - leftEyeData[l].Time;
                    }

                    // Add Focus Switch
                    if (currentPianoRollFocus != nextPianoRollFocus)
                    {
                        focusSwitches++;
                    }

                    // Detect focus corruption
                    if (Mathf.Abs(currentPoint.x) > 1 && Mathf.Abs(nextPoint.x) > 1)
                    {
                        focusAwayTime += leftEyeData[l + 1].Time - leftEyeData[l].Time;

                        // Found Corruption
                        if (focusAwayTime > _corruptFocusThreshold)
                        {
                            Debug.LogError("Corrupted focus in left eye in #" + practiceIndex);
                            corrupted = true;
                            break;
                        }
                    }
                }
            }
            // Detect time corruption (from predicted end to last recorded eye measure)
            if (!corrupted && predictedEnd - prevEyeTime >= _corruptTimeThreshold)
            {
                Debug.LogError("Corrupted time (ended early) in left eye in #" + practiceIndex);
                corrupted = true;
            }

            // Right eye
            if (!corrupted)
            {
                focusAwayTime = 0;
                prevEyeTime = predictedStart;

                for (int r = 0; r < rightEyeData.Length; r++)
                {
                    if (rightEyeData[r].Time < predictedStart)
                        continue;

                    if (rightEyeData[r].Time > predictedEnd)
                        break;

                    if (r + 1 < rightEyeData.Length)
                    {
                        float distanceToPrev = rightEyeData[r].Time - prevEyeTime;
                        prevEyeTime = rightEyeData[r].Time;

                        // Detect time corruption
                        if (distanceToPrev >= _corruptTimeThreshold)
                        {
                            Debug.LogError("Corrupted time in right eye in #" + practiceIndex);
                            corrupted = true;
                            break;
                        }

                        // Add Eye Movement
                        eyeMovement += Quaternion.Angle(rightEyeData[r].Rotation, rightEyeData[r + 1].Rotation);

                        Plane plane = new Plane(Vector3.up, 0);

                        Ray currentRay = new Ray(rightEyeData[r].Position, rightEyeData[r].Rotation * Vector3.forward);
                        float currentDistance = -1;
                        plane.Raycast(currentRay, out currentDistance);
                        Vector3 currentPoint = currentRay.GetPoint(currentDistance);
                        bool currentPianoRollFocus = currentPoint.z > 0;

                        // Add to Cumulative focusDepth
                        focusDepth += currentPoint.z;
                        focusDepthMeasures++;

                        Ray nextRay = new Ray(rightEyeData[r + 1].Position, rightEyeData[r + 1].Rotation * Vector3.forward);
                        float nextDistance = -1;
                        plane.Raycast(nextRay, out nextDistance);
                        Vector3 nextPoint = nextRay.GetPoint(nextDistance);
                        bool nextPianoRollFocus = nextPoint.z > 0;

                        // Add Piano Roll Focus Time (PR Focus Time)
                        if (currentPianoRollFocus && nextPianoRollFocus)
                        {
                            PR_focusTime += rightEyeData[r + 1].Time - rightEyeData[r].Time;
                        }

                        // Add Focus Switch
                        if (currentPianoRollFocus != nextPianoRollFocus)
                        {
                            focusSwitches++;
                        }

                        // Detect focus corruption
                        if (Mathf.Abs(currentPoint.x) > 1 && Mathf.Abs(nextPoint.x) > 1)
                        {
                            focusAwayTime += rightEyeData[r + 1].Time - rightEyeData[r].Time;

                            // Found Corruption
                            if (focusAwayTime > _corruptFocusThreshold)
                            {
                                Debug.LogError("Corrupted focus in right eye in #" + practiceIndex);
                                corrupted = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Detect time corruption (no eye measures or from predicted end to last recorded eye measure)
            if (!corrupted && (prevEyeTime == predictedStart || predictedEnd - prevEyeTime >= _corruptTimeThreshold))
            {
                Debug.LogError("Corrupted time (ended early) in right eye in #" + practiceIndex);
                corrupted = true;
            }

            // Ignore Eye Data (due to corruption)
            if (corrupted)
            {
                eyeDataCount--;
            }
            else // Add Eye Data
            {
                eyeMovementTotal += eyeMovement;
                focusSwitchesTotal += focusSwitches;
                PR_focusTimeTotal += PR_focusTime;
                meanFocusDepthTotal += focusDepth / focusDepthMeasures;
            }

            #endregion
        }

        #region Data To Text (+Averaging)

        _STE_avg = STE_total / practiceIndices.Length;
        _ETE_avg = ETE_total / practiceIndices.Length;
        _MSC_avg = MSC_total / practiceIndices.Length;
        _MEC_avg = MEC_total / practiceIndices.Length;
        _ESC_avg = ESC_total / practiceIndices.Length;
        _EEC_avg = EEC_total / practiceIndices.Length;
        string pressDataString = _STE_avg + "\t" + _ETE_avg + "\t" + _MSC_avg + "\t" + _MEC_avg + "\t" + _ESC_avg + "\t" + _EEC_avg;

        string eyeDataString = "-\t-\t-\t-";
        if (eyeDataCount > 0)
        {
            _eyeMovement_avg = eyeMovementTotal / eyeDataCount;
            _focusSwitches_avg = focusSwitchesTotal / eyeDataCount;
            _PR_focusTime_avg = PR_focusTimeTotal / eyeDataCount;
            _meanFocusDepth_avg = meanFocusDepthTotal / eyeDataCount;

            eyeDataString = _eyeMovement_avg + "\t" + _focusSwitches_avg + "\t" + _PR_focusTime_avg + "\t" + _meanFocusDepth_avg;
        }
        else
        {
            _eyeMovement_avg = -1;
            _focusSwitches_avg = -1;
            _PR_focusTime_avg = -1;
            _meanFocusDepth_avg = -1;
        }

        _copyPaste = pressDataString + "\t" + eyeDataString;

        #endregion
    }

    void PlayEyeData()
    {
        Debug.Log("Playing eye data from practice: " + _viewPractice);

        Session session = SaveAndLoad.data.GetSession(_userId);
        session.PlayEyeData(false, _viewPredictedStart, _viewPredictedEnd);
    }

    void PrintTimeCorruption()
    {
        Session session = SaveAndLoad.data.GetSession(_userId);
        Dictionary<int, Session.PressInfo[]> pressInfo = session.KeyPresses;
        Session.EyeInfo[] leftEyeData = session.LeftEyeData;

        float prevEyeTime = 0;

        string playPauseTimes = "";
        string corruptionStartTimes = "";
        string corruptionLengths = "";

        foreach (Session.PressInfo timing in pressInfo[108])
        {
            playPauseTimes += timing.Start + "\n";
        }

        // Left eye
        for (int l = 0; l < leftEyeData.Length; l++)
        {
            if (l + 1 < leftEyeData.Length)
            {
                float distanceToPrev = leftEyeData[l].Time - prevEyeTime;
                prevEyeTime = leftEyeData[l].Time;

                if (distanceToPrev > 3) // HARDCODED
                {
                    corruptionStartTimes += leftEyeData[l].Time - distanceToPrev + "\n";
                    corruptionLengths += distanceToPrev + "\n";
                }
            }
        }

        print(playPauseTimes);
        print(corruptionStartTimes);
        print(corruptionLengths);
    }

    #region Helper Functions

    bool SameRange()
    {
        if (_analysisRange.Count == _prevRangeAnalysed.Length)
        {
            for (int i = 0; i < _analysisRange.Count; i++)
            {
                if (_analysisRange[i] != _prevRangeAnalysed[i])
                    return false;
            }

            return true;
        }

        return false;
    }

    #endregion

    #region Songs (Directly copied from PianoRoll.cs)

    void GenerateSongs()
    {
        //JingleBells();

        float slow = 1.25f;

        Tutorial(slow);

        Cuckoo(slow);
        // AuClairDeLaLune(slow);
        // ObLaDiLaDa(fast);

        AlleMeineEntchen(slow);
        // BeutifulBrownEyes(fast);
        // GilligansIsle(slow);
    }

    void Tutorial(float tempo)
    {
        Song newSong = new Song(tempo, 56);

        newSong.InputNotes(39, 0, 0, 1, 8, 1);
        newSong.InputNotes(41, 0, 1, 1, 7, 1);
        newSong.InputNotes(43, 0, 2, 1, 6, 1);
        newSong.InputNotes(44, 0, 3, 1, 5, 1);
        newSong.InputNotes(46, 0, 4, 1);

        newSong.InputNotes(39, 16, 0, 2, 8, 2);
        newSong.InputNotes(43, 16, 2, 2, 6, 2);
        newSong.InputNotes(46, 16, 4, 2);

        newSong.InputNotes(46, 32, 0, 1, 8, 4);
        newSong.InputNotes(48, 32, 1, 1, 7, 1);
        newSong.InputNotes(50, 32, 2, 1, 6, 1);
        newSong.InputNotes(51, 32, 3, 1, 5, 1);
        newSong.InputNotes(53, 32, 4, 1);


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

    // void AuClairDeLaLune(float tempo)
    // {
    //     Song newSong = new Song(tempo, 64);

    //     newSong.InputNotes(34 + 12, 0, 0, 1, 1, 1, 2, 1, 8, 1, 12, 4);
    //     newSong.InputNotes(36 + 12, 0, 3, 1, 6, 2, 10, 1, 11, 1);
    //     newSong.InputNotes(38 + 12, 0, 4, 2, 9, 1);

    //     newSong.InputNotes(34 + 12, 16, 0, 1, 1, 1, 2, 1, 8, 1, 12, 4);
    //     newSong.InputNotes(36 + 12, 16, 3, 1, 6, 2, 10, 1, 11, 1);
    //     newSong.InputNotes(38 + 12, 16, 4, 2, 9, 1);

    //     newSong.InputNotes(36 + 12, 32, 0, 1, 1, 1, 2, 1, 3, 1, 8, 1);
    //     newSong.InputNotes(31 + 12, 32, 4, 2, 6, 2, 11, 1);
    //     newSong.InputNotes(34 + 12, 32, 9, 1);
    //     newSong.InputNotes(33 + 12, 32, 10, 1);
    //     newSong.InputNotes(29 + 12, 32, 12, 4);

    //     newSong.InputNotes(34 + 12, 48, 0, 1, 1, 1, 2, 1, 8, 1, 12, 4);
    //     newSong.InputNotes(36 + 12, 48, 3, 1, 6, 2, 10, 1, 11, 1);
    //     newSong.InputNotes(38 + 12, 48, 4, 2, 9, 1);

    //     _songs.Add(newSong);
    // }

    // void ObLaDiLaDa(float tempo)
    // {
    //     Song newSong = new Song(tempo, 64);

    //     newSong.InputNotes(48, 0, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1);
    //     newSong.InputNotes(46, 0, 6, 1, 9, 2, 11, 5);
    //     newSong.InputNotes(44, 0, 7, 1);
    //     newSong.InputNotes(43, 0, 8, 1);

    //     newSong.InputNotes(49, 16, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1);
    //     newSong.InputNotes(48, 16, 6, 1);
    //     newSong.InputNotes(46, 16, 7, 1);
    //     newSong.InputNotes(44, 16, 8, 8);

    //     newSong.InputNotes(49, 32, 6, 1, 8, 1, 15, 1, 18, 1, 21, 1);
    //     newSong.InputNotes(48, 32, 7, 1, 16, 1, 17, 1, 19, 1, 22, 1);
    //     newSong.InputNotes(46, 32, 20, 1, 23, 1);
    //     newSong.InputNotes(44, 32, 24, 8);
    //     newSong.InputNotes(51, 32, 0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 9, 2, 14, 1);
    //     newSong.InputNotes(53, 32, 11, 2, 13, 1);

    //     _songs.Add(newSong);
    // }

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

    // void BeutifulBrownEyes(float tempo)
    // {
    //     Song newSong = new Song(tempo, 48);

    //     newSong.InputNotes(50, 0, 0, 1, 1, 1, 2, 1, 5, 1);
    //     newSong.InputNotes(46, 0, 3, 1);
    //     newSong.InputNotes(48, 0, 4, 1);
    //     newSong.InputNotes(51, 0, 6, 1, 7, 2, 9, 3);

    //     newSong.InputNotes(50, 12, 0, 1, 1, 1, 2, 1, 5, 1);
    //     newSong.InputNotes(46, 12, 3, 1);
    //     newSong.InputNotes(48, 12, 4, 1, 6, 3);
    //     newSong.InputNotes(53, 12, 9, 3);

    //     newSong.InputNotes(50, 24, 0, 1, 1, 1, 2, 1, 5, 1);
    //     newSong.InputNotes(46, 24, 3, 1);
    //     newSong.InputNotes(48, 24, 4, 1);
    //     newSong.InputNotes(51, 24, 6, 1, 7, 2, 9, 3);

    //     newSong.InputNotes(50, 36, 2, 1, 4, 1);
    //     newSong.InputNotes(46, 36, 6, 6);
    //     newSong.InputNotes(48, 36, 1, 1, 5, 1);
    //     newSong.InputNotes(51, 36, 3, 1);
    //     newSong.InputNotes(53, 36, 0, 1);

    //     _songs.Add(newSong);
    // }


    // void GilligansIsle(float tempo)
    // {
    //     Song newSong = new Song(tempo, 64);

    //     newSong.InputNotes(41, 0, 0, 1, 8, 1);
    //     newSong.InputNotes(48, 0, 1, 1, 2, 1, 3, 1, 9, 1, 10, 1, 11, 1);
    //     newSong.InputNotes(46, 0, 4, 1, 12, 3);
    //     newSong.InputNotes(43, 0, 5, 1);
    //     newSong.InputNotes(39, 0, 6, 1, 7, 1, 15, 1);

    //     newSong.InputNotes(41, 16, 0, 1, 12, 3);
    //     newSong.InputNotes(48, 16, 1, 1, 2, 1, 3, 1, 15, 1);
    //     newSong.InputNotes(46, 16, 4, 1, 7, 1);
    //     newSong.InputNotes(44, 16, 8, 1, 9, 1);
    //     newSong.InputNotes(43, 16, 10, 1);
    //     newSong.InputNotes(39, 16, 11, 1);
    //     newSong.InputNotes(51, 16, 5, 1, 6, 1);

    //     newSong.InputNotes(41, 32, 0, 1, 8, 1);
    //     newSong.InputNotes(48, 32, 1, 1, 2, 1, 3, 1, 9, 1, 10, 1, 11, 1);
    //     newSong.InputNotes(46, 32, 4, 1, 12, 3);
    //     newSong.InputNotes(43, 32, 5, 1);
    //     newSong.InputNotes(39, 32, 6, 1, 7, 1, 15, 1);

    //     newSong.InputNotes(41, 48, 0, 1, 12, 4);
    //     newSong.InputNotes(48, 48, 1, 1, 2, 1, 3, 1);
    //     newSong.InputNotes(46, 48, 4, 1, 7, 1);
    //     newSong.InputNotes(44, 48, 8, 2);
    //     newSong.InputNotes(43, 48, 10, 1);
    //     newSong.InputNotes(39, 48, 11, 1);
    //     newSong.InputNotes(51, 48, 5, 1, 6, 1);

    //     _songs.Add(newSong);
    // }

    #endregion
}
