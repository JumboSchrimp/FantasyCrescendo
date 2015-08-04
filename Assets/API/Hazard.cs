using UnityEngine;
using System.Collections;

namespace Crescendo.API {

	/**
	 * Script used by stage hazards. If a stage hazard collides with something,
	 * it will check whether it is a player (through the use of Hurtbox), and
	 * will cause damage and knockback if it is.
	 */
	public class Hazard : MonoBehaviour {

		void OnTriggerEnter (Collider other) {
			Character player = Hurtbox.GetCharacter (other);
			if (player != null) {
				player.Damage (10f);
				player.Knockback (10f);
				Debug.Log ("hit");
			}
		}
	}
}