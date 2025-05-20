
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[SerializeField]
	private VisionAgent m_AgentPrefab = null;

	[SerializeField]
	private Transform m_AgentsContainer = null;

	[SerializeField]
	private int m_AgentsAmount = 10;

	[SerializeField]
	private VisionTarget m_TargetPrefab = null;

	[SerializeField]
	private Transform m_TargetsContainer = null;

	[SerializeField]
	private int m_TargetsAmount = 4;

	[SerializeField]
	private SphereCollider m_SpawnZone = null;

	[SerializeField]
	private bool m_UseRaycastMethod = false;

	private readonly List<VisionAgent> m_VisionAgents = new();
	private readonly List<VisionTarget> m_VisionTargets = new();
	public IReadOnlyList<VisionTarget> VisionTargets => m_VisionTargets;

	public static GameManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		VisionAgent[] agents = FindObjectsByType<VisionAgent>(FindObjectsSortMode.None);
		foreach (VisionAgent agent in agents)
		{
			agent.SetUseRaycastDetection(m_UseRaycastMethod);
			m_VisionAgents.Add(agent);
		}

		m_VisionTargets.AddRange(FindObjectsByType<VisionTarget>(FindObjectsSortMode.None));

		for (int i = m_VisionAgents.Count; i < m_AgentsAmount; ++i)
		{
			Vector3 randomPosition = GetRandomSpawnPosition();
			Quaternion randomRotation = GetRandomSpawnRotation();

			VisionAgent agent = Instantiate(m_AgentPrefab, randomPosition, randomRotation, m_AgentsContainer);
			agent.SetUseRaycastDetection(m_UseRaycastMethod);
			m_VisionAgents.Add(agent);
		}

		for (int i = m_VisionTargets.Count; i < m_TargetsAmount; ++i)
		{
			Vector3 randomPosition = GetRandomSpawnPosition();
			Quaternion randomRotation = GetRandomSpawnRotation();

			m_VisionTargets.Add(Instantiate(m_TargetPrefab, randomPosition, randomRotation, m_TargetsContainer));
		}
	}

	private void OnDestroy()
	{
		Instance = null;

		foreach (VisionAgent agent in m_VisionAgents)
		{
			if (agent != null && agent.gameObject != null)
				Destroy(agent.gameObject);
		}

		foreach (VisionTarget target in m_VisionTargets)
		{
			if (target != null && target.gameObject != null)
				Destroy(target.gameObject);
		}
	}

	private Vector3 GetRandomSpawnPosition()
	{
		Vector2 randomInCircle = Random.insideUnitCircle * m_SpawnZone.radius;
		return new Vector3(randomInCircle.x, 0.0f, randomInCircle.y) + m_SpawnZone.transform.position;
	}

	private static Quaternion GetRandomSpawnRotation()
	{
		float randomAngle = Random.Range(0.0f, 2.0f * Mathf.PI);
		return Quaternion.Euler(0.0f, randomAngle, 0.0f);
	}
}
