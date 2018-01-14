﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshObstacle))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(NpcData))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(HostilityChecker))]
[RequireComponent(typeof(SpriteLookRotator))]
[RequireComponent(typeof(AudioSource))]

public abstract class BaseNpc : MonoBehaviour, ITriggerListener
{
    public enum NpcState { Walking, Idle, Attacking, ReceivingDamage, Dying, Dead, None }
    public enum HostilityType { Friendly, Hostile };

    //-------------------------------------------------------------------------
    // Variables
    //-------------------------------------------------------------------------

    // Public - Editor accessible
    public float m_StoppingDistance = 0.5f;

    public bool m_IsRanged = false;
    public bool m_HasAltRangedAttack = false;
    public float m_AltRangedAttackChance;
    public float m_TimeSinceLastAltAttack = 0.0f;
    public bool m_DoWander = false;
    public float m_MinWanderIdleTime = 1.0f;
    public float m_MaxWanderIdleTime = 2.0f;
    public float m_WanderRadius = 15.0f;

    public bool m_DrawWaypoint = true;

    public float m_UpdateIntervalMs = 50.0f;

    public AudioClip m_AttackSound;
    public AudioClip m_DeathSound;
    public AudioClip m_AwareSound;
    public AudioClip m_WinceSound;

    /*public float m_AgroRange; // Agro on Y axis is not taken into account
    public float m_MeleeRange;*/

    public Vector3 m_SpawnPosition;

    // Private
    protected GameObject m_Player;

    protected Animator m_Animator;
    protected NavMeshAgent m_NavMeshAgent;
    protected NavMeshObstacle m_NavMeshObstacle;
    protected NpcData m_Stats;
    protected HostilityChecker m_HostilityResolver;
    protected SpriteLookRotator m_SpriteLookRotator;
    protected AudioSource m_AudioSource;
    
    protected Vector3 m_CurrentDestination;

    protected float m_RemainingWanderIdleTime = 2.0f;

    protected GameObject m_CurrentWaypoint;

    protected NpcState m_State = NpcState.Idle;

    protected List<GameObject> m_EnemiesInMeleeRange = new List<GameObject>();
    // Agro range is also Ranged range for archers/casters
    protected List<GameObject> m_EnemiesInAgroRange = new List<GameObject>();

    protected GameObject m_Target;

    // State members
    protected string m_Faction;
    protected int m_FleeHealthPercantage;
    protected bool m_IsFleeing = false;

    protected bool m_IsPlayerInMeleeRange = false;

    //-------------------------------------------------------------------------
    // Unity Overrides
    //-------------------------------------------------------------------------

    public void Awake()
    {
        m_SpawnPosition = transform.position;
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_NavMeshAgent.enabled = false; // Supress warnigns
        m_NavMeshObstacle = GetComponent<NavMeshObstacle>();
        m_NavMeshObstacle.enabled = false; // Supress warnigns
        m_Animator = GetComponent<Animator>();
        m_HostilityResolver = GetComponent<HostilityChecker>();
        m_SpriteLookRotator = GetComponent<SpriteLookRotator>();
        m_AudioSource = GetComponent<AudioSource>();
    }

    // Use this for initialization
    public void OnStart ()
    {
        m_Player = GameObject.FindWithTag("Player");
        if (m_Player == null)
        {
            Debug.LogError("Could not find \"Player\" in scene !");
        }

        // Create debug waypoint
        m_CurrentWaypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        m_CurrentWaypoint.gameObject.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        m_CurrentWaypoint.GetComponent<Renderer>().material.color = Color.red;
        m_CurrentWaypoint.name = this.gameObject.name + " Waypoint";
        m_CurrentWaypoint.GetComponent<SphereCollider>().enabled = false;

        m_CurrentWaypoint.SetActive(m_DrawWaypoint);

        SetNavMeshAgentEnabled(true);

        //m_NavMeshAgent
    }

    /**** If NPC is a Guard ****/
    // 1) If it is attacking, do nothing (Waiting for AttackEnded frame event)
    // 2) If it is moving, do nothing (May be interrupted if enemy enters its melee range)
    // 3) If it has hostile unit(s) in range, move to its closest one
    // 4) Else If this unit can Patrol, move to its point within patrol area
    // 5) Else do nothing (Idle)
    // ----- [Event] OnAttackEnded - after attack ends, it will check if it is within melee range of any hostile unit,
    //                           if it is, then it will attack it again, if it is not, it will choose some strafe
    //                           location - e.g. Shoot - Move - Shoot - Move, etc.
    // ------ [Event] If enemy enters its attack range, it will attack immediately
    // ------ [Event] OnDamaged - If it was attacked by a unit which was previously friendly, change this unit to Hostile
    //                            and query all nearby Guards / Villagers of the same affiliation to be hostile towards
    //                            that unit too

    

