using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;


public class MoveToGoalAgent : Agent
{

    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform goalTransform;
    [SerializeField] private MeshRenderer platformBorderRenderer;
    [SerializeField] private Material passMaterial;
    [SerializeField] private Material failMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float timeoutPenalty = -0.5f;
    [SerializeField] private float wallTouchPenalty = -1f;
    [SerializeField] private float ballTouchReward = +1f;



    private void Start()
    {
        platformBorderRenderer.material = defaultMaterial;
    }

    public override void OnEpisodeBegin()
    {
        // Reset the agent's position and the target's position
        //targetTransform.localPosition = new Vector3(0, 1, 0);
        transform.localPosition = new Vector3(Random.Range(-4f, 4f), 1, Random.Range(-4f, 4f));
        goalTransform.localPosition = new Vector3(Random.Range(-4f, 4f), 1, Random.Range(-4f, 4f));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add the agent's position as an observation
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(targetTransform.localPosition);

        sensor.AddObservation(goalTransform.localPosition);
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        // If using Agent MaxStep (per-episode), detect timeout and fail
        // StepCount is the current step count for this episode.
        if (MaxStep > 0 && StepCount >= MaxStep - 1)
        {
            platformBorderRenderer.material = failMaterial;
            AddReward(timeoutPenalty);   // penalize timeout
            EndEpisode();
            return;
        }


        // Get the continuous action values for movement
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        
        // Update Vector position based on the action values
        transform.localPosition+= new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("target"))
        {
            SetReward(ballTouchReward);
            platformBorderRenderer.material = passMaterial;
            EndEpisode();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("target"))
        {
        SetReward(ballTouchReward);
        platformBorderRenderer.material = passMaterial;
        EndEpisode();
        }

        if (other.CompareTag("Wall"))
        {
        SetReward(wallTouchPenalty);
        platformBorderRenderer.material = failMaterial;
        EndEpisode();
        }
    }

    
}
