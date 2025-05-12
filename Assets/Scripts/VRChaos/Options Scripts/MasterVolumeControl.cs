using UnityEngine.UI;
using UnityEngine;

// Not currently in-use, but was made for extending the options menu capability for a public release. Currenlty I am just saving option settings directly to the ScriptableObject, but
// in the future, such classes as these could be utilized to make modifying and extensions easier. This one would be linked to the master volume of the game. 
public class MasterVolumeControl : MonoBehaviour
{
    public Slider volumeSlider;

    private void Start()
    {
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
    }
}
