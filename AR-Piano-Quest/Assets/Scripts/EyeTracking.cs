using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeTracking : MonoBehaviour
{
    public enum Side { Left, Right }
    [SerializeField] Side _side;

    Vector3 _lastSentPosition;
    Quaternion _lastSentRotation;

    [SerializeField] Transform _pianoRollTransform;
    [SerializeField] FirebaseManager _firebaseManager;

    void Update()
    {
        Vector3 relativePosition = _pianoRollTransform.InverseTransformPoint(transform.position);
        Quaternion relativeRotation = Quaternion.Inverse(_pianoRollTransform.rotation) * transform.rotation;

        if (PianoRoll.GetGlobalTime() > 0)
        {
            if (transform.position != Vector3.zero || transform.rotation != Quaternion.identity)
            {
                if (Vector3.Distance(relativePosition, _lastSentPosition) > 0.0001f || Quaternion.Angle(relativeRotation, _lastSentRotation) > 0.1f)
                {
                    string value = Round(PianoRoll.GetGlobalTime() + _firebaseManager.RecordTimeOffset, 3) + "";

                    value += "," + Round(relativePosition.x);
                    value += "," + Round(relativePosition.y);
                    value += "," + Round(relativePosition.z);

                    value += "," + Round(relativeRotation.x);
                    value += "," + Round(relativeRotation.y);
                    value += "," + Round(relativeRotation.z);
                    value += "," + Round(relativeRotation.w);

                    _firebaseManager.CollectEyeData(_side, value);

                    _lastSentPosition = relativePosition;
                    _lastSentRotation = relativeRotation;
                }
            }
        }
    }

    float Round(float value, int dp = 5)
    {
        float multiplier = Mathf.Pow(10, dp);
        return (Mathf.Round(value * multiplier) / multiplier);
    }
}
