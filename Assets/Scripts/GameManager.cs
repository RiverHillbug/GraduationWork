
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
	private DepthMapVisionDetection m_DepthMapDetection = null;

	[SerializeField]
	private int m_TargetsAmount = 4;

	[SerializeField]
	private SphereCollider m_SpawnZone = null;

	[SerializeField]
	private bool m_UseRaycastMethod = false;

	[SerializeField]
	[Min(1)]
	private int m_AgentsToCheckPerFrame = 1;

	private readonly List<VisionAgent> m_VisionAgents = new();
	private readonly List<VisionTarget> m_VisionTargets = new();

	private readonly Queue<VisionAgent> m_VisionDetectionQueue = new();

	public static GameManager Instance { get; private set; }

	public IReadOnlyList<VisionTarget> VisionTargets => m_VisionTargets;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		VisionAgent[] agents = FindObjectsByType<VisionAgent>(FindObjectsSortMode.None);
		foreach (VisionAgent agent in agents)
		{
			agent.SetUseRaycastDetection(m_UseRaycastMethod);
			m_VisionAgents.Add(agent);
		}

		m_VisionTargets.AddRange(FindObjectsByType<VisionTarget>(FindObjectsSortMode.None));
	}

	private void Start()
	{
		BenchmarkRunner benchmarkRunner = FindAnyObjectByType<BenchmarkRunner>(FindObjectsInactive.Exclude);
		if (benchmarkRunner == null || !benchmarkRunner.enabled)
			Initialize(m_AgentsAmount, m_TargetsAmount, m_UseRaycastMethod);
	}

	public void Initialize(int agentsAmount, int targetsAmount, bool useRaycast)
	{
		m_AgentsAmount = agentsAmount;
		m_TargetsAmount = targetsAmount;
		m_UseRaycastMethod = useRaycast;

		m_VisionDetectionQueue.Clear();

		for (int i = 0; i < m_AgentsAmount; ++i)
		{
			Vector3 randomPosition = GetRandomSpawnPosition();
			Quaternion randomRotation = GetRandomSpawnRotation();

			VisionAgent agent;

			if (i >= m_VisionAgents.Count)
			{
				agent = Instantiate(m_AgentPrefab, randomPosition, randomRotation, m_AgentsContainer);
				m_VisionAgents.Add(agent);
			}
			else
			{
				agent = m_VisionAgents[i];

				agent.gameObject.SetActive(true);
				agent.Reset();
				agent.transform.SetPositionAndRotation(randomPosition, randomRotation);
			}

			agent.SetUseRaycastDetection(m_UseRaycastMethod);

			if (m_UseRaycastMethod)
				m_VisionDetectionQueue.Enqueue(agent);
		}

		for (int i = m_AgentsAmount; i < m_VisionAgents.Count; ++i)
			m_VisionAgents[i].gameObject.SetActive(false);

		for (int i = 0; i < m_TargetsAmount; ++i)
		{
			Vector3 randomPosition = GetRandomSpawnPosition();
			Quaternion randomRotation = GetRandomSpawnRotation();

			if (i >= m_VisionTargets.Count)
			{
				m_VisionTargets.Add(Instantiate(m_TargetPrefab, randomPosition, randomRotation, m_TargetsContainer));
			}
			else
			{
				VisionTarget target = m_VisionTargets[i];

				target.gameObject.SetActive(true);
				target.Reset();
				target.transform.SetPositionAndRotation(randomPosition, randomRotation);
			}
		}

		for (int i = m_TargetsAmount; i < m_VisionTargets.Count; ++i)
			m_VisionTargets[i].gameObject.SetActive(false);

		m_DepthMapDetection.SetAgentsToCheckPerFrame(m_AgentsToCheckPerFrame);
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

	private void Update()
	{
		if (!m_UseRaycastMethod || m_VisionDetectionQueue.Count == 0)
			return;

		for (int i = 0; i < m_AgentsToCheckPerFrame; ++i)
		{
			m_VisionDetectionQueue.Peek().DetectVisionImmediate();
			m_VisionDetectionQueue.Enqueue(m_VisionDetectionQueue.Dequeue());
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
