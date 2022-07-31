using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class BallScript : Agent
{
    public int numRayCasts = 8;
    public float castDistance = 1.5f;

    bool collidedThisEpisode = false;

    Rigidbody rBody;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public Transform Target;
    public override void OnEpisodeBegin()
    {
        // If the Agent fell, zero its momentum
        if (this.transform.localPosition.y < 0)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        // Move the ball to a new spot
        this.gameObject.GetComponent<Transform>().localPosition = new Vector3(Random.value * 30 - 15,
                                           0.5f,
                                           Random.value * 30 - 15);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        //sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(Vector3.Distance(this.transform.localPosition, Target.localPosition));

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);



        for (int i = 0; i < numRayCasts; i++)
        {
            sensor.AddObservation(Physics.Raycast(this.transform.position, new Vector3(Mathf.Cos(i * 2 * Mathf.PI / numRayCasts), 0f, Mathf.Sin(i * 2 * Mathf.PI / numRayCasts)), castDistance));
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        collidedThisEpisode = true;
    }

    public float forceMultiplier = 10;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        // Reached target
        if (distanceToTarget < 1.42f)
        {
            //SetReward(1.0f);

            if (collidedThisEpisode)
                SetReward(0.5f);
            else
                SetReward(1.0f);

            collidedThisEpisode = false;
            EndEpisode();
        }

        if (StepCount > 1000)
        {
            
            if (collidedThisEpisode)
                SetReward(1.0f / distanceToTarget * 1/2);
            else
                SetReward(1.0f / distanceToTarget);
            
            //SetReward(0f);
            collidedThisEpisode = false;
            EndEpisode();
        }

        // Fell off platform
        else if (this.transform.localPosition.y < 0)
        {
            SetReward(0f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

}
