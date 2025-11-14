using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI lifeText;

    private void Awake()
    {
        Instance = this;
        lifeText.text = "";
    }

    public void SetHealth(int value)
    {
        lifeText.text = "Vida: " + value;
    }
}
