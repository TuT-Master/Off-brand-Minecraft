using UnityEngine;
using UnityEngine.UI;

public class ToolbarItem : MonoBehaviour
{
    public BlockSO Block;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image blockImage;



    // ----- START -----
    private void Start()
    {
        blockImage.color = Block.Material.color;
    }


    public void SetHighlight(bool toggle)
    {
        backgroundImage.color = toggle ? Color.lightGreen : Color.lightSkyBlue;
    }
}
