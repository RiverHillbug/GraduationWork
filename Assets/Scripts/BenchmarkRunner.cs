
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class BenchmarkRunner : MonoBehaviour
{
	[Serializable]
	private struct Parameters
	{
		public string Name;
		public int AgentsAmount;
		public int TargetsAmount;

		public bool OnlyThis;
	}

	[SerializeField]
	private List<Parameters> m_Parameters = new();

	[SerializeField]
	private float m_SampleDuration = 60.0f;

	[SerializeField]
	private int m_BenchmarksCount = 10;

	private int m_CurrentTestIndex = 0;
	private int m_CurrentBenchmarkIndex = 0;
	private bool m_UseRaycast = false;

	private bool m_OnlyOneTest = false;
	
	private float m_TestTimer = 0.0f;

	private void Start()
	{
		m_UseRaycast = true;
		m_OnlyOneTest = false;

		for (int i = 0; i < m_Parameters.Count; ++i)
		{
			if (m_Parameters[i].OnlyThis)
			{
				m_CurrentTestIndex = i;
				m_OnlyOneTest = true;
			}
		}

		GameManager.Instance.Initialize(m_Parameters[m_CurrentTestIndex].AgentsAmount, m_Parameters[m_CurrentTestIndex].TargetsAmount, m_UseRaycast);

		if (!System.IO.Directory.Exists($"{Application.dataPath}/Benchmarks"))
			AssetDatabase.CreateFolder("Assets", "Benchmarks");

		StartBenchmark();

		Profiler.enableBinaryLog = true;
		Profiler.enabled = true;
	}

	private void OnDestroy()
	{
		Profiler.enabled = false;
		Profiler.enableBinaryLog = false;
		Profiler.logFile = string.Empty;
	}

	private void Update()
	{
		if (Profiler.enabled == false)
			return;

		m_TestTimer -= Time.deltaTime;
		if (m_TestTimer > 0.0f)
			return;

		Debug.Log($"Test {m_Parameters[m_CurrentTestIndex].Name} iteration {m_CurrentBenchmarkIndex + 1} completed.");
		++m_CurrentBenchmarkIndex;

		if (m_CurrentBenchmarkIndex >= m_BenchmarksCount)
		{
			m_CurrentBenchmarkIndex = 0;
			++m_CurrentTestIndex;

			if (m_OnlyOneTest || m_CurrentTestIndex >= m_Parameters.Count)
			{
				Debug.Log($"All tests completed with {(m_UseRaycast ? "raycast" : "depth map")} method.");

				if (m_OnlyOneTest || m_UseRaycast)
				{
					StopTests();
					return;
				}

				m_UseRaycast = true;

				m_CurrentTestIndex = 0;
				m_CurrentBenchmarkIndex = 0;
			}
		}

		StartBenchmark();
	}

	private void StartBenchmark()
	{
		GameManager.Instance.Initialize(m_Parameters[m_CurrentTestIndex].AgentsAmount, m_Parameters[m_CurrentTestIndex].TargetsAmount, m_UseRaycast);

		Debug.Log($"Starting test {m_Parameters[m_CurrentTestIndex].Name}, iteration {m_CurrentBenchmarkIndex + 1} with {(m_UseRaycast ? "raycast" : "depth map")} method.");

		string logPath = $"{(m_UseRaycast ? "Raycast_" : "DepthMap_")}{m_Parameters[m_CurrentTestIndex].Name}";
		if (!System.IO.Directory.Exists($"{Application.dataPath}/Benchmarks/{logPath}"))
			AssetDatabase.CreateFolder("Assets/Benchmarks", logPath);

		// Start recording the data
		Profiler.logFile = $"Assets/Benchmarks/{logPath}/{m_CurrentBenchmarkIndex + 1:00}";

		m_TestTimer = m_SampleDuration;
	}

	private void StopTests()
	{
		Debug.Log($"All tests completed! Quitting application.");

		// Stop writing the log
		Profiler.enabled = false;
		Profiler.enableBinaryLog = false;
		Profiler.logFile = string.Empty;

		EditorApplication.isPlaying = false;
		return;
	}
}
