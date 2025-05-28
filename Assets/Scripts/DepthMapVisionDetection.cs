
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;

public class DepthMapVisionDetection : MonoBehaviour
{
	[SerializeField]
	private Camera m_Camera = null;

	[SerializeField]
	private Shader m_DepthShader = null;

	[SerializeField]
	[Tooltip("Textures will be the size of the screen multiplied by this number (lower number reduces precision but drastically increases performances)")]
	[Range(0.01f, 1.0f)]
	private float m_TexturesSizeMultiplier = 0.8f;

	[SerializeField]
	private int m_AgentsToCheckPerFrame = 1;

	[SerializeField]
	private LayerMask m_EnvironmentLayer = 0;

	[SerializeField]
	private LayerMask m_TargetsLayer = 0;

	private RenderTexture m_TargetsDepthTexture = null;
	private RenderTexture m_EnvironmentDepthTexture = null;
	private Texture2D m_EnvironmentDepthMap = null;
	private Texture2D m_TargetsDepthMap = null;

	private readonly List<VisionAgent> m_RegisteredAgents = new();
	private int m_CurrentAgentIndex = 0;

	private readonly List<VisionTarget> m_VisionTargetsInRange = new();
	private readonly HashSet<VisionTarget> m_CurrentlyDetectedTargets = new();

	private void Awake()
	{
		m_Camera.depthTextureMode = DepthTextureMode.Depth;
		m_Camera.SetReplacementShader(m_DepthShader, replacementTag: string.Empty);
		m_Camera.enabled = false;

		int textureWidth = Mathf.RoundToInt(m_Camera.pixelWidth * m_TexturesSizeMultiplier);
		int textureHeight = Mathf.RoundToInt(m_Camera.pixelHeight * m_TexturesSizeMultiplier);

		m_TargetsDepthTexture = new RenderTexture(textureWidth, textureHeight, 24);
		m_EnvironmentDepthTexture = new RenderTexture(textureWidth, textureHeight, 24);

		m_TargetsDepthMap = new(textureWidth, textureHeight, TextureFormat.RFloat, false);
		m_EnvironmentDepthMap = new(textureWidth, textureHeight, TextureFormat.RFloat, false);
	}

	private void Update()
	{
		if (m_RegisteredAgents.Count == 0)
			return;

		for (int i = 0; i < m_AgentsToCheckPerFrame; ++i)
		{
			if (m_CurrentAgentIndex >= m_RegisteredAgents.Count)
				m_CurrentAgentIndex = 0;

			DetectVisionImmediate(m_RegisteredAgents[m_CurrentAgentIndex]);
			++m_CurrentAgentIndex;
		}
	}

	public void RegisterAgentForVisionDetection(VisionAgent agent)
	{
		if (!m_RegisteredAgents.Contains(agent))
			m_RegisteredAgents.Add(agent);
	}

	public void UnregisterAgent(VisionAgent agent)
	{
		m_RegisteredAgents.Remove(agent);
	}

	/// <summary>
	/// Avoid calling this unless you need the result immediately. Prefer <see cref="RegisterAgentForVisionDetection"/> instead for continuous vision detection.
	/// </summary>
	public bool DetectVisionImmediate(VisionAgent agent)
	{
		transform.SetPositionAndRotation(agent.EyeSocket.position, agent.EyeSocket.rotation);
		m_Camera.enabled = true;

		DetectVisionTargetsInRange();
		RenderEnvironment();
		RenderTargets();

		m_Camera.enabled = false;

		return CompareTextures();
	}

	private void DetectVisionTargetsInRange()
	{
		m_VisionTargetsInRange.Clear();
		m_VisionTargetsInRange.AddRange(GameManager.Instance.VisionTargets);

		for (int i = m_VisionTargetsInRange.Count - 1; i >= 0; --i)
		{
			if (Vector3.Distance(transform.position, m_VisionTargetsInRange[i].transform.position) > m_Camera.farClipPlane ||
				Vector3.Angle(transform.position, m_VisionTargetsInRange[i].transform.position) > m_Camera.fieldOfView * 0.5f)
				m_VisionTargetsInRange.RemoveAt(i);
		}
	}

	private void RenderEnvironment()
	{
		m_Camera.cullingMask = m_EnvironmentLayer;
		m_Camera.targetTexture = m_EnvironmentDepthTexture;
		m_Camera.Render();

		RenderTexture.active = m_EnvironmentDepthTexture;
		m_EnvironmentDepthMap.ReadPixels(new Rect(0, 0, m_EnvironmentDepthTexture.width, m_EnvironmentDepthTexture.height), 0, 0);
		m_EnvironmentDepthMap.Apply();

		/*byte[] bytes = m_EnvironmentDepthMap.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/Environment.png", bytes);*/

		RenderTexture.active = null;
		m_Camera.targetTexture = null;
	}

