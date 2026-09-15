using System.Collections.Generic;
using UnityEngine;

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
    [Header("Debug")]
    [SerializeField] private bool debug;

    // ----- START -----
    private void Start()
    {
        GenerateStartingWorld();
    }
    // ----- WORLD GENERATING -----
    public void GenerateStartingWorld()
    {
        chunks = new Chunk[startWorldSizeInChunks, startWorldSizeInChunks];
        // Create three-dimensional array for virtual blocks AND with pre-defined size
        allVirtualBlocks = new VirtualBlock[startWorldSizeInChunks * chunkDimensions, startWorldSizeInChunks * chunkDimensions, maxHeight];
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
            // Create relative neighbour positions
            Vector3Int[] neighbourPosition = GetNeighbourPositions3D(virtBlock.WorldPosition).ToArray();
            // Check each neighbour positions if it is an air
            foreach(Vector3Int pos in neighbourPosition)
            {
                if (pos.x < 0 || pos.x >= startWorldSizeInChunks * chunkDimensions ||
                    pos.y < 0 || pos.y >= startWorldSizeInChunks * chunkDimensions ||
                    pos.z < 0 || pos.z >= maxHeight)
                {
                    continue;
                }
                // If is neighbour to an air --> set the block visible (spawn it)
                if (allVirtualBlocks[pos.x, pos.y, pos.z].BlockType == Block.BlockType.Air)
                {
                    SpawnBlock(virtBlock);
                    break;
                }
            }
        }
    }
    private void InitializeVirtualBlocksInChunk(Chunk chunk)
    {
        int[,] localHeightMap = GenerateHeightmapForChunk(chunk);
        List<VirtualBlock> blocks = new(chunkDimensions * chunkDimensions * maxHeight);
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
                    Vector3Int chunkPos = new(x, y, height);
                    Vector3Int worldPos = new(x + (chunk.Position.x * chunkDimensions), y + (chunk.Position.y * chunkDimensions), height);
                    VirtualBlock newVirtBlock = new()
                    {
                        BlockType = type,
                        ChunkPosition = chunkPos,
                        WorldPosition = worldPos
                    };
                    blocks.Add(newVirtBlock);
                    allVirtualBlocks[x + (chunk.Position.x * chunkDimensions), y + (chunk.Position.y * chunkDimensions), height] = newVirtBlock;
                }
            }
        }
        // Assign generated blocks
        chunk.AssignVirtualBlocks(blocks);
    }
    private void SpawnBlock(VirtualBlock virtualBlock)
    {
        Vector3 position = new(virtualBlock.WorldPosition.x, virtualBlock.WorldPosition.z, virtualBlock.WorldPosition.y);
        GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, parentFolder ? parentFolder : transform);
        block.GetComponent<Block>().InitializeBlock(virtualBlock.WorldPosition, BlockSOFromBlockType(virtualBlock.BlockType));
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
}