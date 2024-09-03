using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideGrid : MonoBehaviour
{
    [SerializeField] GameObject _exampleVisual;
    [SerializeField] Transform _keyColumnsParent;
    [SerializeField] int _numberOfClones = 10;
    [SerializeField] float _xInterval = 0.1f;

    private void Start()
    {
        CreateKeyColumns();
    }

    void CreateKeyColumns()
    {
        for (int i = 0; i < _numberOfClones; i++)
        {
            // Instantiate a clone of the example visual
            GameObject clone = Instantiate(_exampleVisual, _keyColumnsParent);

            // Calculate the x position based on the interval and index
            float xPos = i * _xInterval;

            // Set the position of the clone
            clone.transform.localPosition = new Vector3(xPos, 0, 0);

            // Optionally, you can customize each clone (e.g., change color)
            // clone.GetComponent<Renderer>().material.color = Color.Lerp(Color.white, Color.black, (float)i / _numberOfClones);
        }
    }


}
