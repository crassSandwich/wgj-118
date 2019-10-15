﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health2D))]
public class Orbiter : MonoBehaviour, IDamager
{
    [System.Serializable]
    public class OrbitalMechanics
    {
        public float CloseForce, FarDistance, FarForce;

        public float GetForce (float distance)
        {
            return Mathf.Lerp(CloseForce, FarForce, distance / FarDistance);
        }
    }

    [System.Serializable]
    public class DamageMechanics
    {
        public float MaxDamage, MaxSpeed, MinDamage, MinSpeed;

        public float GetDamage (float speed)
        {
            float lerpAmt = (speed - MinSpeed) / (MaxSpeed - MinSpeed);
            return Mathf.Lerp(MinDamage, MaxDamage, lerpAmt);
        }
    }

    public DamageMechanics DamageForOthers, DamageForSelf;
    public OrbitalMechanics OrbitalStats;
    public Breakage BreakageStats;
    public Vector2 BurstSpeedRange;
    public float TrueMaxSpeed; // should be experimentally derived based on orbit alone
    public bool TestForTrueMax;

    Rigidbody2D _rb;
    public Rigidbody2D Rigidbody => _rb ?? (_rb = GetComponent<Rigidbody2D>());

    Health2D health;

    void Awake ()
    {
        health = GetComponent<Health2D>();

        health.Died.AddListener(() => {
            BreakageStats.Explode(transform.position);
            Destroy(gameObject);
        });
    }

    void FixedUpdate ()
    {
        Vector2 diffVector = Ship.Instance.OrbitalCenter.position - transform.position;
        Vector2 orbitalForce = OrbitalStats.GetForce(diffVector.magnitude) * diffVector.normalized * Time.deltaTime;
        Rigidbody.AddForce(orbitalForce, ForceMode2D.Force);

        if (Rigidbody.velocity.magnitude > TrueMaxSpeed)
        {
            if (TestForTrueMax)
            {
                TrueMaxSpeed = Rigidbody.velocity.magnitude;
                Debug.Log(TrueMaxSpeed);
            }
            else
            {
                Rigidbody.velocity = Rigidbody.velocity.normalized * TrueMaxSpeed;
            }
        }

        transform.up = Vector2.MoveTowards(transform.up, Rigidbody.velocity, Time.deltaTime);
    }

    void OnCollisionEnter2D (Collision2D other)
    {
        var collisionSpeed = other.relativeVelocity.magnitude;
        var otherHealth = other.gameObject.GetComponent<Health2D>();

        if (otherHealth != null)
        {
            otherHealth.CurrentValue -= DamageForOthers.GetDamage(collisionSpeed);
        }

        if (other.gameObject.GetComponent<IDamager>() == null)
        {
            health.CurrentValue -= DamageForSelf.GetDamage(collisionSpeed);
        }
    }
}
