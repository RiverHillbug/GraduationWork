
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Test_DepthMapVision
{
	[OneTimeSetUp]
	public void LoadScene()
	{
		SceneManager.LoadScene("Gym_Tests");
	}

	[UnityTest]
	public IEnumerator Test_UnobstructedVision()
	{
		VisionAgent agent = GameObject.Find("Agent").GetComponent<VisionAgent>();
		Transform target = GameObject.Find("Target").transform;
		Transform testRoot = GameObject.Find("Test_Unobstructed").transform;

		Transform agentPosition = testRoot.Find("Positions/AgentPosition");
		Transform targetPositionOutOfView = testRoot.Find("Positions/TargetPosition_OutOfView");
		Transform targetPositionInView = testRoot.Find("Positions/TargetPosition_InView");

		agent.SetUseRaycastDetection(false);

		TestUtils.PlaceAt(agent.transform, agentPosition);
		TestUtils.PlaceAt(target, targetPositionOutOfView);

		yield return null;
		Assert.False(agent.DetectVisionImmediate(), "Target was detected out of view");

		TestUtils.PlaceAt(target, targetPositionInView);

		yield return null;
		Assert.True(agent.DetectVisionImmediate(), "Target was not detected when in view");

		TestUtils.PlaceAt(target, targetPositionOutOfView);

		yield return null;
		Assert.False(agent.DetectVisionImmediate(), "Target was still detected after being moved out of view");
	}

	[UnityTest]
	public IEnumerator Test_ObstructedVision()
	{
		VisionAgent agent = GameObject.Find("Agent").GetComponent<VisionAgent>();
		Transform target = GameObject.Find("Target").transform;
		Transform testRoot = GameObject.Find("Test_Obstructed").transform;

		Transform agentPosition = testRoot.Find("Positions/AgentPosition");
		Transform targetPositionOutOfView = testRoot.Find("Positions/TargetPosition_OutOfView");
		Transform targetPositionInView = testRoot.Find("Positions/TargetPosition_InView");
		Transform targetPositionObstructed = testRoot.Find("Positions/TargetPosition_Obstructed");
		Transform targetPositionInFrontOfObstruction = testRoot.Find("Positions/TargetPosition_InFrontOfObstruction");

		agent.SetUseRaycastDetection(false);

		TestUtils.PlaceAt(agent.transform, agentPosition);
		TestUtils.PlaceAt(target, targetPositionOutOfView);

		yield return null;
		Assert.False(agent.DetectVisionImmediate(), "Target was detected out of view");

		TestUtils.PlaceAt(target, targetPositionInView);

		yield return null;
		Assert.True(agent.DetectVisionImmediate(), "Target was not detected when in view");

		TestUtils.PlaceAt(target, targetPositionObstructed);

		yield return null;
		Assert.False(agent.DetectVisionImmediate(), "Target was detected when obstructed");

		TestUtils.PlaceAt(target, targetPositionInFrontOfObstruction);

		yield return null;
		Assert.True(agent.DetectVisionImmediate(), "Target was not detected despite being in front of obstruction");
	}

	[UnityTest]
	public IEnumerator Test_PartiallyObstructedVision()
	{
		VisionAgent agent = GameObject.Find("Agent").GetComponent<VisionAgent>();
		Transform target = GameObject.Find("Target").transform;
		Transform testRoot = GameObject.Find("Test_PartiallyObstructed").transform;

		Transform agentPosition = testRoot.Find("Positions/AgentPosition");
		Transform targetPositionOutOfView = testRoot.Find("Positions/TargetPosition_OutOfView");
		Transform targetPositionObstructed = testRoot.Find("Positions/TargetPosition_Obstructed");

		agent.SetUseRaycastDetection(false);

		TestUtils.PlaceAt(agent.transform, agentPosition);
		TestUtils.PlaceAt(target, targetPositionOutOfView);

		yield return null;
		Assert.False(agent.DetectVisionImmediate(), "Target was detected out of view");

		TestUtils.PlaceAt(target, targetPositionObstructed);

		yield return null;
		Assert.True(agent.DetectVisionImmediate(), "Target was not detected when partially obstructed");
	}
}
