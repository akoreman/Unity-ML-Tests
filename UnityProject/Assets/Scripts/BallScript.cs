using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BallScript : Agent
{
    public int numRayCasts = 8;
    public float castDistance = 2.5f;
    public float penaltyPerCollision = .05f;

    bool collidedThisEpisode = false;
    float collidedPenalty = 0f;

    float[] rayVector;

    float inf = 0f;

    Rigidbody rBody;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public Transform Target;
    public override void OnEpisodeBegin()
    {
        // If the Agent fell, zero its momentum
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        
        // Move the ball to a new spot
        this.transform.localPosition = new Vector3(Random.value * 30 - 15,
                                           0.5f,
                                           Random.value * 30 - 15);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // target and Agent positions
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, Target.localPosition));

        // agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

        rayVector = new float[8];

        RaycastHit raycastHit;

        for (int i = 0; i < numRayCasts; i++)
            rayVector[i] = Physics.Raycast(this.transform.position, new Vector3(Mathf.Cos(i / numRayCasts * 2 * Mathf.PI), 0f, Mathf.Sin(i / numRayCasts * 2 * Mathf.PI)), out raycastHit, castDistance) ? raycastHit.distance : inf;

        sensor.AddObservation(rayVector);
    }

    void OnCollisionEnter(Collision collision)
    {
        collidedPenalty += penaltyPerCollision;
    }

    public float forceMultiplier = 10;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        //rBody.velocity = controlSignal * forceMultiplier;

        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        // reward when target reached
        if (distanceToTarget < 1.42f)
        {
            EndWithReward(Mathf.Max(1.0f - collidedPenalty, 0.5f));
        }

        // when too many steps
        if (StepCount > 1000)
        {
            EndWithReward(Mathf.Max(1.42f / distanceToTarget - collidedPenalty, 0f));
        }

        // when fall off platform
        if (this.transform.localPosition.y < 0)
        {
            EndWithReward(0f);
        }
    }

    void EndWithReward(float reward)
    {
        SetReward(reward);

        collidedPenalty = 0f;
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

}
