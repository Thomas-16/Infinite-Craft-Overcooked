using UnityEngine;

public class DeathZone : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		Player player = other.GetComponent<Player>();
		if (player != null)
		{
			player.OnHitDeathZone();
		}

		LLement element = other.GetComponent<LLement>();
		if (element != null)
		{
			Destroy(element);
		}
	}
}