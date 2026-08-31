using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public Vector2 Position { get; private set; }
    public HashSet<Block> Blocks { get; private set; }


    public Chunk(Vector2 position, HashSet<Block> blocks)
    {
        Position = position;
        Blocks = blocks;
    }
}
