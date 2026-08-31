using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float jumpHeight = 1.5f;
    private Rigidbody rb;


    [Header("Camera Settings")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.25f;


    [Header("Building Settings")]
    [SerializeField] private GameObject block_prefab;
    [SerializeField] private List<ToolbarItem> toolbarItems;
    [SerializeField] private float blockInteractionRange = 5f;
    private BlockSO selectedBlock = null;


    // Input actions
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lookAction;
    private InputAction toolbar1Action;
    private InputAction toolbar2Action;
    private InputAction toolbar3Action;
    private InputAction toolbar4Action;
    private InputAction lmbAction;
    private InputAction rmbAction;

    // Input variables
    private Vector2 moveVector = Vector2.zero;
    private Vector2 lookVector = Vector2.zero;
    private bool jump = false;





    // ----- START -----
    private void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        toolbar1Action = InputSystem.actions.FindAction("ToolbarSelection_1");
        toolbar2Action = InputSystem.actions.FindAction("ToolbarSelection_2");
        toolbar3Action = InputSystem.actions.FindAction("ToolbarSelection_3");
        toolbar4Action = InputSystem.actions.FindAction("ToolbarSelection_4");
        lmbAction = InputSystem.actions.FindAction("LMB");
        rmbAction = InputSystem.actions.FindAction("RMB");

        rb = GetComponent<Rigidbody>();

        // Disable cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SelectToolbarItem(0);
    }



    // ----- UPDATES -----
    private void Update()
    {
        moveVector = moveAction.ReadValue<Vector2>();
        lookVector = lookAction.ReadValue<Vector2>();
        jump = jumpAction.ReadValue<float>() > 0.1f;

        // Handle toolbar item selection
        if (toolbar1Action.ReadValue<float>() > 0.1f)
            SelectToolbarItem(0);
        else if (toolbar2Action.ReadValue<float>() > 0.1f)
            SelectToolbarItem(1);
        else if (toolbar3Action.ReadValue<float>() > 0.1f)
            SelectToolbarItem(2);
        else if (toolbar4Action.ReadValue<float>() > 0.1f)
            SelectToolbarItem(3);

        // Block interactions
        Block pointedBlock = PointedBlock(out Vector3 direction);
        if (pointedBlock != null)
        {
            if (lmbAction.WasPressedThisFrame()) // LMB was pressed
            {
                DestroyBlock(pointedBlock);
            }
            else if (rmbAction.WasPressedThisFrame()) // RMB was pressed
            {
                PlaceBlock(pointedBlock.Position + direction);
            }
        }
    }
    private void FixedUpdate()
    {
        Move();
    }
    private void LateUpdate()
    {
        HandleCamera();
    }



    // ----- MOVEMENT -----
    private void Move()
    {
        // Handle moving
        if (moveVector.magnitude > 0.1f)
        {
            Vector3 force = new Vector3(moveVector.x, 0f, moveVector.y) * movementSpeed;
            rb.AddRelativeForce(force);
        }

        // Handle jumping
        if(jump && IsGrounded())
        {
            rb.AddForce(rb.transform.up * jumpHeight, ForceMode.VelocityChange);
        }
    }
    private void HandleCamera()
    {
        if (lookVector.magnitude > 0.1f)
        {
            Vector2 look = lookVector * mouseSensitivity;

            // Yaw
            float yaw = transform.eulerAngles.y + look.x;
            transform.rotation = Quaternion.Euler(0, yaw, 0);

            // Pitch
            float pitch = cameraTransform.localEulerAngles.x;
            if (pitch > 180)
                pitch -= 360;

            pitch -= look.y;
            pitch = Mathf.Clamp(pitch, -85f, 85f);

            cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
    }
    private bool IsGrounded()
    {
        // Check if player is touching ground
        return Physics.Raycast(transform.position, -transform.up, 0.95f);
    }



    // ----- TOOLBAR -----
    private void SelectToolbarItem(int index)
    {
        if (toolbarItems == null || toolbarItems.Count <= index)
            return;

        selectedBlock = toolbarItems[index].Block;

        // Highlight selected item in toolbar
        toolbarItems.ForEach(item => item.SetHighlight(item == toolbarItems[index]));
    }



    // ----- WORLD INTERACTIONS -----
    private Block PointedBlock(out Vector3 direction)
    {
        Block block = null;
        direction = Vector3.zero;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, blockInteractionRange) && hit.collider.TryGetComponent(out block))
            direction = hit.normal;

        return block;
    }
    private void PlaceBlock(Vector3 position)
    {
        if (selectedBlock == null)
            return;

        GameObject newBlock = Instantiate(block_prefab, position, Quaternion.identity);
        newBlock.GetComponent<MeshRenderer>().material = selectedBlock.Material;
    }
    private void DestroyBlock(Block blockToDestroy)
    {
        if (!blockToDestroy.gameObject.IsDestroying())
            Destroy(blockToDestroy.gameObject);
    }
}
