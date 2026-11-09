using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    void Start()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(TriggerSound);
    }

    void TriggerSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClick();
        }
    }
}