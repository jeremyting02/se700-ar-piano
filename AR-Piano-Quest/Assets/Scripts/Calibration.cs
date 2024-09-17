using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Calibration : MonoBehaviour
{
    [SerializeField] Transform _leftController;
    [SerializeField] Transform _rightController;

    void Update()
    {
        if (_leftController.position != _rightController.position && OVRInput.Get(OVRInput.Button.One))
        {
            transform.position = Vector3.Lerp(_leftController.position, _rightController.position, 0.5f);
            transform.LookAt(_leftController);
            transform.Rotate(new Vector3(0, 90, 0));
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }
    }
}