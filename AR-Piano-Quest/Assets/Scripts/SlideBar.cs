using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlideBar : MonoBehaviour
{
    float _depth;
    float _beatLength;
    float _barThickness;

    [SerializeField] GameObject _exampleVisual;

    // Single Bar GameObject
    GameObject _barVisual;
    float _barStartTime;

    /// <summary>
    /// Initializes the SlideBar with the provided parameters.
    /// </summary>
    /// <param name="time">Current time to set the bar's start time.</param>
    /// <param name="width">Width of the SlideBar.</param>
    /// <param name="depth">Depth within which the bar moves.</param>
    /// <param name="beatLength">Length of each beat.</param>
    /// <param name="barThickness">Thickness of the bar.</param>
    /// <param name="barHover">Vertical offset for the bar.</param>
    public void Initialise(float time, float width, float depth, float beatLength, float barThickness, float barHover)
    {
        // Position and scale adjustments
        transform.localPosition += new Vector3(0, barHover, 0);
        transform.localScale = new Vector3(width, 1, 1);

        // Assigning parameters
        _depth = depth;
        _beatLength = beatLength;
        _barThickness = barThickness;

        // Reset the SlideBar
        DoReset(time);
    }

    /// <summary>
    /// Updates the SlideBar based on elapsed time.
    /// </summary>
    /// <param name="time">Current time to update the bar's position.</param>
    public void Elapse(float time)
    {
        if (_barVisual == null)
        {
            // Instantiate and show the bar
            _barVisual = Instantiate(_exampleVisual, transform);
            _barVisual.SetActive(true);
            _barStartTime = time;
        }

        // Calculate the new position based on elapsed time
        float elapsedTime = time - _barStartTime;
        float zPos = elapsedTime * _beatLength;

        if (zPos > _depth)
        {
            // The bar has moved beyond the depth, so reset it
            Destroy(_barVisual);
            _barVisual = null;
        }
        else
        {
            // Update the bar's position
            _barVisual.transform.localPosition = new Vector3(0, 0, zPos);
            _barVisual.transform.localScale = new Vector3(1, 1, _barThickness);
        }
    }

    /// <summary>
    /// Sets the SlideBar to metronome mode.
    /// </summary>
    public void SetMetronomeMode()
    {
        if (_barVisual != null)
        {
            _barVisual.SetActive(true); // Or adjust properties as needed
        }
    }

    /// <summary>
    /// Resets the SlideBar to its initial state.
    /// </summary>
    /// <param name="time">Current time to reset the bar's start time.</param>
    public void DoReset(float time)
    {
        if (_barVisual != null)
        {
            Destroy(_barVisual);
            _barVisual = null;
        }

        // Create and show a new bar
        _barVisual = Instantiate(_exampleVisual, transform);
        _barVisual.SetActive(true);
        _barStartTime = time;

        // Update the bar's position
        Elapse(time);
        SetMetronomeMode();
    }
}
