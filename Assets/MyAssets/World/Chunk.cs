using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Chunk
{
    public Vector2Int Position { get; private set; }
    public int Height { get; private set; }
    public List<VirtualBlock> VirtualBlocks { get; private set; }
    public List<Block> RenderedBlocks { get; private set; }
    public Dictionary<Vector2Int, Chunk> NeighbourChunks = new();

    public Chunk(Vector2Int position)
    {
        Position = position;
    }
    public void SetHeight(int height)
    {
        Height = height;
    }
    public void AssignVirtualBlocks(List<VirtualBlock> blocks)
    {
        VirtualBlocks = blocks;
    }
    public void ToggleVisibility(bool visible)
    {
        RenderedBlocks.ForEach(block => block.gameObject.SetActive(visible));
    }
}


public struct VirtualBlock
{
    public Block.BlockType BlockType;
    public Vector3Int WorldPosition;
    public Vector3Int ChunkPosition;
}