	private void RenderTargets()
	{
		m_Camera.cullingMask = m_TargetsLayer;
		m_Camera.targetTexture = m_TargetsDepthTexture;
		m_Camera.Render();

		RenderTexture.active = m_TargetsDepthTexture;
		m_TargetsDepthMap.ReadPixels(new Rect(0, 0, m_TargetsDepthTexture.width, m_TargetsDepthTexture.height), 0, 0);
		m_TargetsDepthMap.Apply();

		/*byte[] bytes = m_TargetsDepthMap.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/Targets.png", bytes);*/

		RenderTexture.active = null;
		m_Camera.targetTexture = null;
	}

	private bool CompareTextures()
	{
		m_CurrentlyDetectedTargets.Clear();

		for (int y = 0; y < m_EnvironmentDepthMap.height; ++y)
		{
			for (int x = 0; x < m_EnvironmentDepthMap.width; ++x)
			{
				float envDepth = m_EnvironmentDepthMap.GetPixel(x, y).r;
				float targetDepth = m_TargetsDepthMap.GetPixel(x, y).r;

				if (targetDepth < envDepth)
				{
					RaycastOnTarget((float)x / m_TargetsDepthMap.width, (float)y / m_TargetsDepthMap.height, targetDepth * (m_Camera.farClipPlane - m_Camera.nearClipPlane));
					//EraseTargetShape(x, y); // Needs some fixing (make it not recursive, mostly)
				}
			}
		}

		return m_CurrentlyDetectedTargets.Count > 0;
	}

	private void RaycastOnTarget(float x, float y, float distance)
	{
		Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y));
		//Debug.DrawLine(ray.origin, ray.origin + (ray.direction * (distance * 2.0f)), Color.red);

		// The distance is not exact, since the depth map is based on near clip plane, instead of camera position, so we can't use it with precision
		if (!Physics.Raycast(ray, out RaycastHit hit, distance * 2.0f, m_TargetsLayer))
			return;

		foreach (VisionTarget target in m_VisionTargetsInRange)
		{
			if (!target.HasCollider(hit.collider))
				continue;

			if (!m_CurrentlyDetectedTargets.Contains(target))
			{
				m_CurrentlyDetectedTargets.Add(target);

				Debug.Log($"Target detected: {target.name}");
			}

			break;
		}
	}

	private void EraseTargetShape(int x, int y)
	{
		// Closed list are the pixels that have or are being treated already.
		// It's a hashset because we just need to know that they are in there, we don't iterate over them from it.
		HashSet<Vector2Int> closedList = HashSetPool<Vector2Int>.Get();

		float startingDepth = m_TargetsDepthMap.GetPixel(x, y).r;
		EraseSurroundingSimilarPixels(new(x, y), startingDepth, ref closedList);
		m_TargetsDepthMap.SetPixel(x, y, Color.white);

		closedList.Clear();
		HashSetPool<Vector2Int>.Release(closedList);

		/*byte[] bytesAfter = m_TargetsDepthMap.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/After.png", bytesAfter);*/
	}

	private void EraseSurroundingSimilarPixels(Vector2Int startingPixel,float startingDepth, ref HashSet<Vector2Int> closedList)
	{
		const float depthDifferenceToConsiderDifferentEntity = 0.01f;

		Vector2Int right = new(startingPixel.x + 1, startingPixel.y);
		if (!closedList.Contains(right) && startingPixel.x + 1 < m_TargetsDepthMap.width)
		{
			closedList.Add(right);

			float depth = m_TargetsDepthMap.GetPixel(right.x, right.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_TargetsDepthMap.SetPixel(right.x, right.y, Color.white);
				EraseSurroundingSimilarPixels(right, depth, ref closedList);
			}
		}

		Vector2Int up = new(startingPixel.x, startingPixel.y + 1);
		if (!closedList.Contains(up) && startingPixel.y + 1 < m_TargetsDepthMap.height)
		{
			closedList.Add(up);

			float depth = m_TargetsDepthMap.GetPixel(up.x, up.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_TargetsDepthMap.SetPixel(up.x, up.y, Color.white);
				EraseSurroundingSimilarPixels(up, depth, ref closedList);
			}
		}

		Vector2Int left = new(startingPixel.x - 1, startingPixel.y);
		if (!closedList.Contains(left) && startingPixel.x - 1 >= 0)
		{
			closedList.Add(left);

			float depth = m_TargetsDepthMap.GetPixel(left.x, left.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_TargetsDepthMap.SetPixel(left.x, left.y, Color.white);
				EraseSurroundingSimilarPixels(left, depth, ref closedList);
			}
		}

		Vector2Int down = new(startingPixel.x, startingPixel.y - 1);
		if (!closedList.Contains(down) && startingPixel.y - 1 >= 0)
		{
			closedList.Add(down);

			float depth = m_TargetsDepthMap.GetPixel(down.x, down.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_TargetsDepthMap.SetPixel(down.x, down.y, Color.white);
				EraseSurroundingSimilarPixels(down, depth, ref closedList);
			}
		}
	}
}
