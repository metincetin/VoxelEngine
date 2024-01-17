using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VoxelEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float _movementSpeed;
    [SerializeField]
    private float _jumpPower;


    [SerializeField]
    private VoxelWorld _world;
    private CharacterController _controller;

    private float _verticalVelocity;

    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private float _maxCamXRot = 80;

    [SerializeField]
    private float _lookSensitivity;
    private float _camXRot = 0;

    [SerializeField]
    private byte _placedBlockId;

    [SerializeField]
    private bool _isGrounded;


    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    private void Update()
    {
        _isGrounded = _controller.isGrounded;
        var movInp = new Vector3(
            Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical")
        );
        movInp = Vector3.ClampMagnitude(movInp, 1);

        if (_controller.isGrounded)
        {
            _verticalVelocity = Physics.gravity.y;
        }
        else
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && _controller.isGrounded)
        {
            _verticalVelocity = _jumpPower;
        }

        Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * _lookSensitivity;

        _camXRot -= mouseDelta.y;

        _camXRot = Mathf.Clamp(_camXRot, -_maxCamXRot, _maxCamXRot);

        _camera.transform.localEulerAngles = new Vector3(_camXRot, 0, 0);


        transform.Rotate(0, mouseDelta.x, 0);

        movInp = transform.TransformDirection(movInp);
        _controller.Move(movInp * _movementSpeed * Time.deltaTime + Vector3.up * _verticalVelocity * Time.deltaTime);


        // remove block
        if (Input.GetMouseButtonDown(0))
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (_world.Raycast(ray, 10.5f, out var hit))
            {
                hit.Chunk.SetBlock(hit.VoxelPosition.x, hit.VoxelPosition.y, hit.VoxelPosition.z, 0);
            }
        }

        // add block
        if (Input.GetMouseButtonDown(1))
        {
            var ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (_world.Raycast(ray, 5.5f, out var hit))
            {
                var p = hit.VoxelPosition + hit.VoxelNormal;
                Vector3 placementWP = hit.Chunk.WorldToLocal.inverse * new Vector4(p.x, p.y, p.z, 1);

                // bottom left origin to voxel center
                placementWP += Vector3.one * 0.5f;

                // we should only be able to place blocks that are outside our collider's bounding box
                if (!_controller.bounds.Intersects(new Bounds(placementWP, Vector3.one)))
                {
                    var placementPosition = new Vector3Int((int)p.x, (int)p.y, (int)p.z);
                    if (hit.Chunk.IsVoxelValid(placementPosition.x, placementPosition.y, placementPosition.z))
                        hit.Chunk.SetBlock(placementPosition.x, placementPosition.y, placementPosition.z, _placedBlockId);
                }
            }
        }

        // remove everything in 10m radius
        if (Input.GetKeyDown(KeyCode.P))
        {
            _world.ReplaceInRadius(transform.position, 10, 0);
        }
    }
}