    //-------------------------------------------------------------------------
    // Methods
    //-------------------------------------------------------------------------

    public bool IsWalking()
    {
        if (!m_NavMeshAgent.enabled)
        {
            return false;
        }

        if (!m_NavMeshAgent.pathPending)
        {
            if (m_NavMeshAgent.remainingDistance <= m_NavMeshAgent.stoppingDistance)
            {
                m_NavMeshAgent.SetDestination(transform.position);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                return false;
            }
        }

        return true;
    }

    public void WanderWithinSpawnArea(float wanderRadius)
    {
        SetNavMeshAgentEnabled(true);

        m_CurrentDestination = m_SpawnPosition + new Vector3(
            UnityEngine.Random.Range((int) - wanderRadius * 0.5f - 2, (int)wanderRadius * 0.5f + 2), 
            0,
            UnityEngine.Random.Range((int) - wanderRadius * 0.5f - 2, (int)wanderRadius * 0.5f + 2));
        m_NavMeshAgent.ResetPath();

        m_NavMeshAgent.SetDestination(m_CurrentDestination);

        m_CurrentWaypoint.transform.position = m_CurrentDestination;

        Vector3 direction = (m_CurrentDestination - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
        //transform.rotation = Quaternion.Slerp(transform.rotation, qDir, Time.deltaTime * rotSpeed);
    }

    public void WanderAwayFromEnemy(GameObject enemy)
    {
        SetNavMeshAgentEnabled(true);

        Vector3 heading = enemy.transform.position - transform.position;
        heading.Normalize();

        float randRotMod = UnityEngine.Random.Range(-20.0f, 20.0f);
        //randRotMod = 90.0f;
        heading = Quaternion.AngleAxis(randRotMod, Vector3.up) * heading;

        m_CurrentDestination = transform.position - heading * 6.0f;
        m_NavMeshAgent.ResetPath();
        m_NavMeshAgent.SetDestination(m_CurrentDestination);

        m_CurrentWaypoint.transform.position = m_CurrentDestination;

        Vector3 direction = (m_CurrentDestination - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    public void ChaseTarget(GameObject target)
    {
        SetNavMeshAgentEnabled(true);

        m_Target = target;

        m_CurrentDestination = target.transform.position;
        m_NavMeshAgent.ResetPath();

        m_NavMeshAgent.SetDestination(m_CurrentDestination);

        m_CurrentWaypoint.transform.position = m_CurrentDestination;

        Vector3 direction = (m_CurrentDestination - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        m_Animator.SetInteger("State", (int)NpcState.Walking);
    }

    public void StopMoving()
    {
        SetNavMeshAgentEnabled(true);
        m_NavMeshAgent.ResetPath();
    }

    public void SetNavMeshAgentEnabled(bool enabled)
    {
        // This is to supress warnings
        if (enabled)
        {
            m_NavMeshObstacle.enabled = false;
            m_NavMeshAgent.enabled = true;
        }
        else
        {
            m_NavMeshAgent.enabled = false;
            m_NavMeshObstacle.enabled = true;
        }
    }

    public void TurnToObject(GameObject go)
    {
        if (go == null)
        {
            Debug.LogError("Cannot turn to null object !");
            return;
        }

        if (go.CompareTag("Player"))
        {
            transform.LookAt(transform.position + go.transform.rotation * Vector3.back, go.transform.rotation * Vector3.up);
            m_SpriteLookRotator.OnLookDirectionChanged(SpriteLookRotator.LookDirection.Front);
        }
        else
        {
            transform.LookAt(go.transform);
            m_SpriteLookRotator.AlignRotation();
        }
    }

    public void OnObjectEnteredMyTrigger(GameObject other, TriggerType triggerType)
    {
        switch (triggerType)
        {
            case TriggerType.MeleeRange:
                OnObjectEnteredMeleeRange(other);
                break;

            case TriggerType.AgroRange:
                OnObjectEnteredAgroRange(other);
                break;

            default:
                Debug.LogError("Unhandled Trigger Type: " + triggerType);
                break;
        }
    }

    public void OnObjectLeftMyTrigger(GameObject other, TriggerType triggerType)
    {
        switch (triggerType)
        {
            case TriggerType.MeleeRange:
                OnObjectLeftMeleeRange(other);
                break;

            case TriggerType.AgroRange:
                OnObjectLeftAgroRange(other);
                break;

            default:
                Debug.LogError("Unhandled Trigger Type: " + triggerType);
                break;
        }
    }

    abstract public void OnObjectEnteredMeleeRange(GameObject other);
    abstract public void OnObjectEnteredAgroRange(GameObject other);

    abstract public void OnObjectLeftMeleeRange(GameObject other);
    abstract public void OnObjectLeftAgroRange(GameObject other);
}