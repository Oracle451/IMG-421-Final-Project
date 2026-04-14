using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements Craig Reynolds' Boids algorithm for enemy fleet movement.
/// Attach to a fleet parent GameObject; child ships subscribe to this flock.
/// </summary>
public class BoidsFlock : MonoBehaviour
{
    [Header("Boid Weights")]
    public float SeparationWeight = 2.0f;
    public float AlignmentWeight  = 1.0f;
    public float CohesionWeight   = 1.0f;

    [Header("Distances")]
    public float SeparationRadius = 4f;
    public float NeighborRadius   = 12f;

    [Header("Movement")]
    public float FlockSpeed = 5f;

    [Header("Leader")]
    public Transform LeaderTarget;  // If set, flock chases the leader

    // Registered boid agents
    private readonly List<BoidAgent> _agents = new();

    public void RegisterAgent(BoidAgent agent)   => _agents.Add(agent);
    public void UnregisterAgent(BoidAgent agent) => _agents.Remove(agent);

    void Update()
    {
        foreach (BoidAgent agent in _agents)
        {
            if (agent == null) continue;
            Vector3 steering = ComputeSteering(agent);
            agent.ApplySteering(steering, FlockSpeed);
        }
    }

    Vector3 ComputeSteering(BoidAgent agent)
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment  = Vector3.zero;
        Vector3 cohesion   = Vector3.zero;
        int neighborCount  = 0;

        foreach (BoidAgent other in _agents)
        {
            if (other == agent || other == null) continue;
            Vector3 diff = agent.transform.position - other.transform.position;
            float dist   = diff.magnitude;

            if (dist < SeparationRadius && dist > 0.01f)
                separation += diff.normalized / dist;

            if (dist < NeighborRadius)
            {
                alignment  += other.Velocity;
                cohesion   += other.transform.position;
                neighborCount++;
            }
        }

        Vector3 result = Vector3.zero;
        result += separation * SeparationWeight;

        if (neighborCount > 0)
        {
            alignment  = (alignment / neighborCount).normalized;
            cohesion   = (cohesion / neighborCount) - agent.transform.position;
            result    += alignment * AlignmentWeight;
            result    += cohesion.normalized * CohesionWeight;
        }

        // Seek leader
        if (LeaderTarget != null)
        {
            Vector3 toLeader = (LeaderTarget.position - agent.transform.position).normalized;
            result += toLeader * 1.5f;
        }

        return result.normalized;
    }
}
