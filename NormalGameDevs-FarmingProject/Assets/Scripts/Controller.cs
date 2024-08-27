using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(CustomCharacterMovement3D))]
//[RequireComponent(typeof(Vision))]
//[RequireComponent(typeof(Targetable))]
public class Controller : MonoBehaviour
{
    //[Header("Components")]
    [SerializeField] protected CustomCharacterMovement3D _movement;
    //[SerializeField] protected Vision _vision;
    //[SerializeField] protected Targetable _targetable;

    //[Header("Weapons")]
    //[SerializeField] protected Weapon[] _weapons = new Weapon[0];

    // OnValidate is run only in the editor, when inspector values change
    protected virtual void OnValidate()
    {
        _movement = GetComponent<CustomCharacterMovement3D>();
        //_vision = GetComponent<Vision>();
        //_targetable = GetComponent<Targetable>();
        //_weapons = GetComponentsInChildren<Weapon>();
    }
}
