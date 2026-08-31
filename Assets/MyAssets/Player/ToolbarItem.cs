using UnityEngine;
using UnityEngine.UI;

public class ToolbarItem : MonoBehaviour
{
    public BlockSO Block;
    [SerializeField] private Image backgroundImage;

    public void SetHighlight(bool toggle)
    {
        backgroundImage.color = toggle ? Color.lightGreen : Color.lightSkyBlue;
    }
}
