﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Model
{
	public enum DLLType
	{
		Model,
		Hotfix,
		Editor,
	}

	public sealed class EventSystem
	{
		private readonly Dictionary<DLLType, Assembly> assemblies = new Dictionary<DLLType, Assembly>();

		private readonly Dictionary<EventIdType, List<object>> allEvents = new Dictionary<EventIdType, List<object>>();

		private readonly UnOrderMultiMap<Type, AAwakeSystem> awakeEvents = new UnOrderMultiMap<Type, AAwakeSystem>();

		private readonly UnOrderMultiMap<Type, AStartSystem> startEvents = new UnOrderMultiMap<Type, AStartSystem>();

		private readonly UnOrderMultiMap<Type, ALoadSystem> loadEvents = new UnOrderMultiMap<Type, ALoadSystem>();

		private readonly UnOrderMultiMap<Type, AUpdateSystem> updateEvents = new UnOrderMultiMap<Type, AUpdateSystem>();

		private readonly UnOrderMultiMap<Type, ALateUpdateSystem> lateUpdateEvents = new UnOrderMultiMap<Type, ALateUpdateSystem>();

		private Queue<Component> updates = new Queue<Component>();
		private Queue<Component> updates2 = new Queue<Component>();

		private readonly Queue<Component> starts = new Queue<Component>();

		private Queue<Component> loaders = new Queue<Component>();
		private Queue<Component> loaders2 = new Queue<Component>();

		private readonly HashSet<Disposer> unique = new HashSet<Disposer>();

		public void Add(DLLType dllType, Assembly assembly)
		{
			this.assemblies[dllType] = assembly;

			this.awakeEvents.Clear();
			this.lateUpdateEvents.Clear();
			this.updateEvents.Clear();
			this.startEvents.Clear();
			this.loadEvents.Clear();
			
			Type[] types = DllHelper.GetMonoTypes();
			foreach (Type type in types)
			{
				object[] attrs = type.GetCustomAttributes(typeof(ObjectSystemAttribute), false);

				if (attrs.Length == 0)
				{
					continue;
				}

				object obj = Activator.CreateInstance(type);

				AAwakeSystem objectSystem = obj as AAwakeSystem;
				if (objectSystem != null)
				{
					this.awakeEvents.Add(objectSystem.Type(), objectSystem);
				}

				AUpdateSystem aUpdateSystem = obj as AUpdateSystem;
				if (aUpdateSystem != null)
				{
					this.updateEvents.Add(aUpdateSystem.Type(), aUpdateSystem);
				}

				ALateUpdateSystem aLateUpdateSystem = obj as ALateUpdateSystem;
				if (aLateUpdateSystem != null)
				{
					this.lateUpdateEvents.Add(aLateUpdateSystem.Type(), aLateUpdateSystem);
				}

				AStartSystem aStartSystem = obj as AStartSystem;
				if (aStartSystem != null)
				{
					this.startEvents.Add(aStartSystem.Type(), aStartSystem);
				}

				ALoadSystem aLoadSystem = obj as ALoadSystem;
				if (aLoadSystem != null)
				{
					this.loadEvents.Add(aLoadSystem.Type(), aLoadSystem);
				}
			}


			allEvents.Clear();
			foreach (Type type in types)
			{
				object[] attrs = type.GetCustomAttributes(typeof(EventAttribute), false);

				foreach (object attr in attrs)
				{
					EventAttribute aEventAttribute = (EventAttribute)attr;
					object obj = Activator.CreateInstance(type);
					if (!this.allEvents.ContainsKey((EventIdType)aEventAttribute.Type))
					{
						this.allEvents.Add((EventIdType)aEventAttribute.Type, new List<object>());
					}
					this.allEvents[(EventIdType)aEventAttribute.Type].Add(obj);
				}
			}

			this.Load();
		}

		public Assembly Get(DLLType dllType)
		{
			return this.assemblies[dllType];
		}

		public Assembly[] GetAll()
		{
			return this.assemblies.Values.ToArray();
		}

		public void Add(Component disposer)
		{
			Type type = disposer.GetType();

			if (this.loadEvents.ContainsKey(type))
			{
				this.loaders.Enqueue(disposer);
			}

			if (this.updateEvents.ContainsKey(type))
			{
				this.updates.Enqueue(disposer);
			}

			if (this.startEvents.ContainsKey(type))
			{
				this.starts.Enqueue(disposer);
			}
		}

		public void Awake(Component disposer)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				aAwakeSystem.Run(disposer);
			}
		}

		public void Awake<P1>(Component disposer, P1 p1)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				aAwakeSystem.Run(disposer, p1);
			}
		}

		public void Awake<P1, P2>(Component disposer, P1 p1, P2 p2)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				aAwakeSystem.Run(disposer, p1, p2);
			}
		}

		public void Awake<P1, P2, P3>(Component disposer, P1 p1, P2 p2, P3 p3)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				aAwakeSystem.Run(disposer, p1, p2, p3);
			}
		}

		public void Load()
		{
			unique.Clear();
			while (this.loaders.Count > 0)
			{
				Component disposer = this.loaders.Dequeue();
				if (disposer.IsDisposed)
				{
					continue;
				}

				if (!this.unique.Add(disposer))
				{
					continue;
				}

				List<ALoadSystem> aLoadSystems = this.loadEvents[disposer.GetType()];
				if (aLoadSystems == null)
				{
					continue;
				}

				this.loaders2.Enqueue(disposer);

				foreach (ALoadSystem aLoadSystem in aLoadSystems)
				{
					try
					{
						aLoadSystem.Run(disposer);
					}
					catch (Exception e)
					{
						Log.Error(e.ToString());
					}
				}
			}

			ObjectHelper.Swap(ref this.loaders, ref this.loaders2);
		}

		private void Start()
		{
			unique.Clear();
			while (this.starts.Count > 0)
			{
				Component disposer = this.starts.Dequeue();

				if (!this.unique.Add(disposer))
				{
					continue;
				}

				List<AStartSystem> aStartSystems = this.startEvents[disposer.GetType()];
				if (aStartSystems == null)
				{
					continue;
				}

				foreach (AStartSystem aStartSystem in aStartSystems)
				{
					try
					{
						aStartSystem.Run(disposer);
					}
					catch (Exception e)
					{
						Log.Error(e.ToString());
					}
				}
			}
		}

		public void Update()
		{
			this.Start();

			this.unique.Clear();
			while (this.updates.Count > 0)
			{
				Component disposer = this.updates.Dequeue();
				if (disposer.IsDisposed)
				{
					continue;
				}

				if (!this.unique.Add(disposer))
				{
					continue;
				}

				List<AUpdateSystem> aUpdateSystems = this.updateEvents[disposer.GetType()];
				if (aUpdateSystems == null)
				{
					continue;
				}

				this.updates2.Enqueue(disposer);

				foreach (AUpdateSystem aUpdateSystem in aUpdateSystems)
				{
					try
					{
						aUpdateSystem.Run(disposer);
					}
					catch (Exception e)
					{
						Log.Error(e.ToString());
					}
				}
			}

			ObjectHelper.Swap(ref this.updates, ref this.updates2);
		}

		public void Run(EventIdType type)
		{
			List<object> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}
			foreach (object obj in iEvents)
			{
				try
				{
					IEvent iEvent = (IEvent)obj;
					iEvent.Run();
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}

		public void Run<A>(EventIdType type, A a)
		{
			List<object> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}

			foreach (object obj in iEvents)
			{
				try
				{
					IEvent<A> iEvent = (IEvent<A>)obj;
					iEvent.Run(a);
				}
				catch (Exception err)
				{
					Log.Error(err.ToString());
				}
			}
		}

		public void Run<A, B>(EventIdType type, A a, B b)
		{
			List<object> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}

			foreach (object obj in iEvents)
			{
				try
				{
					IEvent<A, B> iEvent = (IEvent<A, B>)obj;
					iEvent.Run(a, b);
				}
				catch (Exception err)
				{
					Log.Error(err.ToString());
				}
			}
		}

		public void Run<A, B, C>(EventIdType type, A a, B b, C c)
		{
			List<object> iEvents;
			if (!this.allEvents.TryGetValue(type, out iEvents))
			{
				return;
			}

			foreach (object obj in iEvents)
			{
				try
				{
					IEvent<A, B, C> iEvent = (IEvent<A, B, C>)obj;
					iEvent.Run(a, b, c);
				}
				catch (Exception err)
				{
					Log.Error(err.ToString());
				}
			}
		}
	}
}