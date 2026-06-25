using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// This allows me to right-click the Timeline to add this specific marker 
[CustomStyle("SubtitleMarker")]
public class SubtitleMarker : Marker, INotification // THE DATA
{
    [Header("Subtitle Data")]
    [Tooltip("Type the Localization Key (or raw English text for testing")]
    public string subtitleKey;
    
    [Tooltip("How many seconds should this stay on screen?")]
    public float duration = 2f;

    // Required by the INotification interface, we can leave it blank
    public PropertyName id { get; }
}
