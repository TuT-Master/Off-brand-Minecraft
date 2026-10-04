using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class WorldManager : MonoBehaviour
{
    [Header("Blocks")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private BlockSO grassBlock;
    [SerializeField] private BlockSO dirtBlock;
    [SerializeField] private BlockSO snowBlock;
    [SerializeField] private BlockSO stoneBlock;
    [Header("World Generation Settings")]
    [SerializeField] private int startWorldSizeInChunks = 7;
    [SerializeField] private int maxHeight = 128;
    [SerializeField] private int maxHeightDifferenceBetweenNeighbourChunks = 4;
    [SerializeField] private Transform parentFolder;
    [SerializeField] private int dirtBlockCountBeneathGrass = 3;
    [Header("Chunk Settings")]
    [SerializeField] private int chunkDimensions = 16;
    private Chunk[,] chunks;
    private VirtualBlock[,,] allVirtualBlocks;
    [Header("References")]
    [SerializeField] private Player player;
    [Header("Debug")]
    [SerializeField] private bool debug;
    // Block pooling
    private static readonly int defaultPoolSize = 1000;
    private readonly Queue<GameObject> blocksInPool = new();
    private GameObject[,,] visibleBlocks;

    // ----- START -----
    private void Start()
    {
        for (int i = 0; i < defaultPoolSize; i++)
        {
            GameObject block = Instantiate(blockPrefab, parentFolder ? parentFolder : transform);
            blocksInPool.Enqueue(block);
            block.SetActive(false);
        }
        GenerateStartingWorld(out Vector3Int centerPosition);
        centerPosition.y += 1;
        player.transform.position = centerPosition;
    }
    // ----- WORLD GENERATING -----
    public void GenerateStartingWorld(out Vector3Int centerPosition)
    {
        chunks = new Chunk[startWorldSizeInChunks, startWorldSizeInChunks];
        // Create three-dimensional array for virtual blocks AND with pre-defined size
        allVirtualBlocks = new VirtualBlock[startWorldSizeInChunks * chunkDimensions, maxHeight, startWorldSizeInChunks * chunkDimensions];
        visibleBlocks = new GameObject[startWorldSizeInChunks * chunkDimensions, maxHeight, startWorldSizeInChunks * chunkDimensions];
        // Create height map of chunks
        for (int y = 0; y < startWorldSizeInChunks; y++)
        {
            for (int x = 0; x < startWorldSizeInChunks; x++)
            {
                // Create new chunk at position
                Chunk chunk = new(new(x, y));
                // Create relative neighbour positions
                List<Vector2Int> neighbourPositions = GetNeighbourPositions2D(chunk.Position);
                int _newHeight;
                if (x == 0 && y == 0) // First chunk on random height
                {
                    _newHeight = (int)(maxHeight * Random.Range(0f, 1f));
                }
                else
                {
                    int _minHeight = 0;
                    int _maxHeight = maxHeight;
                    for (int i = neighbourPositions.Count - 1; i >= 0; i--)
                    {
                        // Assign only positions not outside of bounds
                        if (neighbourPositions[i].x >= 0 && neighbourPositions[i].x < startWorldSizeInChunks &&
                            neighbourPositions[i].y >= 0 && neighbourPositions[i].y < startWorldSizeInChunks &&
                            ChunkAtPosition(neighbourPositions[i]) != null)
                        {
                            Chunk neighbourChunk = ChunkAtPosition(neighbourPositions[i]);
                            int lowerHeightLimit = Mathf.Clamp(neighbourChunk.Height - maxHeightDifferenceBetweenNeighbourChunks, 0, maxHeight);
                            int upperHeightLimit = Mathf.Clamp(neighbourChunk.Height + maxHeightDifferenceBetweenNeighbourChunks, 0, maxHeight);
                            _minHeight = _minHeight < lowerHeightLimit ? lowerHeightLimit : _minHeight;
                            _maxHeight = _maxHeight > upperHeightLimit ? upperHeightLimit : _maxHeight;
                        }
                    }
                    // Randomize height
                    _newHeight = Random.Range(_minHeight, _maxHeight + 1);
                }
                chunk.SetHeight(_newHeight);
                // Add created chunk to array
                chunks[x, y] = chunk;
            }
        }
        // Create and assign virtual blocks and assign neighbour chunks
        foreach (Chunk chunk in chunks)
        {
            List<Vector2Int> neighbourPositions = GetNeighbourPositions2D(chunk.Position);
            for (int i = neighbourPositions.Count - 1; i >= 0; i--)
            {
                // Assign only positions not outside of bounds
                if (neighbourPositions[i].x >= 0 && neighbourPositions[i].x < startWorldSizeInChunks &&
                    neighbourPositions[i].y >= 0 && neighbourPositions[i].y < startWorldSizeInChunks &&
                    ChunkAtPosition(neighbourPositions[i]) != null)
                {
                    chunk.NeighbourChunks.Add(neighbourPositions[i] - chunk.Position, ChunkAtPosition(neighbourPositions[i]));
                }
            }
            InitializeVirtualBlocksInChunk(chunk);
        }
        // Render visible blocks
        foreach (VirtualBlock virtBlock in allVirtualBlocks)
        {
            // Skip air blocks
            if (virtBlock.BlockType == Block.BlockType.Air)
            {
                continue;
            }
            // Check each neighbour positions if it is an air
            List<Vector3Int> neighbourPositions = GetNeighbourPositions3D(virtBlock.WorldPosition);
            foreach (Vector3Int pos in neighbourPositions)
            {
                if (pos.x < 0 || pos.x >= startWorldSizeInChunks * chunkDimensions ||
                    pos.z < 0 || pos.z >= startWorldSizeInChunks * chunkDimensions ||
                    pos.y < 0 || pos.y >= maxHeight)
                {
                    continue;
                }
                // If is neighbour to an air --> set the block visible (spawn it)
                if (allVirtualBlocks[pos.x, pos.y, pos.z].BlockType == Block.BlockType.Air)
                {
                    DisplayBlock(virtBlock);
                    break;
                }
            }
        }
        // Assign centerPosition
        Vector2Int centerPos2D = new(startWorldSizeInChunks * chunkDimensions / 2, startWorldSizeInChunks * chunkDimensions / 2);
        centerPosition = new(centerPos2D.x, HeightAtWorldPosition(centerPos2D), centerPos2D.y);
    }
    public int HeightAtWorldPosition(Vector2Int position)
    {
        for (int height = 0; height < maxHeight; height++)
        {
            VirtualBlock virtualBlock = allVirtualBlocks[position.x, height, position.y];
            if (virtualBlock.BlockType == Block.BlockType.Grass || virtualBlock.BlockType == Block.BlockType.Snow)
            {
                return virtualBlock.WorldPosition.y;
            }
        }
        return -1;
    }
    private void InitializeVirtualBlocksInChunk(Chunk chunk)
    {
        int[,] localHeightMap = GenerateHeightmapForChunk(chunk);
        List<VirtualBlock> blocks = new(chunkDimensions * maxHeight * chunkDimensions);
        // Modify height map from neigbour chunks 'chunk.NeighourChunks'
        for (int y = 0; y < chunkDimensions; y++)
        {
            for (int x = 0; x < chunkDimensions; x++)
            {
                if (debug)
                {
                    localHeightMap[x, y] = chunk.Height;
                }
                for (int height = maxHeight - 1; height >= 0; height--)
                {
                    Block.BlockType type;
                    if (height > localHeightMap[x, y])
                    {
                        type = Block.BlockType.Air;
                    }
                    else if (height == localHeightMap[x, y])
                    {
                        type = localHeightMap[x, y] >= maxHeight * 0.75f ? Block.BlockType.Snow : Block.BlockType.Grass;
                    }
                    else if (height > localHeightMap[x, y] - dirtBlockCountBeneathGrass)
                    {
                        type = Block.BlockType.Dirt;
                    }
                    else
                    {
                        type = Block.BlockType.Stone;
                    }
                    Vector3Int chunkPos = new(x, height, y);
                    Vector3Int worldPos = new(x + (chunk.Position.x * chunkDimensions), height, y + (chunk.Position.y * chunkDimensions));
                    VirtualBlock newVirtBlock = new()
                    {
                        BlockType = type,
                        ChunkPosition = chunkPos,
                        WorldPosition = worldPos
                    };
                    blocks.Add(newVirtBlock);
                    allVirtualBlocks[x + (chunk.Position.x * chunkDimensions), height, y + (chunk.Position.y * chunkDimensions)] = newVirtBlock;
                }
            }
        }
        // Assign generated blocks to chunks
        chunk.AssignVirtualBlocks(blocks);
    }
    private BlockSO BlockSOFromBlockType(Block.BlockType blockType)
    {
        return blockType switch
        {
            Block.BlockType.Grass => grassBlock,
            Block.BlockType.Dirt => dirtBlock,
            Block.BlockType.Snow => snowBlock,
            Block.BlockType.Stone => stoneBlock,
            _ => dirtBlock,
        };
    }
    private Chunk ChunkAtPosition(Vector2Int position)
    {
        return chunks[position.x, position.y];
    }
    private List<Vector2Int> GetNeighbourPositions2D(Vector2Int position)
    {
        return new()
                {
                    position + Vector2Int.up,
                    position + Vector2Int.down,
                    position + Vector2Int.left,
                    position + Vector2Int.right
                };
    }
    private List<Vector3Int> GetNeighbourPositions3D(Vector3Int position)
    {
        return new()
            {
                position + Vector3Int.up,
                position + Vector3Int.down,
                position + Vector3Int.left,
                position + Vector3Int.right,
                position + Vector3Int.forward,
                position + Vector3Int.back
            };
    }
    private int[,] GenerateHeightmapForChunk(Chunk chunk)
    {
        int[,] heightmap = new int[chunkDimensions, chunkDimensions];
        int xHeightDelta, yHeightDelta;
        int leftNeighbourChunkHeight = chunk.NeighbourChunks.ContainsKey(Vector2Int.left) ? chunk.NeighbourChunks[Vector2Int.left].Height : -1;
        int rightNeighbourChunkHeight = chunk.NeighbourChunks.ContainsKey(Vector2Int.right) ? chunk.NeighbourChunks[Vector2Int.right].Height : -1;
        int upNeighbourChunkHeight = chunk.NeighbourChunks.ContainsKey(Vector2Int.up) ? chunk.NeighbourChunks[Vector2Int.up].Height : -1;
        int bottomNeighbourChunkHeight = chunk.NeighbourChunks.ContainsKey(Vector2Int.down) ? chunk.NeighbourChunks[Vector2Int.down].Height : -1;
        if (leftNeighbourChunkHeight != -1 && rightNeighbourChunkHeight != -1)
        {
            xHeightDelta = (rightNeighbourChunkHeight - leftNeighbourChunkHeight) / 2;
        }
        else if (leftNeighbourChunkHeight != -1)
        {
            xHeightDelta = leftNeighbourChunkHeight - chunk.Height;
        }
        else
        {
            xHeightDelta = rightNeighbourChunkHeight - chunk.Height;
        }
        if (upNeighbourChunkHeight != -1 && bottomNeighbourChunkHeight != -1)
        {
            yHeightDelta = (upNeighbourChunkHeight - bottomNeighbourChunkHeight) / 2;
        }
        else if (upNeighbourChunkHeight != -1)
        {
            yHeightDelta = upNeighbourChunkHeight - chunk.Height;
        }
        else
        {
            yHeightDelta = chunk.Height - bottomNeighbourChunkHeight;
        }
        int[] xSlope = new int[chunkDimensions];
        int[] ySlope = new int[chunkDimensions];
        for(int i = 0; i < chunkDimensions; i++)
        {
            xSlope[i] = Mathf.RoundToInt(xHeightDelta * (float)(i / (float)chunkDimensions));
            ySlope[i] = Mathf.RoundToInt(yHeightDelta * (float)(i / (float)chunkDimensions));
        }
        for (int y = 0; y < chunkDimensions; y++)
        {
            for (int x = 0; x < chunkDimensions; x++)
            {
                heightmap[x, y] = ((xSlope[x] + ySlope[y]) / 2) + chunk.Height;
            }
        }
        return heightmap;
    }
    // ----- BLOCK POOL -----
    private GameObject GetBlockFromPool()
    {
        GameObject block = blocksInPool.Count > 0 ? blocksInPool.Dequeue() : Instantiate(blockPrefab, parentFolder ? parentFolder : transform);
        block.SetActive(true);
        return block;
    }
    private void ReturnBlockToPool(GameObject block)
    {
        if (block != null)
        {
            blocksInPool.Enqueue(block);
            block.SetActive(false);
        }
    }
    // ----- BLOCKS -----
    private void DisplayBlock(VirtualBlock virtualBlock)
    {
        if (visibleBlocks[virtualBlock.WorldPosition.x, virtualBlock.WorldPosition.y, virtualBlock.WorldPosition.z] == null)
        {
            virtualBlock.IsVisible = true;
            GameObject block = GetBlockFromPool();
            block.transform.SetParent(transform);
            visibleBlocks[virtualBlock.WorldPosition.x, virtualBlock.WorldPosition.y, virtualBlock.WorldPosition.z] = block;
            block.transform.SetPositionAndRotation(virtualBlock.WorldPosition, Quaternion.identity);
            block.GetComponent<Block>().InitializeBlock(virtualBlock.WorldPosition, BlockSOFromBlockType(virtualBlock.BlockType));
        }
    }
    private void HideBlock(VirtualBlock virtualBlock)
    {
        virtualBlock.IsVisible = false;
        ReturnBlockToPool(visibleBlocks[virtualBlock.WorldPosition.x, virtualBlock.WorldPosition.y, virtualBlock.WorldPosition.z]);
        visibleBlocks[virtualBlock.WorldPosition.x, virtualBlock.WorldPosition.y, virtualBlock.WorldPosition.z] = null;
    }
    private void UpdateVisibilityOfBlocks(List<VirtualBlock> virtualBlock)
    {
        foreach (VirtualBlock block in virtualBlock)
        {
            if (block.BlockType == Block.BlockType.Air || block.IsVisible)
            {
                continue;
            }
            List<VirtualBlock> neighbourBlocks = GetNeighbourBlocks(block);
            bool isVisible = false;
            foreach (VirtualBlock neighbourBlock in neighbourBlocks)
            {
                if (neighbourBlock.BlockType == Block.BlockType.Air)
                {
                    isVisible = true;
                    DisplayBlock(block);
                    break;
                }
            }
            if (!isVisible)
            {
                HideBlock(block);
            }
        }
    }
    public void PlaceBlock(BlockSO blockSO, Vector3 position)
    {
        Vector3Int pos = new((int)position.x, (int)position.y, (int)position.z);
        VirtualBlock virtualBlock = allVirtualBlocks[pos.x, pos.y, pos.z];
        virtualBlock.BlockType = blockSO.BlockType;
        DisplayBlock(virtualBlock);
        List<VirtualBlock> virtualBlocks = GetNeighbourBlocks(virtualBlock);
        virtualBlocks.Add(virtualBlock);
        UpdateVisibilityOfBlocks(virtualBlocks);
    }
    public void DestroyBlock(Block block)
    {
        // First set this block as air and update visibility of neighbour blocks
        Vector3Int blockPos = block.WorldPosition;
        allVirtualBlocks[blockPos.x, blockPos.y, blockPos.z].BlockType = Block.BlockType.Air;
        visibleBlocks[blockPos.x, blockPos.y, blockPos.z] = null;
        ReturnBlockToPool(block.gameObject);
        UpdateVisibilityOfBlocks(GetNeighbourBlocks(allVirtualBlocks[blockPos.x, blockPos.y, blockPos.z]));
    }
    private List<VirtualBlock> GetNeighbourBlocks(VirtualBlock virtualBlock)
    {
        List<VirtualBlock> result = new();
        List<Vector3Int> neighbourPositions = GetNeighbourPositions3D(virtualBlock.WorldPosition);
        foreach (Vector3Int pos in neighbourPositions)
        {
            if (pos.x < 0 || pos.x >= startWorldSizeInChunks * chunkDimensions ||
                pos.z < 0 || pos.z >= startWorldSizeInChunks * chunkDimensions ||
                pos.y < 0 || pos.y >= maxHeight)
            {
                continue;
            }
            result.Add(allVirtualBlocks[pos.x, pos.y, pos.z]);
        }
        return result;
    }
}