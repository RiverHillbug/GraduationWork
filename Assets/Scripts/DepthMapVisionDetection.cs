
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Profiling;

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
	private LayerMask m_EnvironmentLayer = 0;

	[SerializeField]
	private LayerMask m_TargetsLayer = 0;

	[SerializeField]
	private bool m_UseShapeDetectionOptimization = true;

	[SerializeField]
	private Texture2D m_TargetsDepthMap = null;
	private bool m_ProvidedTestTexture = false;

	private RenderTexture m_TargetsDepthTexture = null;
	private RenderTexture m_EnvironmentDepthTexture = null;
	private Texture2D m_EnvironmentDepthMap = null;

	private readonly List<VisionAgent> m_RegisteredAgents = new();
	private int m_CurrentAgentIndex = 0;

	private int m_AgentsToCheckPerFrame = 1;

	private readonly List<VisionTarget> m_VisionTargetsInRange = new();
	private readonly HashSet<VisionTarget> m_CurrentlyDetectedTargets = new();
	private readonly HashSet<Vector2Int> m_AlreadyDetectedPixels = new();
	private readonly HashSet<Vector2Int> m_AlreadyCheckedPixels = new();
	private readonly Queue<Vector2Int> m_NextVerticalChecks = new();

	private void Awake()
	{
		m_Camera.depthTextureMode = DepthTextureMode.Depth;
		m_Camera.SetReplacementShader(m_DepthShader, replacementTag: string.Empty);
		m_Camera.enabled = false;

		int textureWidth = Mathf.RoundToInt(m_Camera.pixelWidth * m_TexturesSizeMultiplier);
		int textureHeight = Mathf.RoundToInt(m_Camera.pixelHeight * m_TexturesSizeMultiplier);

		if (m_TargetsDepthMap != null)
		{
			textureWidth = m_TargetsDepthMap.width;
			textureHeight = m_TargetsDepthMap.height;
			m_ProvidedTestTexture = true;
		}

		if (!m_ProvidedTestTexture)
		{
			m_TargetsDepthTexture = new RenderTexture(textureWidth, textureHeight, 24);
			m_EnvironmentDepthTexture = new RenderTexture(textureWidth, textureHeight, 24);
		}

		if (!m_ProvidedTestTexture)
			m_TargetsDepthMap = new(textureWidth, textureHeight, TextureFormat.RFloat, false);
		m_EnvironmentDepthMap = new(textureWidth, textureHeight, TextureFormat.RFloat, false);

		if (m_ProvidedTestTexture)
		{
			for (int y = 0; y < textureHeight; ++y)
			{
				for (int x = 0; x < textureWidth; ++x)
				{
					m_EnvironmentDepthMap.SetPixel(x, y, Color.white);
				}
			}
		}
	}

	private void Update()
	{
		if (m_RegisteredAgents.Count == 0)
			return;

		Profiler.BeginSample("DepthMapVisionDetection");

		for (int i = 0; i < m_AgentsToCheckPerFrame; ++i)
		{
			if (m_CurrentAgentIndex >= m_RegisteredAgents.Count)
				m_CurrentAgentIndex = 0;

			DetectVisionImmediate(m_RegisteredAgents[m_CurrentAgentIndex]);
			++m_CurrentAgentIndex;
		}

		Profiler.EndSample();
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

	public void SetAgentsToCheckPerFrame(int count)
	{
		m_AgentsToCheckPerFrame = Mathf.Clamp(count, 1, m_RegisteredAgents.Count);
	}

	/// <summary>
	/// Avoid calling this unless you need the result immediately. Prefer <see cref="RegisterAgentForVisionDetection"/> instead for continuous vision detection.
	/// </summary>
	public bool DetectVisionImmediate(VisionAgent agent)
	{
		transform.SetPositionAndRotation(agent.EyeSocket.position, agent.EyeSocket.rotation);
		m_Camera.enabled = true;

		DetectVisionTargetsInRange();

		if (!m_ProvidedTestTexture)
		{
			RenderEnvironment();
			RenderTargets();
		}

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
		m_AlreadyDetectedPixels.Clear();
		m_AlreadyCheckedPixels.Clear();

		for (int y = 0; y < m_EnvironmentDepthMap.height; ++y)
		{
			for (int x = 0; x < m_EnvironmentDepthMap.width; ++x)
			{
				if (m_UseShapeDetectionOptimization && m_AlreadyDetectedPixels.Contains(new(x, y)))
					continue;

				float envDepth = m_EnvironmentDepthMap.GetPixel(x, y).r;
				float targetDepth = m_TargetsDepthMap.GetPixel(x, y).r;

				if (targetDepth < envDepth)
				{
					//Profiler.EndSample();

					RaycastOnTarget((float)x / m_TargetsDepthMap.width, (float)y / m_TargetsDepthMap.height, targetDepth * (m_Camera.farClipPlane - m_Camera.nearClipPlane));

					if (m_UseShapeDetectionOptimization)
						DetectShapePixels(x, y);

					//Profiler.BeginSample("DepthMapVisionDetection");
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

				//Debug.Log($"Target detected: {target.name}");
			}

			break;
		}
	}

	private void DetectShapePixels(int x, int y)
	{
		//float startingDepth = m_TargetsDepthMap.GetPixel(x, y).r;
		Vector2Int startingPixel = new(x, y);

		//m_AlreadyDetectedPixels.Add(startingPixel);
		//DetectSurroundingSimilarPixels(startingPixel, startingDepth);
		DetectSurroundingSimilarPixelsByMarching(startingPixel);

		/*int textureWidth = m_TargetsDepthMap.width;
		int textureHeight = m_TargetsDepthMap.height;
		Texture2D outputTest = new(textureWidth, textureHeight, TextureFormat.RFloat, false);

		for (y = 0; y < textureHeight; ++y)
		{
			for (x = 0; x < textureWidth; ++x)
			{
				outputTest.SetPixel(x, y, m_AlreadyDetectedPixels.Contains(new(x, y)) ? Color.black : Color.white);
			}
		}

		byte[] bytes = outputTest.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/Detected.png", bytes);*/
	}

	private void DetectSurroundingSimilarPixels(Vector2Int startingPixel,float startingDepth)
	{
		const float depthDifferenceToConsiderDifferentEntity = 0.01f;

		Vector2Int right = new(startingPixel.x + 1, startingPixel.y);
		if (!m_AlreadyDetectedPixels.Contains(right) && !m_AlreadyCheckedPixels.Contains(right) && right.x < m_TargetsDepthMap.width)
		{
			m_AlreadyCheckedPixels.Add(right);

			float depth = m_TargetsDepthMap.GetPixel(right.x, right.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_AlreadyDetectedPixels.Add(right);
				DetectSurroundingSimilarPixels(right, depth);
			}
		}

		Vector2Int up = new(startingPixel.x, startingPixel.y + 1);
		if (!m_AlreadyDetectedPixels.Contains(up) && !m_AlreadyCheckedPixels.Contains(up) && up.y < m_TargetsDepthMap.height)
		{
			m_AlreadyCheckedPixels.Add(up);

			float depth = m_TargetsDepthMap.GetPixel(up.x, up.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_AlreadyDetectedPixels.Add(up);
				DetectSurroundingSimilarPixels(up, depth);
			}
		}

		Vector2Int left = new(startingPixel.x - 1, startingPixel.y);
		if (!m_AlreadyDetectedPixels.Contains(left) && !m_AlreadyCheckedPixels.Contains(left) && left.x >= 0)
		{
			m_AlreadyCheckedPixels.Add(left);

			float depth = m_TargetsDepthMap.GetPixel(left.x, left.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_AlreadyDetectedPixels.Add(left);
				DetectSurroundingSimilarPixels(left, depth);
			}
		}

		Vector2Int down = new(startingPixel.x, startingPixel.y - 1);
		if (!m_AlreadyDetectedPixels.Contains(down) && !m_AlreadyCheckedPixels.Contains(down) && down.y >= 0)
		{
			m_AlreadyCheckedPixels.Add(down);

			float depth = m_TargetsDepthMap.GetPixel(down.x, down.y).r;
			if (Mathf.Abs(depth - startingDepth) <= depthDifferenceToConsiderDifferentEntity)
			{
				m_AlreadyDetectedPixels.Add(down);
				DetectSurroundingSimilarPixels(down, depth);
			}
		}
	}

	private void DetectSurroundingSimilarPixelsByMarching(Vector2Int startingPixel)
	{
		bool isDone = false;
		Vector2Int nextCheckLeft = -Vector2Int.one;
		bool isMovingLeft = false;

		bool isContinuousUp = false;
		bool isContinuousDown = false;

		Vector2Int currentPosition = startingPixel;
		m_AlreadyDetectedPixels.Add(currentPosition);

		do
		{
			float currentDepth = m_TargetsDepthMap.GetPixel(currentPosition.x, currentPosition.y).r;

			Vector2Int up = new(currentPosition.x, currentPosition.y + 1);
			if (!m_AlreadyCheckedPixels.Contains(up) && !m_AlreadyDetectedPixels.Contains(up) && up.y < m_TargetsDepthMap.height)
			{
				m_AlreadyCheckedPixels.Add(up);

				if (IsPixelSimilarDepth(up, currentDepth))
				{
					m_AlreadyDetectedPixels.Add(up);

					if (!isContinuousUp)
						m_NextVerticalChecks.Enqueue(up);

					isContinuousUp = true;
				}
				else
				{
					isContinuousUp = false;
				}
			}
			else
			{
				isContinuousUp = m_AlreadyDetectedPixels.Contains(up);
			}

			Vector2Int down = new(currentPosition.x, currentPosition.y - 1);
			if (!m_AlreadyCheckedPixels.Contains(down) && !m_AlreadyDetectedPixels.Contains(down) && down.y >= 0)
			{
				m_AlreadyCheckedPixels.Add(down);

				if (IsPixelSimilarDepth(down, currentDepth))
				{
					m_AlreadyDetectedPixels.Add(down);

					if (!isContinuousDown)
						m_NextVerticalChecks.Enqueue(up);

					isContinuousDown = true;
				}
				else
				{
					isContinuousDown = false;
				}
			}
			else
			{
				isContinuousDown = m_AlreadyDetectedPixels.Contains(down);
			}

			currentPosition.x += (isMovingLeft ? -1 : 1);

			bool cantMoveFurther = true;
			if (!m_AlreadyCheckedPixels.Contains(currentPosition) && !m_AlreadyDetectedPixels.Contains(currentPosition) && currentPosition.x >= 0 && currentPosition.x < m_TargetsDepthMap.width)
			{
				m_AlreadyCheckedPixels.Add(currentPosition);

				if (IsPixelSimilarDepth(currentPosition, currentDepth))
				{
					m_AlreadyDetectedPixels.Add(currentPosition);
					cantMoveFurther = false;
				}
			}

			if (cantMoveFurther)
			{
				if (nextCheckLeft != -Vector2Int.one)
				{
					currentPosition = nextCheckLeft;
					nextCheckLeft = -Vector2Int.one;
					isMovingLeft = true;

					isContinuousUp = m_AlreadyDetectedPixels.Contains(currentPosition + new Vector2Int(1, 1));
					isContinuousDown = m_AlreadyDetectedPixels.Contains(currentPosition + new Vector2Int(1, -1));
				}
				else
				{
					isMovingLeft = false;

					if (m_NextVerticalChecks.Count > 0)
					{
						currentPosition = m_NextVerticalChecks.Dequeue();
						currentDepth = m_TargetsDepthMap.GetPixel(currentPosition.x, currentPosition.y).r;

						isContinuousUp = m_AlreadyDetectedPixels.Contains(currentPosition + Vector2Int.up);
						isContinuousDown = m_AlreadyDetectedPixels.Contains(currentPosition + Vector2Int.down);

						Vector2Int left = new(currentPosition.x - 1, currentPosition.y);
						if (left.x >= 0)
						{
							m_AlreadyCheckedPixels.Add(left);

							if (IsPixelSimilarDepth(left, currentDepth))
							{
								m_AlreadyDetectedPixels.Add(left);
								nextCheckLeft = left;
							}
						}
					}
					else
					{
						isDone = true;
					}
				}
			}
		} while (!isDone);
	}

	private bool IsPixelSimilarDepth(Vector2Int pixel, float depthToCheckAgainst)
	{
		const float depthDifferenceToConsiderDifferentEntity = 0.01f;

		float depth = m_TargetsDepthMap.GetPixel(pixel.x, pixel.y).r;
		return Mathf.Abs(depth - depthToCheckAgainst) <= depthDifferenceToConsiderDifferentEntity;
	}
}
