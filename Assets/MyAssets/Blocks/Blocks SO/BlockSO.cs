using UnityEngine;

[CreateAssetMenu(fileName = "New Block", menuName = "Block")]
public class BlockSO : ScriptableObject
{
    public string Name => blockName;
    public float Health => blockHealth;
    public Material Material => blockMaterial;
    public Block.BlockType BlockType => blockType;
    [SerializeField] private string blockName = "Unnamed block";
    [SerializeField] private float blockHealth = 1f;
    [SerializeField] private Material blockMaterial;
    [SerializeField] private Block.BlockType blockType;
}
