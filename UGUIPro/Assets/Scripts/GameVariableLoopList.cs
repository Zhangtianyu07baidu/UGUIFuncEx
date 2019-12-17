

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Engine.UI
{
	public abstract class UIListItem
	{
		protected GameObject itemObj;
		protected GameVariableLoopList loopList;

		public void Init(GameVariableLoopList loopList, GameObject obj)
		{
			this.loopList = loopList;
			this.itemObj = obj;
			this.OnInit();
		}

		public void Update(int index)
		{
			this.OnUpdate(index, null);
		}

		protected virtual void OnInit()
		{

		}

		protected virtual void OnUpdate(int index, object data)
		{

		}
	}

	public class GameVariableLoopList
	{
		public GameObject Parent;

		private readonly VariableLoopList list;
		private Type itemType;
		private Func<int, int> GetTemplateIndexFunc;

		private readonly Dictionary<GameObject, UIListItem> listItems = new Dictionary<GameObject, UIListItem>();

		public GameVariableLoopList(GameObject parent, VariableLoopList list)
		{
			this.Parent = parent;
			this.list = list;
			this.list.OnInitItemEvent += List_OnInitItemEvent;
			this.list.OnUpdateItemEvent += List_OnUpdateItemEvent;
			this.list.TemplateIndexFunc = this.GetTemplateIndex;
		}

		private int GetTemplateIndex(int index)
		{
			return this.GetTemplateIndexFunc?.Invoke(index) ?? 0;
		}

		private void List_OnUpdateItemEvent(UnityEngine.GameObject obj, int index)
		{
			UIListItem item = this.listItems[obj];
			item.Update(index);
		}

		private void List_OnInitItemEvent(UnityEngine.GameObject obj)
		{
			var listItem = Activator.CreateInstance(this.itemType) as UIListItem;
			this.listItems[obj] = listItem;
			listItem.Init(this, obj);
		}

		public GameVariableLoopList SetItemType(Type type)
		{
			this.itemType = type;
			return this;
		}

		public GameVariableLoopList SetTemplateIndexFunc(Func<int, int> func)
		{
			this.GetTemplateIndexFunc = func;
			return this;
		}

		public void SetListNum(int num, int toIndex = 0)
		{
			this.list.SetDataList(num, toIndex);
		}
	}
}
