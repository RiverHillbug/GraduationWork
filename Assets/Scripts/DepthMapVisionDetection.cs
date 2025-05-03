
using UnityEngine;

public class DepthMapVisionDetection : MonoBehaviour
{
	[SerializeField]
	private Camera m_Vision = null;

	[SerializeField]
	private LayerMask m_EnvironmentLayer = 0;
	[SerializeField]
	private LayerMask m_TargetsLayer = 0;

	private Texture2D m_EnvironmentDepthMap;
	private Texture2D m_TargetsDepthMap;

	private RenderTexture m_EnvironmentDepthTexture;
	private RenderTexture m_TargetsDepthTexture;

	private void Awake()
	{
		m_EnvironmentDepthTexture = new RenderTexture(Screen.width, Screen.height, 24);
		m_TargetsDepthTexture = new RenderTexture(Screen.width, Screen.height, 24);

		m_EnvironmentDepthMap = new(m_EnvironmentDepthTexture.width, m_EnvironmentDepthTexture.height, TextureFormat.RFloat, false);
		m_TargetsDepthMap = new(m_TargetsDepthTexture.width, m_TargetsDepthTexture.height, TextureFormat.RFloat, false);
	}

	private void Update()
	{
		//ClearTextures();
		RenderEnvironment();
		RenderTargets();
		DetectVision();
	}

	private void RenderEnvironment()
	{
		m_Vision.cullingMask = m_EnvironmentLayer;
		m_Vision.targetTexture = m_EnvironmentDepthTexture;
		m_Vision.Render();

		RenderTexture.active = m_EnvironmentDepthTexture;
		m_EnvironmentDepthMap.ReadPixels(new Rect(0, 0, m_EnvironmentDepthTexture.width, m_EnvironmentDepthTexture.height), 0, 0);
		m_EnvironmentDepthMap.Apply();
		GL.Clear(true, true, Color.clear); // clear the texture to avoid artifacts
		RenderTexture.active = null;
		m_Vision.targetTexture = null;
	}

	private void RenderTargets()
	{
		m_Vision.cullingMask = m_TargetsLayer;
		m_Vision.targetTexture = m_TargetsDepthTexture;
		m_Vision.Render();

		RenderTexture.active = m_TargetsDepthTexture;
		m_TargetsDepthMap.ReadPixels(new Rect(0, 0, m_TargetsDepthTexture.width, m_TargetsDepthTexture.height), 0, 0);
		m_TargetsDepthMap.Apply();
		GL.Clear(true, true, Color.clear); // clear the texture to avoid artifacts
		RenderTexture.active = null;
		m_Vision.targetTexture = null;
	}

	private void DetectVision()
	{
		for (int y = 0; y < m_Vision.pixelHeight; ++y)
		{
			for (int x = 0; x < m_Vision.pixelWidth; ++x)
			{
				float envDepth = m_EnvironmentDepthMap.GetPixel(x, y).r;
				float targetDepth = m_TargetsDepthMap.GetPixel(x, y).r;
				if (targetDepth < envDepth) // darker pixel is in front of the lighter pixel
				{
					// calculate the world position of the target ?
					//Vector3 targetPosition = m_Vision.ViewportToWorldPoint(new Vector3((float)x / m_Vision.pixelWidth, (float)y / m_Vision.pixelHeight, targetDepth));
					//Debug.Log("Target detected at: " + targetPosition);

					Debug.Log("Target detected at: " + new Vector3(x, y));
					return;
				}
			}
		}
	}

	/*private void ClearTextures()
	{
		m_EnvironmentDepthTexture = new RenderTexture(Screen.width, Screen.height, 24);
		m_TargetsDepthTexture = new RenderTexture(Screen.width, Screen.height, 24);

		m_EnvironmentDepthMap = new(m_EnvironmentDepthTexture.width, m_EnvironmentDepthTexture.height, TextureFormat.RFloat, false);
		m_TargetsDepthMap = new(m_TargetsDepthTexture.width, m_TargetsDepthTexture.height, TextureFormat.RFloat, false);
	}*/
}
