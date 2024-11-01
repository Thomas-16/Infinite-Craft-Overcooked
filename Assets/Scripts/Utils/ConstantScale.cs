using UnityEngine;

public class ConstantScale : MonoBehaviour
{
	public float baseScale = 0.002f;

	private void LateUpdate()
	{
		if (Camera.main == null) return;

		// Calculate desired world scale
		float targetWorldScale = Camera.main.orthographicSize * baseScale;

		// Get parent's world scale (if no parent, this will be Vector3.one)
		Vector3 parentWorldScale = Vector3.one;
		if (transform.parent != null)
		{
			parentWorldScale = transform.parent.lossyScale;
		}

		// Calculate required local scale to achieve desired world scale
		Vector3 newLocalScale = new Vector3(
			parentWorldScale.x != 0 ? targetWorldScale / parentWorldScale.x : targetWorldScale,
			parentWorldScale.y != 0 ? targetWorldScale / parentWorldScale.y : targetWorldScale,
			parentWorldScale.z != 0 ? targetWorldScale / parentWorldScale.z : targetWorldScale
		);

		transform.localScale = newLocalScale;
	}

#if UNITY_EDITOR
	// Optional: Add validation to warn about potential division by zero
	private void OnValidate()
	{
		if (transform.parent != null)
		{
			Vector3 parentScale = transform.parent.lossyScale;
			if (parentScale.x == 0 || parentScale.y == 0 || parentScale.z == 0)
			{
				Debug.LogWarning($"ConstantScale on {gameObject.name}: Parent hierarchy contains zero scale which may cause issues", this);
			}
		}
	}
#endif
}