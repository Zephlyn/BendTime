using UnityEngine;

namespace BendTime {

	/// <summary>
	/// Carries velocity over from frozen creatures
	/// </summary>

	internal class StoredPhysicsData : MonoBehaviour {
		private Vector3 angularVelocity;
		internal Vector3 velocity;

		public void StoreDataFromRigidBody(Rigidbody rb) {
			angularVelocity = rb.angularVelocity;
			velocity = rb.velocity;
		}

		public void SetRigidbodyFromStoredData(Rigidbody rb) {
			rb.angularVelocity = angularVelocity;
			rb.velocity = velocity;
		}

	}
}