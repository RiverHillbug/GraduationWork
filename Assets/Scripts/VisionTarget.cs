
using System.Collections.Generic;
using UnityEngine;

public class VisionTarget : MonoBehaviour
{
	[SerializeField]
	private WanderBehavior m_WanderBehavior = null;

	[SerializeField]
	private List<Transform> m_RaycastTargets = new();
	public IReadOnlyList<Transform> RaycastTargets => m_RaycastTargets;

	private readonly HashSet<Collider> m_AllColliders = new();

	private void Awake()
	{
		Collider[] colliders = GetComponentsInChildren<Collider>();
		foreach (Collider collider in colliders)
			m_AllColliders.Add(collider);
	}

	public bool HasCollider(Collider collider)
	{
		return m_AllColliders.Contains(collider);
	}

	public void Reset()
	{
		m_WanderBehavior.Reset();
	}
}
