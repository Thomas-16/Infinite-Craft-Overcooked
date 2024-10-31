using UnityEngine;

public class ConstantScale : MonoBehaviour
{
	public float baseScale = 0.002f;

	private void LateUpdate()
	{
		if (Camera.main == null) return;
		float scale = Camera.main.orthographicSize * baseScale;
		transform.localScale = new Vector3(scale, scale, scale);
	}
}