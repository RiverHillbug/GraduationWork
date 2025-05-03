
using System.Collections.Generic;
using UnityEngine;

public class RaycastVisionDetection : MonoBehaviour
{
	[SerializeField]
	private float m_VisionDistance = 50.0f;
	[SerializeField]
	private float m_FieldOfView = 100.0f;
	[SerializeField]
	private LayerMask m_ObstacleLayer;
	[SerializeField]
	private LayerMask m_TargetLayer;

	private readonly List<Transform> m_VisionTargetsInRange = new();

	private void Update()
	{
		DetectVisionTargetsInRange();

		if (m_VisionTargetsInRange.Count == 0)
			return;

		DetectVision();
	}

	private void DetectVisionTargetsInRange()
	{
		m_VisionTargetsInRange.Clear();
		m_VisionTargetsInRange.AddRange(GameManager.Instance.GetVisionTargets());

		for (int i = m_VisionTargetsInRange.Count - 1; i >= 0; --i)
		{
			if (Vector3.Distance(transform.position, m_VisionTargetsInRange[i].position) > m_VisionDistance ||
				Vector3.Angle(transform.position, m_VisionTargetsInRange[i].position) > m_FieldOfView * 0.5f)
				m_VisionTargetsInRange.RemoveAt(i);
		}
	}

	private void DetectVision()
	{
		if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, m_VisionDistance, m_TargetLayer))
			return;

		float angle = Vector3.Angle(transform.forward, hit.transform.position - transform.position);
		if (angle < m_FieldOfView / 2)
			Debug.Log("Target detected: " + hit.transform.name);
	}
}
