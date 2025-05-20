
using UnityEngine;
using UnityEngine.AI;

public class WanderBehavior : MonoBehaviour
{
	[SerializeField] private float m_WanderRadius = 50.0f;
	[SerializeField] private float m_MinWanderTime = 5.0f;
	[SerializeField] private float m_MaxWanderTime = 20.0f;
	[SerializeField] private float m_RotationSpeed = 5.0f;
	[SerializeField] private NavMeshAgent m_Agent = null;

	private float m_Timer;
	private float m_WanderTimer;

	private void Start()
	{
		WanderToRandomPoint();
		SetRandomWanderTime();
	}

	private void Update()
	{
		m_Timer += Time.deltaTime;

		if (m_Timer >= m_WanderTimer || m_Agent.isStopped)
		{
			WanderToRandomPoint();
			SetRandomWanderTime();
		}

		RotateAgentTowardsDestination();
	}

	private void SetRandomWanderTime()
	{
		m_WanderTimer = Random.Range(m_MinWanderTime, m_MaxWanderTime);
		m_Timer = 0.0f;
	}

	private void WanderToRandomPoint()
	{
		Vector3 randomDirection = Random.insideUnitSphere * m_WanderRadius;
		randomDirection += transform.position;

		NavMeshHit hit;
		if (NavMesh.SamplePosition(randomDirection, out hit, m_WanderRadius, NavMesh.AllAreas))
		{
			m_Agent.SetDestination(hit.position);
		}
	}

	private void RotateAgentTowardsDestination()
	{
		Vector3 directionToTarget = m_Agent.steeringTarget - transform.position;
		if (directionToTarget.sqrMagnitude <= 0.5f)
			return;

		Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_RotationSpeed * Time.deltaTime);
	}
}
