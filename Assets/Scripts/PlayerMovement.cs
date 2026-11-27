
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public float Speed = 5f;
    public float JumpForce = 5f;
    public float GravityValue = -9.81f;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _jumpPressed;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null &&
            keyboard.spaceKey.wasPressedThisFrame)
        {
            _jumpPressed = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        float x = 0f;
        float z = 0f;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
        }

        Vector3 move = new Vector3(x, 0, z).normalized;

        if (_controller.isGrounded)
        {
            if (_velocity.y < 0f)
                _velocity.y = -1f;

            if (_jumpPressed)
                _velocity.y = JumpForce;
        }

        _velocity.y += GravityValue * Runner.DeltaTime;

        Vector3 motion = move * Speed;
        motion.y = _velocity.y;

        _controller.Move(motion * Runner.DeltaTime);

        if (move.sqrMagnitude > 0.001f)
        {
            transform.forward = move;
        }

        _jumpPressed = false;
    }
}
