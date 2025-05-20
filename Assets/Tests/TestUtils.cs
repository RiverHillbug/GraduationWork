
using UnityEngine;

public static class TestUtils
{
	public static void PlaceAt(Transform child, Transform parent)
	{
		child.SetParent(parent, worldPositionStays: false);
		child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
	}
}
