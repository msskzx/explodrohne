using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System;

public class MoveTo : MonoBehaviour
{
    // the target spot that the drone will move towards
    public GameObject targetPrefab;
    // map containing discretized view of the environment which has been explored so far
    private int[,] map;
    // radius of the view circle around the drone
    private int radius;

    void Start()
    {
        // the drone
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        GameObject targetInstance;
        radius = 3;

        map = new int[7, 7];

        // initial target, to start exploring
        targetInstance = Instantiate(targetPrefab);

        // TODO
        // make the initial position random
        targetInstance.transform.position = agent.transform.position + new Vector3(-2, 0, 3);
        agent.destination = targetInstance.transform.position;

    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "Target")
        {
            // destroy on reaching the object
            Destroy(collision.gameObject);

            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            GameObject targetInstance;

            // generate a position outside the view circle
            // this will be the position of the next target
            System.Random rnd = new System.Random();

            // TODO
            // make sure the next position is an unexplored spot
            float xt = rnd.Next(-1*radius, radius+1) + radius;
            float zt = rnd.Next(-1*radius, radius+1) + radius;
            
            // the new destination
            targetInstance = Instantiate(targetPrefab);
            targetInstance.transform.position = new Vector3(xt, 1, zt);
            agent.destination = targetInstance.transform.position;

        }
    }

    void Update()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        RaycastHit hit;
        int range = radius;
        
        // forward direction
        if(Physics.Raycast(agent.transform.position, agent.transform.forward, out hit, range))
        {
            // There's an object forward
            Debug.Log(hit.transform.name);

            // TODO
            // mark this cell as occupied

        }
        else
        {
            // TODO
            // mark this cell as free
        }

        // TODO
        // do the remaining 7 directions

        // TODO
        // mark the current cell as free

    }
}