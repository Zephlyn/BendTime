using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace BendTime {
	public class DelayFreeze : MonoBehaviour
	{
		public float timeToFreeze = 0.2f;

		private void Update()
		{
			timeToFreeze -= Time.deltaTime;
			if (timeToFreeze <= 0f)
				Freeze();
		}

		private void Freeze()
		{
			TimeController.Instance.FreezeGameObject(gameObject);
			GameObject.Destroy(this);
		}
	}
}
