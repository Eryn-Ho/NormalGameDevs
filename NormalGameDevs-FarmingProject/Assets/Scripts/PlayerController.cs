using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerController : Controller
{
    [Header("Aiming")]
    // layermasks let us flag which layers to use with Raycasts
    // 1 << 0 is a "bit shift" operation, that flips the 1st bit, setting the LayerMask to 'default'
    [SerializeField] private LayerMask _groundMask = 1 << 0;

    private Vector2 _moveInput2D;
    private Vector3 _aimPosition;

    // called from PlayerInput component
    public void OnMove(InputValue value)
    {
        // retrieve Vector2 (up/down - left/right)
        _moveInput2D = value.Get<Vector2>();
    }

    // also called from PlayerInput, we don't care about input values for jumping
    public void OnJump()
    {
        _movement.Jump();
    }

    public void OnShoot()
    {
        // assume weapon 0 is gun
        //Weapon weapon = _weapons[0];
        //weapon.TryAttack(_aimPosition, _targetable.Team, gameObject);
    }

    public void OnMelee()
    {
        // assume weapon 1 is melee
        //Weapon weapon = _weapons[1];
        //weapon.TryAttack(_aimPosition, _targetable.Team, gameObject);
    }

    private void Update()
    {
        // convert player input from WASD/stick (Vector2) to world space Vector3 move direction
        Vector3 moveInput3D = new Vector3(_moveInput2D.x, 0f, _moveInput2D.y);
        // move character
        _movement.SetMoveInput(moveInput3D);

        // use Raycast to find world position of mouse
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, _groundMask))
        {
            Vector3 mouseWorldPosition = hit.point;
            // create a virtual plane hovering 1m above our mouse point
            Plane aimPlane = new Plane(Vector3.up, mouseWorldPosition + Vector3.up);

            // raycast against virtual plane, returning distance to plane along our mouse ray
            if (aimPlane.Raycast(mouseRay, out float planeDistance))
            {
                // move mouse world position to intersection point on plane
                mouseWorldPosition = mouseRay.GetPoint(planeDistance);
            }

            Debug.DrawRay(mouseWorldPosition, hit.normal, Color.yellow);

            // look in aim position
            _movement.SetLookPosition(mouseWorldPosition);

            _aimPosition = mouseWorldPosition;
        }
    }
}
