using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Sirenix.OdinInspector;
using System;
using FMODUnity;

public class Weapon : MonoBehaviour
{
    /*
    [field: SerializeField, BoxGroup("Weapon")] public float EffectiveRange { get; protected set; } = 5f;
    [field: SerializeField, BoxGroup("Weapon")] public float Damage { get; protected set; } = 5f;
    [field: SerializeField, BoxGroup("Weapon")] public float Cooldown { get; protected set; } = 1f;
    [field: SerializeField, BoxGroup("Weapon")] public float Duration { get; protected set; } = 1f;

    // we use EventReference to hook up FMOD events in the inspector
    [field: SerializeField, BoxGroup("Audio")] public EventReference AttackSFX { get; protected set; }

    [field: SerializeField, BoxGroup("Animations")] public Animator Animator { get; protected set; }
    [field: SerializeField, BoxGroup("Animations")] public string AnimationTrigger { get; protected set; }
    [field: SerializeField, BoxGroup("Animations")] public float AttackSpeed { get; protected set; } = 1f;
    */
    [SerializeField] public float Damage { get; protected set; } = 5f;
    [SerializeField] public float Cooldown { get; protected set; } = 1f;
    [SerializeField] public float EffectiveRange { get; protected set; } = 5f;
    [SerializeField] public float Duration { get; protected set; } = 1f;

    // we use EventReference to hook up FMOD events in the inspector
    [SerializeField] public EventReference AttackSFX { get; protected set; }

    [SerializeField] public Animator Animator { get; protected set; }
    [SerializeField] public string AnimationTrigger { get; protected set; }
    [SerializeField] public float AttackSpeed { get; protected set; } = 1f;


    private float _lastAttackTime;
    protected Vector3 _aimPosition;
    protected int _team;
    protected GameObject _instigator;

    private void OnValidate()
    {
        if (Animator == null) Animator = GetComponentInParent<Animator>();
    }

    public bool TryAttack(Vector3 aimPosition, int team, GameObject instigator)
    {
        if (Time.time < _lastAttackTime + Cooldown) return false;
        _lastAttackTime = Time.time;

        _aimPosition = aimPosition;
        _team = team;
        _instigator = instigator;

        Animator.SetFloat("AttackSpeed", AttackSpeed);

        Attack(aimPosition, team, instigator);
        return true;
    }

    protected virtual void Attack(Vector3 aimPosition, int team, GameObject instigator)
    {
        // play animation if trigger exists
        if (!string.IsNullOrEmpty(AnimationTrigger)) Animator.SetTrigger(AnimationTrigger);
        // play one shot FMOD event, checking for null
        if (!AttackSFX.IsNull) RuntimeManager.PlayOneShot(AttackSFX, transform.position);
    }

    // optional override function for attacks based on animations
    public virtual void AttackAnimEvent(int attackIndex)
    {

    }
}