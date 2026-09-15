using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Block : MonoBehaviour
{
    public enum BlockType
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Snow,
    }
    public float Health { get; private set; } = 1f;
    public Vector3Int WorldPosition { get; private set; } = Vector3Int.zero;
    public Vector3Int ChunkPosition { get; private set; } = Vector3Int.zero;
    private MeshRenderer meshRenderer;

    // ----- INITIALIZATION -----
    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (WorldPosition != transform.position)
        {
            WorldPosition = new((int)transform.position.x, (int)transform.position.y, (int)transform.position.z);
        }
    }
    public void InitializeBlock(Vector3Int position, BlockSO blockSO)
    {
        WorldPosition = position;
        Health = blockSO.Health;
        meshRenderer = meshRenderer != null ? meshRenderer : GetComponent<MeshRenderer>();
        meshRenderer.material = blockSO.Material;
    }
    // ----- VISIBILITY -----
    public void PlaceBlock()
    {

    }
    public void DestroyBlock()
    {

    }
}
