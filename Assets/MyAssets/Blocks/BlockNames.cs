using System.Collections.Generic;
using static Block;

public static class BlockNames
{
    public static readonly Dictionary<BlockType, string> typeToName = new()
    {
        { BlockType.Grass,  "Grass" },
        { BlockType.Dirt,   "Dirt" },
        { BlockType.Stone,  "Stone" },
        { BlockType.Snow,   "Snow" },
    };
}
