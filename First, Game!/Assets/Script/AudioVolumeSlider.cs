using UnityEngine;
using UnityEngine.UI;

public class AudioVolumeSlider : MonoBehaviour
{
    public AudioPooler audioPooler; // Reference to your AudioPooler
    public string poolName;         // Name of the sound to control
    public Slider slider;

    private void Start()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderChanged);

            // Initialize slider value from prefab volume
            AudioPoolItem item = audioPooler.audioItems.Find(x => x.name == poolName);
            if (item != null) slider.value = item.volume;
        }
    }

    private void OnSliderChanged(float value)
    {
        AudioPoolItem item = audioPooler.audioItems.Find(x => x.name == poolName);
        if (item != null) item.volume = value;
    }
}
