using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System;

public class MoveTo : MonoBehaviour
{
    // the target spot that the drone will move towards
    public GameObject targetPrefab;
    // map containing discretized view of the environment which has been explored so far
    private char[,] map;
    // radius of the view circle around the drone
    private int radius;
    private int mapSize;
    private int cellSize;
    private Vector3[] directions = new Vector3[8];
    private char empty, occupied, unkown, unreachable;
    private String targetTag;
    private String obstacleTag;
    private ArrayList targetLocations;

    void Start()
    {
        // the drone
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        // radius of vision
        radius = 8;
        mapSize = 6;
        cellSize = 6;
        map = new char[mapSize, mapSize];
        targetLocations = new ArrayList();

        empty = 'O';
        occupied = 'X';
        unkown = '?';
        unreachable = 'U';
        targetTag = "Target";
        obstacleTag = "Obstacle";

        // raycast directions
        directions[0] = new Vector3(0, 0, 1);
        directions[1] = new Vector3(0, 0, -1);
        directions[2] = new Vector3(1, 0, 0);
        directions[3] = new Vector3(-1, 0, 0);
        directions[4] = new Vector3(1, 0, 1);
        directions[5] = new Vector3(1, 0, -1);
        directions[6] = new Vector3(-1, 0, 1);
        directions[7] = new Vector3(-1, 0, -1);

        // initialize the map with ?
        for (int i = 0; i < mapSize; i++)
            for (int j = 0; j < mapSize; j++)
                map[i, j] = unkown;

        // initial target, to start exploring
        MarkCell((int)agent.transform.position.x, (int)agent.transform.position.z, empty);
        Raycast8();
        PrintMap();
        NewTarget();
    }

    void Update()
    {
        Raycast8();
        RemoveCandidates();
    }

    Boolean doneExploring()
    {
        return targetLocations.Count == 0;
    }

    void OnCollisionEnter(Collision collision)
    {
        // if the drone hit a target and there are more places to explore
        if (collision.collider.tag == targetTag)
        {
            // destroy on reaching the object
            Destroy(collision.gameObject);

            // create a new target
            if (!doneExploring())
            {
                NewTarget();
            }

            PrintMap();
        }

    }

    // remove all cells that don't have adjacent unknown cells from the target locations
    void RemoveCandidates()
    {
        for (int i = 0; i < targetLocations.Count; i++)
        {
            Vector3 tmp = (Vector3)targetLocations[i];
            if (!HasAdjacentUnknown((int)tmp.x, (int)tmp.z))
            {
                targetLocations.RemoveAt(i);
            }
        }

    }

    // creating a new target destination
    void NewTarget()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        GameObject targetInstance;

        // get a new target destination
        targetInstance = Instantiate(targetPrefab);
        Vector3 nextPosition = (Vector3)targetLocations[0];
        // remove it from the array of possible locations
        targetLocations.RemoveAt(0);
        targetInstance.transform.position = new Vector3(nextPosition.x * cellSize, 1, nextPosition.z * cellSize);
        agent.destination = targetInstance.transform.position;
    }


    // print the map
    void PrintMap()
    {
        String s = "";
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
                s += " " + map[i, j];
            s += "\n";
        }
        Debug.Log(s);
    }

    void Raycast8()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        RaycastHit hit;
        int range = radius;

        // Raycast in the 8 directions
        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.Raycast(agent.transform.position, directions[i], out hit, range))
            {
                if (hit.collider.tag == obstacleTag)
                {
                    // There's an object, mark this cell as occupied
                    MarkCell((int)hit.transform.position.x, (int)hit.transform.position.z, occupied);
                }
            }
            else
            {
                // mark this cell as free
                int tmpx = (int)agent.transform.position.x + (int)(range * directions[i].x),
                    tmpz = (int)agent.transform.position.z + (int)(range * directions[i].z);
                // add this cell to the candidate target locations if it has adjacent unkown cells, and if it wasn't added
                // to the candidate list before.. i.e. if it has ?
                if (CheckBoundaries(tmpx / cellSize, tmpz / cellSize) && HasAdjacentUnknown(tmpx / cellSize, tmpz / cellSize) && map[tmpx / cellSize, tmpz / cellSize] == '?')
                {
                    targetLocations.Add(new Vector3(tmpx / cellSize, 1, tmpz / cellSize));
                }
                MarkCell(tmpx, tmpz, empty);
            }
        }
    }

    Boolean NotInCandidates(int x, int y, int z)
    {
        for (int i = 0; i < targetLocations.Count; i++)
        {
            Vector3 tmp = (Vector3)targetLocations[i];
            if (tmp.x == x && tmp.y == y && tmp.z == z)
                return false;
        }
        return true;
    }

    Boolean HasAdjacentUnknown(int i, int j)
    {
        return (CheckBoundaries(i, j + 1) && map[i, j + 1] == '?')
            || (CheckBoundaries(i, j - 1) && map[i, j - 1] == '?')
            || (CheckBoundaries(i + 1, j + 1) && map[i + 1, j + 1] == '?')
            || (CheckBoundaries(i + 1, j) && map[i + 1, j] == '?')
            || (CheckBoundaries(i + 1, j - 1) && map[i + 1, j - 1] == '?')
            || (CheckBoundaries(i - 1, j) && map[i - 1, j] == '?')
            || (CheckBoundaries(i - 1, j + 1) && map[i - 1, j + 1] == '?')
            || (CheckBoundaries(i - 1, j - 1) && map[i - 1, j - 1] == '?');
    }

    Boolean CheckBoundaries(int i, int j)
    {
        return i > -1 && i < mapSize && j > -1 && j < mapSize;
    }

    // mark this cell as free or occupied
    void MarkCell(int x, int z, char c)
    {
        x /= cellSize;
        z /= cellSize;
        if (x > -1 && x < mapSize && z > -1 && z < mapSize && map[x, z] != occupied)
            map[x, z] = c;
    }

}

