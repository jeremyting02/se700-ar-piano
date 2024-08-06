using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeDataPlayer : MonoBehaviour
{
    public static EyeDataPlayer Left;
    public static EyeDataPlayer Right;

    public enum Side { Left, Right }
    [SerializeField] Side _side;

    float _time;
    float _endTime;
    List<Session.EyeInfo> _infoToPlay = new List<Session.EyeInfo>();

    private void Awake()
    {
        if (_side == Side.Left) Left = this;
        if (_side == Side.Right) Right = this;
    }

    public void Play(Session.EyeInfo[] eyeInfos, float start = 0, float end = 99999)
    {
        _time = start;
        _endTime = end;
        _infoToPlay.Clear();

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        for (int i = 0; i < eyeInfos.Length; i++)
        {
            if (eyeInfos[i].Time < start || eyeInfos[i].Time > _endTime)
                continue;

            _infoToPlay.Add(eyeInfos[i]);
        }
    }

    void Update()
    {
        if (_time < _endTime)
        {
            _time += Time.deltaTime;
        }

        while (_infoToPlay.Count > 0 && _infoToPlay[0].Time <= _time)
        {
            transform.position = _infoToPlay[0].Position;
            transform.rotation = _infoToPlay[0].Rotation;

            _infoToPlay.RemoveAt(0);
        }

        if (_time >= _endTime && (transform.position != Vector3.zero || transform.rotation != Quaternion.identity))
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }
}
