
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

	private readonly List<VisionTarget> m_VisionTargetsInRange = new();

	public bool DetectVision(VisionAgent agent)
	{
		DetectVisionTargetsInRange();
		return RaycastOnTargetsInRange(agent);
	}

	private void DetectVisionTargetsInRange()
	{
		m_VisionTargetsInRange.Clear();
		m_VisionTargetsInRange.AddRange(GameManager.Instance.VisionTargets);

		for (int i = m_VisionTargetsInRange.Count - 1; i >= 0; --i)
		{
			if (Vector3.Distance(transform.position, m_VisionTargetsInRange[i].transform.position) > m_VisionDistance ||
				Vector3.Angle(transform.position, m_VisionTargetsInRange[i].transform.position) > m_FieldOfView * 0.5f)
				m_VisionTargetsInRange.RemoveAt(i);
		}
	}

	private bool RaycastOnTargetsInRange(VisionAgent agent)
	{
		bool hasDetectedATarget = false;

		foreach (VisionTarget target in m_VisionTargetsInRange)
		{
			IReadOnlyList<Transform> raycastTargets = target.RaycastTargets;

			foreach (Transform raycastTarget in raycastTargets)
			{
				if (Physics.Raycast(agent.EyeSocket.position, raycastTarget.position, out RaycastHit hit, m_VisionDistance, m_TargetLayer))
				{
					Debug.Log($"Target detected: {target.name}");
					hasDetectedATarget = true;

					break;
				}
			}
		}

		return hasDetectedATarget;
	}
}
