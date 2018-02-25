using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics_v3.Module
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class CollisionManager4 : MonoBehaviour
	{
		protected static CollisionManager4 _instance;
		
		public static CollisionManager4 Instance { get { return _instance; } }

		private bool requireUpdate;

		private static List<List<Collider>> vesselsList = new List<List<Collider>>(32);

		private void Awake()
		{
			GameScenes scene = HighLogic.LoadedScene;

	//		if(scene == GameScenes.FLIGHT)
			{
				_instance = this;
			}
	//		else
	//		{
	//			_instance = null;
	//		}
		}

		private void Start()
		{
			GameEvents.OnCollisionIgnoreUpdate.Add(new EventVoid.OnEvent(this.OnCollisionIgnoreUpdate));
		}

		private void OnDestroy()
		{
			GameEvents.OnCollisionIgnoreUpdate.Remove(new EventVoid.OnEvent(this.OnCollisionIgnoreUpdate));

			if(_instance != null && _instance == this)
				_instance = null;
		}

		private void OnCollisionIgnoreUpdate()
		{
			requireUpdate = true;
		}

		private void FixedUpdate()
		{
			UpdateCollisionIgnores();
		}

		private void UpdateCollisionIgnores()
		{
			if(FlightGlobals.ready && requireUpdate)
			{
	//			StartCoroutine(UpdatePartCollisionIgnores());

				requireUpdate = false;
			}
		}

		private List<List<Collider>> GetAllVesselColliders()
		{
			List<List<Collider>> list = CollisionManager4.vesselsList;
			list.Clear();
			bool flag = false;
			int count = FlightGlobals.VesselsLoaded.Count;
			
			while(count-- > 0)
			{
				if(FlightGlobals.VesselsLoaded[count].isEVA)
				{
					flag = true;
					break;
				}
			}
			int i = 0;
			int count2 = FlightGlobals.Vessels.Count;
			while(i < count2)
			{
				Vessel vessel = FlightGlobals.Vessels[i];
				List<Collider> list2 = new List<Collider>();
				int j = 0;
				int count3 = vessel.parts.Count;
				while(j < count3)
				{
					Part part = vessel.parts[j];
					Collider[] componentsInChildren = part.partTransform.GetComponentsInChildren<Collider>(flag);
					if(componentsInChildren != null)
					{
						int num = componentsInChildren.Length;
						for(int k = 0; k < num; k++)
						{
							Collider collider = componentsInChildren[k];
							if((collider.gameObject.activeInHierarchy && collider.enabled) || (flag && (collider.tag == "Ladder" || collider.tag == "Airlock")))
								list2.Add(collider);
						}
					}
					j++;
				}
				list.Add(list2);
				i++;
			}
			return list;
		}

		public IEnumerator UpdatePartCollisionIgnores()
		{
			// wait for next frame so that all other functions did
			// what they want to do with collision settings
for(int iii = 0; iii < 100; iii++)
			yield return new WaitForFixedUpdate();

			// now update the collision settings

			List<List<Collider>> allVesselColliders = this.GetAllVesselColliders();
			int i = 0;
			int count = allVesselColliders.Count;
			while(i < count)
			{
				int j = i;
				int count2 = allVesselColliders.Count;
				while(j < count2)
				{
					List<Collider> list = allVesselColliders[i];
					List<Collider> list2 = allVesselColliders[j];
					bool flag = i == j;
					int k = 0;
					int count3 = list.Count;
					while(k < count3)
					{
						int l = (!flag) ? 0 : (k + 1);
						int count4 = list2.Count;
						while(l < count4)
						{
							Collider collider = list[k];
							Collider collider2 = list2[l];
		//					if(!(collider.attachedRigidbody == collider2.attachedRigidbody))
		//						Physics.IgnoreCollision(collider, collider2, flag);

// FEHLER, schneller Versuch mal...
//if(collider.attachedRigidbody == collider2.attachedRigidbody)
	Physics.IgnoreCollision(collider, collider2, false);

							l++;
						}
						k++;
					}
					j++;
				}
				i++;
			}
			allVesselColliders.Clear();
		}

/*
		public static void IgnoreCollidersOnVessel(Vessel vessel, params Collider[] ignoreColliders)
		{
			for(int i = vessel.parts.Count - 1; i >= 0; i--)
			{
				Collider[] componentsInChildren = vessel.parts[i].partTransform.GetComponentsInChildren<Collider>(false);
				if(componentsInChildren != null)
				{
					for(int j = componentsInChildren.Length - 1; j >= 0; j--)
					{
						Collider collider = componentsInChildren[j];
						if(collider.gameObject.activeInHierarchy && collider.enabled)
						{
							for(int k = ignoreColliders.Length - 1; k >= 0; k--)
								Physics.IgnoreCollision(ignoreColliders[k], collider, true);
						}
					}
				}
			}
		}
 */
	}
}
