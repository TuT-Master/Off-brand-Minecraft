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

    // Public fields
    public float Health { get; private set; } = 1f;
    public Vector3 Position { get; private set; } = Vector3.zero;


    // References
    private MeshRenderer meshRenderer;



    // ----- INITIALIZATION -----
    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }
    public void InitializeBlock(Vector3 position, float health, Material material)
    {
        Position = position;
        Health = health;
        meshRenderer.material = material;
    }



    // ----- RENDERING -----
    public bool ShouldRender()
    {
        // Check all six sides if there are any neighbor block of type Air -> render

        return true;
    }
}
