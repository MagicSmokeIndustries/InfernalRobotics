using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using InfernalRobotics_v3.Module;


namespace InfernalRobotics_v3.Servo
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class CollisionManager4 : MonoBehaviour
	{
		protected static CollisionManager4 _instance = null;
		public static CollisionManager4 Instance { get { return _instance; } }

		private List<ModuleIRServo_v3> registeredServo = new List<ModuleIRServo_v3>();

		private bool requireUpdate = false;

		private void Awake()
		{
			if(!HighLogic.LoadedSceneIsFlight)
				return;

			_instance = this;
		}

		private void Start()
		{
			if(!HighLogic.LoadedSceneIsFlight)
				return;

			GameEvents.OnCollisionIgnoreUpdate.Add(new EventVoid.OnEvent(this.OnCollisionIgnoreUpdate));
		}

		private void OnDestroy()
		{
			GameEvents.OnCollisionIgnoreUpdate.Remove(new EventVoid.OnEvent(this.OnCollisionIgnoreUpdate));

			if(_instance != null && _instance == this)
				_instance = null;
		}

		public void RegisterServo(ModuleIRServo_v3 servo)
		{
			registeredServo.Add(servo);
		}

		public void UnregisterServo(ModuleIRServo_v3 servo)
		{
			registeredServo.Remove(servo);
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
			if(!HighLogic.LoadedSceneIsFlight)
				return;

			if(FlightGlobals.ready && requireUpdate)
			{
				StartCoroutine(UpdatePartCollisionIgnores());

				requireUpdate = false;
			}
		}

		private void FindChildParts(Part parent, List<Part> parts, List<Part> roots)
		{
			for(int i = 0; i < parent.children.Count; i++)
			{
				Part part = parent.children[i];
				ModuleIRServo_v3 servo = part.GetComponent<ModuleIRServo_v3>();
				if((servo != null) && servo.activateCollisions)
					roots.Add(part);
				else
				{
					parts.Add(part);
					FindChildParts(part, parts, roots);
				}
			}
		}

		private List<Collider> GetAllPartGroupColliders(List<Part> parts)
		{
			List<Collider> list = new List<Collider>();
			for(int i = 0; i < parts.Count; i++)
			{
				Part part = parts[i];
				Collider[] componentsInChildren = part.partTransform.GetComponentsInChildren<Collider>(false);
				if(componentsInChildren != null)
				{
					for(int j = 0; j < componentsInChildren.Length; j++)
					{
						Collider collider = componentsInChildren[j];
						if(collider.gameObject.activeInHierarchy && collider.enabled)
							list.Add(collider);
					}
				}
			}
			return list;
		}

		private List<Collider> GetAllPartColliders(Part part)
		{
			List<Collider> list = new List<Collider>();
			Collider[] componentsInChildren = part.partTransform.GetComponentsInChildren<Collider>(false);
			if(componentsInChildren != null)
			{
				for(int j = 0; j < componentsInChildren.Length; j++)
				{
					Collider collider = componentsInChildren[j];
					if(collider.gameObject.activeInHierarchy && collider.enabled)
						list.Add(collider);
				}
			}
			return list;
		}

		public IEnumerator UpdatePartCollisionIgnores()
		{
			// wait for next frame(s) so that all other functions did
			// what they want to do with collision settings
			int wait = 4;
			while(--wait > 0)
				yield return new WaitForFixedUpdate();

			// now update the collision settings
			HashSet<Vessel> allVessels = new HashSet<Vessel>();

			for(int i = 0; i < registeredServo.Count; i++)
				allVessels.Add(registeredServo[i].vessel);

			foreach(Vessel vessel in allVessels)
			{
				// find all part groups
				List<List<Part>> partListList = new List<List<Part>>();

				List<Part> roots = new List<Part>();
				roots.Add(vessel.rootPart);

				while(roots.Count > 0)
				{
					List<Part> parts = new List<Part>();

					parts.Add(roots[0]);

					FindChildParts(roots[0], parts, roots);

					partListList.Add(parts);

					roots.RemoveAt(0);
				}

				// get all colliders of those groups
				List<List<Collider>> colliderListList = new List<List<Collider>>();

				for(int i = 0; i < partListList.Count; i++)
					colliderListList.Add(GetAllPartGroupColliders(partListList[i]));

				// activate collisions between all those groups
				for(int i = 0; i < colliderListList.Count; i++)
				{
					for(int j = i + 1; j < colliderListList.Count; j++)
						SetCollisions(colliderListList[i], colliderListList[j], false);
				}

				// deactivate collisions of servo objects with their parents
				for(int i = 1; i < partListList.Count; i++)
				{
					List<Collider> partCollider = GetAllPartColliders(partListList[i][0]);
					List<Collider> parentCollider = GetAllPartColliders(partListList[i][0].parent);

					SetCollisions(partCollider, parentCollider, true);
				}
			}
		}

		void SetCollisions(List<Collider> a, List<Collider> b, bool ignore)
		{
			for(int i = 0; i < a.Count; i++)
			{
				for(int j = 0; j < b.Count; j++)
				{
					Collider collider = a[i];
					Collider collider2 = b[j];
					if(!(collider.attachedRigidbody == collider2.attachedRigidbody))
						Physics.IgnoreCollision(collider, collider2, ignore);
				}
			}
		}
	}
}
