using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using System;

public class MoveTo : MonoBehaviour
{
    // the target spot that the drone will move towards
    public GameObject targetPrefab;
    // map containing discretized grid view of the environment which has been explored so far
    private char[,] map;
    private ArrayList[] graph;

    private Vector3[] directions;
    private String targetTag;
    private String obstacleTag;
    // set of target locations to explore, 
    // contains vectors: indices on the grid
    private ArrayList targetLocations;
    // path to current unexplored spot
    private Stack routeToTarget;

    // radius of the view circle around the drone
    private const int range = 11;
    private const int cellSize = 8;
    private const char empty = 'O', occupied = 'X', unknown = '?', unreachable = '#';
    private int mapSize = 10;
    private int V;
    
    void Start()
    {
        // the drone
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        map = new char[mapSize, mapSize];
        targetLocations = new ArrayList();
        routeToTarget = new Stack();
        V = mapSize * mapSize;
        graph = new ArrayList[V];
        targetTag = "Target";
        obstacleTag = "Obstacle";

        // raycast directions
        directions = new Vector3[8];
        directions[0] = new Vector3(0, 0, 1);
        directions[1] = new Vector3(1, 0, 1);
        directions[2] = new Vector3(1, 0, 0);
        directions[3] = new Vector3(1, 0, -1);
        directions[4] = new Vector3(0, 0, -1);
        directions[5] = new Vector3(-1, 0, -1);
        directions[6] = new Vector3(-1, 0, 0);
        directions[7] = new Vector3(-1, 0, 1);

        // initialize the map
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                map[i, j] = unknown;
                graph[IndicesToVertex(new Vector3(i, 1, j))] = new ArrayList();
            }
        }

        // print vertices numbering
        String s = "";
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
                s += " " + IndicesToVertex(new Vector3(i, 1, j));
            s += "\n";
        }
        Debug.Log(s);


        // initial target, to start exploring
        MarkCell(PositionToIndices(agent.transform.position), empty);
        
        Raycast8();
        PrintTargetLocations();
        PrintMap();
        if (DoneExploring())
        {
            MarkUnreachable();
            PrintMap();
        }
        else
        {
            NewTarget();
        }
    }



    void Update()
    {
        Raycast8();
        RemoveCandidates();
        if(DoneExploring())
        {
            PrintMap();
        }
    }



    void PrintGraph()
    {
        for (int i = 0; i < graph.Length; i++)
        {
            String s = "";
            if (graph[i].Count > 0)
            {
                s = "Node" + i + ": ";
                for (int j = 0; j < graph[i].Count; j++)
                {
                    s += ", " + graph[i][j];

                }
            }
            Debug.Log(s);
        }
    }



    Boolean DoneExploring()
    {
        return targetLocations.Count == 0;
    }



    void OnCollisionEnter(Collision collision)
    {
        PrintTargetLocations();
        // if the drone hit a target and there are more places to explore
        if (collision.collider.tag == targetTag)
        {
            // destroy on reaching the object
            Destroy(collision.gameObject);

            // create a new target
            if (!DoneExploring())
            {
                NewTarget();
            }
            else
            {
                // no more targets mark the remaining cells as unreach-able
                MarkUnreachable();
            }

            PrintMap();
        }
    }



    void PrintTargetLocations()
    {
        String x = "Target Locations: ";
        for (int i = 0; i < targetLocations.Count; i++)
        {
            x += IndicesToVertex((Vector3)targetLocations[i]) + ", ";
        }
        Debug.Log(x);
    }


    // remove all cells that don't have adjacent unknown cells from the target locations
    void RemoveCandidates()
    {
        for (int i = 0; i < targetLocations.Count; i++)
        {
            Vector3 tmp = (Vector3)targetLocations[i];
            if (!HasAdjacentUnknown(tmp))
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

        // dfs, get the nearest cell
        int index = targetLocations.Count - 1;

        // get a new target destination
        targetInstance = Instantiate(targetPrefab);
        // indices on the grid
        Vector3 nextPosition = (Vector3)targetLocations[index];

        // find a short path and add targets along that path
        if (routeToTarget.Count == 0)
        {
            // find a path from the agent to the next position
            FindRouteToTarget(IndicesToVertex(PositionToIndices(agent.transform.position)), IndicesToVertex(nextPosition));
            //routeToTarget.Push(IndicesToVertex(nextPosition));
            targetLocations.RemoveAt(index);
        }

        // set target position and agent destination to the next point on the path
        int nextVertix = (int)routeToTarget.Pop();

        nextPosition = IndicesToPosition(VertexToIndices(nextVertix));
        targetInstance.transform.position = nextPosition;
        agent.destination = targetInstance.transform.position;
    }



    Vector3 VertexToIndices(int a)
    {
        return new Vector3(a / mapSize, 1, a % mapSize);
    }



    void MarkUnreachable()
    {
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                if (map[i, j] == unknown)
                    map[i, j] = unreachable;
            }
        }
    }



    int IndicesToVertex(Vector3 a)
    {
        return (int)a.x * mapSize + (int)a.z;
    }



    Vector3 IndicesToPosition(Vector3 a)
    {
        return new Vector3(a.x * cellSize + cellSize / 2.0f, 1, a.z * cellSize + cellSize / 2.0f);
    }



    void FindRouteToTarget(int src, int dest)
    {
        Queue queueBFS = new Queue();
        Boolean[] visited = new Boolean[V];
        int[] prevCell = new int[V];

        for (int i = 0; i < V; i++)
        {
            prevCell[i] = -1;
            visited[i] = false;
        }

        queueBFS.Enqueue(src);
        int cur = src;
        visited[src] = true;

        while (queueBFS.Count != 0)
        {
            cur = (int)queueBFS.Dequeue();
            if (cur == dest)
            {
                // done
                break;
            }

            for (int i = 0; i < graph[cur].Count; i++)
            {
                int v = (int)((ArrayList)graph[cur])[i];

                if (!visited[v])
                {
                    visited[v] = true;
                    queueBFS.Enqueue(v);
                    prevCell[v] = cur;
                }
            }
        }

        // save the path in the route stack
        cur = dest;
        for (int i = 0; i < V; i++)
        {
            if (cur == src || cur == -1)
                break;
            routeToTarget.Push(cur);
            cur = prevCell[cur];
        }
    }



    void PrintRoute()
    {
        Stack x = new Stack();
        String s = "";

        while (routeToTarget.Count > 0)
        {
            int tmp = (int)routeToTarget.Pop();
            s += tmp + ", ";
            x.Push(tmp);
        }
        while (x.Count > 0)
        {

            routeToTarget.Push(x.Pop());
        }

        Debug.Log(s);
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



    // Raycast in 8 directions
    void Raycast8()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        RaycastHit hit;

        // Raycast in the 8 directions

        for (int i = 0; i < directions.Length; i++)
        {
            
            if (Physics.Raycast(agent.transform.position, directions[i], out hit, range))
            {
                if (hit.collider.tag == obstacleTag)
                {
                    // There's an object, mark this cell as occupied
                    MarkCell(PositionToIndices(hit.transform.position), occupied);
                }
            }
            else
            {
                // mark this cell as free
                // position of the cell
                int tmpx = (int)agent.transform.position.x + (int)(range * directions[i].x),
                    tmpz = (int)agent.transform.position.z + (int)(range * directions[i].z);
                Vector3 seenCell = PositionToIndices(new Vector3(tmpx, 1, tmpz));
                if (CheckBoundaries(seenCell))
                {
                    // add this cell to the candidate target locations if it has adjacent unkown cells, 
                    // and if it wasn't added to the candidate list before.. i.e. if it has ?                   
                    if (HasAdjacentUnknown(seenCell) && map[(int)seenCell.x, (int)seenCell.z] == '?')
                    {
                        targetLocations.Add(seenCell);
                    }
                    MarkCell(seenCell, empty);

                    // adding edges between empty cells, surrounding this cell
                    for (int k = 0; k < directions.Length; k++)
                    {
                        Vector3 surroundingCell = new Vector3(seenCell.x + directions[k].x, 1, seenCell.z + directions[k].z);
                        if (CheckBoundaries(surroundingCell))
                        {
                            AddEdge(IndicesToVertex(seenCell), IndicesToVertex(surroundingCell));
                        }
                    }
                }
            }
        }
    }


    Vector3 PositionToIndices(Vector3 a)
    {
        return new Vector3((int)(a.x / cellSize), 1, (int)(a.z / cellSize));
    }



    // given indices of the cell as a Vector
    Boolean HasAdjacentUnknown(Vector3 a)
    {
        int i = (int)a.x,
            j = (int)a.z;
        return (CheckBoundaries(new Vector3(i, 1, j + 1)) && map[i, j + 1] == '?')
            || (CheckBoundaries(new Vector3(i, 1, j - 1)) && map[i, j - 1] == '?')
            || (CheckBoundaries(new Vector3(i + 1, 1, j + 1)) && map[i + 1, j + 1] == '?')
            || (CheckBoundaries(new Vector3(i + 1, 1, j)) && map[i + 1, j] == '?')
            || (CheckBoundaries(new Vector3(i + 1, 1, j - 1)) && map[i + 1, j - 1] == '?')
            || (CheckBoundaries(new Vector3(i - 1, 1, j)) && map[i - 1, j] == '?')
            || (CheckBoundaries(new Vector3(i - 1, 1, j + 1)) && map[i - 1, j + 1] == '?')
            || (CheckBoundaries(new Vector3(i - 1, 1, j - 1)) && map[i - 1, j - 1] == '?');
    }



    // given indices of the cell
    Boolean CheckBoundaries(Vector3 a)
    {
        int i = (int)a.x,
            j = (int)a.z;
        return i > -1 && i < mapSize && j > -1 && j < mapSize;
    }



    // mark this cell as free or occupied
    // given indices of the cells
    void MarkCell(Vector3 a, char c)
    {
        int x = (int)a.x,
            z = (int)a.z;
        if (CheckBoundaries(a) && map[x, z] != occupied)
        {
            map[x, z] = c;
        }
    }



    // given vertices, add an edge between them
    void AddEdge(int a, int b)
    {
        if (!graph[a].Contains(b) && a != b)
        {
            graph[a].Add(b);
            graph[b].Add(a);
        }
    }

}

