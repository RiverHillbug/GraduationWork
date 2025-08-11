
using UnityEngine;

public class VisionAgent : MonoBehaviour
{
	[SerializeField]
	private RaycastVisionDetection m_RaycastVisionDetection = null;

	[SerializeField]
	private bool m_UseRaycastDetection = false;

	[SerializeField]
	private bool m_UpdateVisionEveryFrame = true;

	[SerializeField]
	private Transform m_EyeSocket = null;

	[SerializeField]
	private WanderBehavior m_WanderBehavior = null;

	private DepthMapVisionDetection m_DepthMapVisionDetection = null;

	public Transform EyeSocket => m_EyeSocket;

	private void OnEnable()
	{
		if (m_DepthMapVisionDetection == null)
			m_DepthMapVisionDetection = FindFirstObjectByType<DepthMapVisionDetection>();

		if (m_UpdateVisionEveryFrame && !m_UseRaycastDetection)
			m_DepthMapVisionDetection.RegisterAgentForVisionDetection(this);
	}

	private void OnDisable()
	{
		m_DepthMapVisionDetection.UnregisterAgent(this);
	}

	private void Update()
	{
		if (m_UpdateVisionEveryFrame && m_UseRaycastDetection)
			m_RaycastVisionDetection.DetectVision(this);
	}

	public bool DetectVisionImmediate()
	{
		return m_UseRaycastDetection ? m_RaycastVisionDetection.DetectVision(this) : m_DepthMapVisionDetection.DetectVisionImmediate(this);
	}

	public void SetUseRaycastDetection(bool useRaycast)
	{
		if (useRaycast == m_UseRaycastDetection)
			return;

		m_UseRaycastDetection = useRaycast;

		if (!m_UpdateVisionEveryFrame)
			return;

		if (useRaycast)
			m_DepthMapVisionDetection.UnregisterAgent(this);
		else
			m_DepthMapVisionDetection.RegisterAgentForVisionDetection(this);
	}

	public void Reset()
	{
		m_WanderBehavior.Reset();
	}
}
