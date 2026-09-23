using UnityEngine;
using UnityEngine.AI;

/*
 * NPC通用ai控制器
 * 
 */

public abstract class CommonAIController : MonoBehaviour
{
    public NavMeshAgent Agent { get; private set; }
    
    public virtual void Initialize(Enemy enemy)
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    public virtual void AIOnMove(Transform target)
    {
        if (Agent == null && !Agent.enabled) return;
        if (target == null) return;

        Agent.SetDestination(target.position);
    }
    
    public virtual T GetParentRef<T>(ref T parent)
    {
        return GetComponent<T>();
    }
}
