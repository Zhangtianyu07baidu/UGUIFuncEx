using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Vector2 = UnityEngine.Vector2;

namespace Engine.UI
{
	/// <summary>
	/// 可变长循环列表
	/// </summary>
	public class VariableLoopList : MonoBehaviour
	{
		private float DEFAULT_SIZE_F = 0;

		private ScrollRect scrollRect;
		private Vector2 viewSize;

		public List<RectTransform> Templates;
		public RectTransform ContentRect => this.scrollRect.content;
		public bool IsHorizontal => this.scrollRect.horizontal;

		/// <summary>
		/// 数据总数
		/// </summary>
		private int num;

		/// <summary>
		/// 当前正在显示的UI链表
		/// </summary>
		private readonly LinkedList<RectTransform> usingItemList = new LinkedList<RectTransform>();

		/// <summary>
		/// 实例化的item对应的template索引关系
		/// </summary>
		private readonly Dictionary<RectTransform, int> poolIndexDic = new Dictionary<RectTransform, int>();

		/// <summary>
		/// template索引对应的pool关系
		/// </summary>
		private readonly Dictionary<int, Stack<RectTransform>> pools = new Dictionary<int, Stack<RectTransform>>();

		private readonly Dictionary<int, float> sizeDic = new Dictionary<int, float>();

		/// <summary>
		/// 当前显示的数据上边界索引
		/// </summary>
		private int minIndex;
		/// <summary>
		/// 当前显示的数据下边界索引
		/// </summary>
		private int maxIndex;

		private float contentSize;
		private float lastScrollY;
		private float lastScrollX;

		private bool isCreatingFirst;
		private bool isCreatingLast;

		/// <summary>
		/// 是否在末尾添加一个
		/// </summary>
		private bool isAddOne;
		/// <summary>
		/// 当在末尾添加一个时，保存当前末尾的位置
		/// </summary>
		private float lastItemPos;

		public event Action<GameObject> OnInitItemEvent;
		public event Action<GameObject, int> OnUpdateItemEvent;
		public Func<int, int> TemplateIndexFunc;

		void Awake()
		{
			this.scrollRect = this.GetComponent<ScrollRect>();
			this.ContentRect.pivot = new Vector2(0, 1);
			for (int i = 0; i < this.Templates.Count; i++)
			{
				RectTransform template = this.Templates[i];
				template.gameObject.SetActive(true);
				if(this.IsHorizontal)
					template.pivot = new Vector2(0f, template.pivot.y);
				else
					template.pivot = new Vector2(template.pivot.x, 1f);
			}
		}

		/// <summary>
		/// 默认尺寸给个较大的值，效果会好一些
		/// </summary>
		private void CalculateDefaultSize()
		{
			Debug.Assert(this.Templates.Count > 0, "this.Templates.Count > 0");
			float size = 0f;
			for (int i = 0; i < this.Templates.Count; i++)
			{
				RectTransform item = this.Templates[i];
				size += this.GetTemplateSize(item);
				item.gameObject.SetActive(false);
			}
			this.DEFAULT_SIZE_F = size / this.Templates.Count * 5;
			Debug.Log("DEFAULT_SIZE_F: " + this.DEFAULT_SIZE_F);
		}

		public void SetDataList(int num, int toIndex = 0)
		{
			this.isAddOne = false;
			this.num = num;
			this.StopAllCoroutines();
			this.Reset();
			this.StartCoroutine(this.SetListCoroutine(toIndex));
		}

		public void RefreshList()
		{
			int index = Math.Max(this.minIndex + 1, 0);
			index = Math.Min(index, this.num - 1);
			this.SetDataList(this.num, index);
		}

		public void AddOneAtLast()
		{
			this.StopAllCoroutines();
			this.StartCoroutine(this.AddOneLastCoroutine());
		}

		private IEnumerator AddOneLastCoroutine()
		{
			this.lastItemPos = 0;
			if (this.num > 0)
			{
				if (!this.sizeDic.ContainsKey(this.num - 1))
				{
					this.isAddOne = false;
					this.Reset();
					yield return this.StartCoroutine(this.SetListCoroutine(this.num - 1));
				}

				if (this.sizeDic.TryGetValue(this.num - 1, out float lastSize))
				{
					if (this.IsHorizontal)
					{
						this.lastItemPos = this.contentSize - lastSize;
					}
					else
					{
						this.lastItemPos = lastSize - this.contentSize;
					}
				}
				else
				{
					Debug.LogError($"Can't get size by {this.num - 1} in AddOneLastCoroutine");
				}
			}
			this.isAddOne = true;
			this.num++;
			this.Reset(false);
			this.contentSize += DEFAULT_SIZE_F;
			this.StartCoroutine(this.SetListCoroutine(this.num - 1, false));
		}

		private void Reset(bool resetSize = true)
		{
			this.isCreatingFirst = false;
			this.isCreatingLast = false;
			this.scrollRect.onValueChanged.RemoveAllListeners();
			this.usingItemList.Clear();
			if (resetSize)
			{
				this.sizeDic.Clear();
			}

			foreach (var pair in this.pools)
			{
				pair.Value.Clear();
			}

			foreach (var pair in this.poolIndexDic)
			{
				this.pools[pair.Value].Push(pair.Key);
			}

			this.lastScrollX = 0;
			this.lastScrollY = 1;
		}

		private void PushItemToPool(RectTransform item)
		{
			int tIndex = this.poolIndexDic[item];
			this.PushItemToPool(tIndex, item);
		}

		private void PushItemToPool(int tIndex, RectTransform item)
		{
			//item.name = "pool" + tIndex;
			if (this.pools.TryGetValue(tIndex, out var pool))
			{
				pool.Push(item);
			}
			else
			{
				Debug.LogError("Can't find pool by " + tIndex);
			}
		}

		private RectTransform PopItemFromPool(int index)
		{
			int tIndex = this.GetTemplateIndex(index);
			if (!this.pools.TryGetValue(tIndex, out Stack<RectTransform> pool))
			{
				pool = new Stack<RectTransform>();
				this.pools.Add(tIndex, pool);
			}

			RectTransform obj = null;
			if (pool.Count > 0)
			{
				obj = pool.Pop();
			}
			else
			{
				RectTransform template = this.Templates[tIndex];
				obj = Object.Instantiate(template, this.ContentRect);
				this.OnInitItem(obj);
				this.poolIndexDic.Add(obj, tIndex);
			}
			obj.gameObject.SetActive(true);
			//obj.name = "temp";
			if (pool.Count <= 0)
			{
				this.RecycleFirst();
				this.RecycleLast();
			}
			return obj;
		}

		private IEnumerator SetListCoroutine(int toIndex = 0, bool resetSize = true)
		{
			this.scrollRect.inertia = false;
			yield return null;
			if (this.DEFAULT_SIZE_F - 0f < 1e-6f) // == 0
			{
				this.CalculateDefaultSize();
			}
			if (resetSize)
			{
				//初始content size先给个预估值
				this.contentSize = DEFAULT_SIZE_F * this.num;
			}
			this.ResizeContent();
			this.viewSize = this.scrollRect.viewport.rect.size;
			if (this.IsHorizontal)
			{
				if (this.contentSize > this.viewSize.x)
				{
					float value = Math.Min(this.contentSize - this.viewSize.x, toIndex * DEFAULT_SIZE_F);
					this.ContentRect.anchoredPosition = new Vector2(-value, this.ContentRect.anchoredPosition.y);
				}
				else
				{
					//内容区域太小
					this.ContentRect.anchoredPosition = new Vector2(0, this.ContentRect.anchoredPosition.y);
				}

				this.minIndex = toIndex - 1;
				this.maxIndex = toIndex;
				yield return this.CreateItemAtLast();

				float totalSize = 0f;
				for (int i = toIndex; i < this.num; i++)
				{
					if (!this.sizeDic.TryGetValue(i, out float size))
					{
						size = DEFAULT_SIZE_F;
					}
					totalSize += size;
					if (totalSize >= this.viewSize.x)
						break;
				}
				if (totalSize < this.viewSize.x)
				{
					//toIndex后面的尺寸填充不满显示区域
					float value = Math.Max(0, this.contentSize - this.viewSize.x);
					this.ContentRect.anchoredPosition = new Vector2(-value, this.ContentRect.anchoredPosition.y);
					yield return this.CreateItemAtFirst();
					value = Math.Max(0, this.contentSize - this.viewSize.x);
					this.ContentRect.anchoredPosition = new Vector2(-value, this.ContentRect.anchoredPosition.y);
				}
				else
				{
					//有一种情况是：contentSize - viewSize < toIndex * DEFAULT_SIZE_F, content的下边界要与scroll下边界对齐
					this.ContentRect.anchoredPosition = new Vector2(-toIndex * DEFAULT_SIZE_F, this.ContentRect.anchoredPosition.y);
				}
			}
			else
			{
				if (this.contentSize > this.viewSize.y)
				{
					float value = Math.Min(this.contentSize - this.viewSize.y, toIndex * DEFAULT_SIZE_F);
					this.ContentRect.anchoredPosition = new Vector2(this.ContentRect.anchoredPosition.x, value);
				}
				else
				{
					//内容区域太小
					this.ContentRect.anchoredPosition = new Vector2(this.ContentRect.anchoredPosition.x, 0);
				}

				this.minIndex = toIndex - 1;
				this.maxIndex = toIndex;
				yield return this.CreateItemAtLast();

				float totalSize = 0f;
				for (int i = toIndex; i < this.num; i++)
				{
					if (!this.sizeDic.TryGetValue(i, out float size))
					{
						size = DEFAULT_SIZE_F;
					}
					totalSize += size;
					if (totalSize >= this.viewSize.y)
						break;
				}
				if (totalSize < this.viewSize.y)
				{
					//toIndex后面的尺寸填充不满显示区域
					float value = Math.Max(0, this.contentSize - this.viewSize.y);
					this.ContentRect.anchoredPosition = new Vector2(this.ContentRect.anchoredPosition.x, value);
					yield return this.CreateItemAtFirst();
					value = Math.Max(0, this.contentSize - this.viewSize.y);
					this.ContentRect.anchoredPosition = new Vector2(this.ContentRect.anchoredPosition.x, value);
				}
				else
				{
					//有一种情况是：contentSize - viewSize < toIndex * DEFAULT_SIZE_F, content的下边界要与scroll下边界对齐
					this.ContentRect.anchoredPosition = new Vector2(this.ContentRect.anchoredPosition.x, toIndex * DEFAULT_SIZE_F);
				}
			}
			
			this.scrollRect.onValueChanged.AddListener(this.OnScrollValueChanged);
			this.scrollRect.inertia = true;
			//隐藏池子里没用到的
			foreach (var pair in this.pools)
			{
				foreach (var item in pair.Value)
				{
					item.gameObject.SetActive(false);
				}
			}
		}

		private void ResizeContent()
		{
			if (this.IsHorizontal)
			{
				this.ContentRect.sizeDelta = new Vector2(this.contentSize, this.ContentRect.sizeDelta.y);
				//item尺寸改变，导致content缩小，此时计算超出边界不准确
				if (-this.ContentRect.anchoredPosition.x + this.viewSize.x > this.contentSize)
				{
					float value = Math.Max(0, this.contentSize - this.viewSize.x);
					this.ContentRect.anchoredPosition = new Vector2(-value, this.ContentRect.anchoredPosition.y);
				}
			}
			else
			{
				this.ContentRect.sizeDelta = new Vector2(this.ContentRect.sizeDelta.x, this.contentSize);
				//item尺寸改变，导致content缩小，此时计算超出边界不准确
				if (this.ContentRect.anchoredPosition.y + this.viewSize.y > this.contentSize)
				{
					float value = Math.Max(0, this.contentSize - this.viewSize.y);
					this.ContentRect.anchoredPosition = new Vector2(this.ContentRect.anchoredPosition.x, value);
				}
			}
		}

		/// <summary>
		/// 是否已经超出显示区域上边界
		/// </summary>
		private bool IsOutofViewMax(RectTransform transform)
		{
			if (this.IsHorizontal)
			{
				float contentX = this.ContentRect.anchoredPosition.x;
				float targetRightX = transform.anchoredPosition.x + this.GetTemplateSize(transform);
				return targetRightX + contentX < 0;
			}
			else
			{
				float contentY = this.ContentRect.anchoredPosition.y;
				float targetBottomY = transform.anchoredPosition.y - this.GetTemplateSize(transform);
				return -targetBottomY - contentY < 0;
			}
		}

		private bool IsOutofViewMin(RectTransform transform)
		{
			if (this.IsHorizontal)
			{
				float contentX = this.ContentRect.anchoredPosition.x;
				float targetLeftX = transform.anchoredPosition.x;
				return targetLeftX + contentX > this.viewSize.x;
			}
			else
			{
				float contentY = this.ContentRect.anchoredPosition.y;
				float targetTopY = transform.anchoredPosition.y;
				return -targetTopY - contentY > this.viewSize.y;
			}
		}

		/// <summary>
		/// 能否接触到显示区域底部
		/// </summary>
		private bool CanTouchViewMin(RectTransform transform)
		{
			if (this.IsHorizontal)
			{
				float contentX = this.ContentRect.anchoredPosition.x;
				float targetRightX = transform.anchoredPosition.x + this.GetTemplateSize(transform);
				return targetRightX + contentX > this.viewSize.x;
			}
			else
			{
				float contentY = this.ContentRect.anchoredPosition.y;
				float targetBottomY = transform.anchoredPosition.y - this.GetTemplateSize(transform);
				return -targetBottomY - contentY > this.viewSize.y;
			}
		}

		private bool CanTouchViewMax(RectTransform transform)
		{
			if (this.IsHorizontal)
			{
				float contentX = this.ContentRect.anchoredPosition.x;
				float targetLeftX = transform.anchoredPosition.x;
				return targetLeftX + contentX < 0;
			}
			else
			{
				float contentY = this.ContentRect.anchoredPosition.y;
				float targetTopY = transform.anchoredPosition.y;
				return -targetTopY - contentY < 0;
			}
		}

		private void OnScrollValueChanged(Vector2 val)
		{
			if (this.IsHorizontal)
			{
				if(val.x < 0 || val.x > 1)
					return;

				if (val.x > this.lastScrollX)
				{
					//向右滑
					this.CheckLast();
				}
				else
				{
					//向左滑
					this.CheckFirst();
				}
				this.lastScrollX = val.x;
			}
			else
			{
				if (val.y < 0 || val.y > 1)
					return;

				if (val.y - this.lastScrollY > 0f)
				{
					//向上翻
					this.CheckFirst();
				}
				else
				{
					//向下翻
					this.CheckLast();
				}
				this.lastScrollY = val.y;
			}
		}

		private void CheckFirst()
		{
			if (this.usingItemList.Count > 0 && this.CanTouchViewMax(this.usingItemList.First.Value))
				this.RecycleFirst();

			if (this.minIndex < 0)
			{
				return;
			}

			if (!this.isCreatingFirst)
			{
				this.isCreatingFirst = true;
				this.StartCoroutine(this.CreateItemAtFirst());
			}
		}

		private void RecycleFirst()
		{
			if(this.usingItemList.Count <= 0)
				return;

			while (this.usingItemList.Last.Previous != null)
			{
				RectTransform trans = this.usingItemList.Last.Value;
				if (this.IsOutofViewMin(trans))
				{
					trans.gameObject.SetActive(false);
					this.usingItemList.RemoveLast();
					this.PushItemToPool(trans);
					this.maxIndex--;
				}
				else
				{
					break;
				}
			}
		}

		private void CheckLast()
		{
			if (this.usingItemList.Count > 0 && this.CanTouchViewMin(this.usingItemList.Last.Value))
				this.RecycleLast();

			if (this.maxIndex >= this.num)
			{
				return;
			}

			if (!this.isCreatingLast)
			{
				this.isCreatingLast = true;
				this.StartCoroutine(this.CreateItemAtLast());
			}
		}

		private void RecycleLast()
		{
			if (this.usingItemList.Count <= 0)
				return;

			while (this.usingItemList.First.Next != null)
			{
				RectTransform trans = this.usingItemList.First.Value;
				if (this.IsOutofViewMax(trans))
				{
					trans.gameObject.SetActive(false);
					this.usingItemList.RemoveFirst();
					this.PushItemToPool(trans);
					this.minIndex++;
				}
				else
				{
					break;
				}
			}
		}

		private IEnumerator CreateItemAtLast()
		{
			while (this.maxIndex < this.num)
			{
				if (this.usingItemList.Count > 0)
				{
					if (this.CanTouchViewMin(this.usingItemList.Last.Value))
					{
						this.isCreatingLast = false;
						yield break;
					}
				}
				RectTransform transform = this.PopItemFromPool(this.maxIndex);
				this.OnUpdateItemByIndex(transform, this.maxIndex);
				if (!this.sizeDic.TryGetValue(this.maxIndex, out float size))
				{
					yield return null;
					size = this.GetTemplateSize(transform);
					this.contentSize -= DEFAULT_SIZE_F;
					this.contentSize += size;
					this.sizeDic[this.maxIndex] = size;
					this.ResizeContent();
				}

				if (this.IsHorizontal)
				{
					float anchoredPosY = transform.anchoredPosition.y;
					float anchoredPosX = 0f;
					if (this.usingItemList.Count > 0)
					{
						anchoredPosX = this.usingItemList.Last.Value.anchoredPosition.x;
					}
					else
					{
						if (this.isAddOne)
							anchoredPosX = this.lastItemPos;
						else
							anchoredPosX = this.maxIndex * DEFAULT_SIZE_F;
					}
					if (!this.sizeDic.TryGetValue(this.maxIndex - 1, out float lastSize))
					{
						lastSize = 0;
					}
					transform.anchoredPosition = new Vector2(anchoredPosX + lastSize, anchoredPosY);
				}
				else
				{
					float anchoredPosX = transform.anchoredPosition.x;
					float anchoredPosY = 0f;
					if (this.usingItemList.Count > 0)
					{
						anchoredPosY = this.usingItemList.Last.Value.anchoredPosition.y;
					}
					else
					{
						if (this.isAddOne)
							anchoredPosY = this.lastItemPos;
						else
							anchoredPosY = -this.maxIndex * DEFAULT_SIZE_F;
					}
					if (!this.sizeDic.TryGetValue(this.maxIndex - 1, out float lastSize))
					{
						lastSize = 0;
					}
					transform.anchoredPosition = new Vector2(anchoredPosX, anchoredPosY - lastSize);
				}
				this.usingItemList.AddLast(transform);
				//transform.name = "using";
				this.maxIndex++;
			}

			this.isCreatingLast = false;
		}

		private IEnumerator CreateItemAtFirst()
		{
			while (this.minIndex >= 0)
			{
				if (this.usingItemList.Count > 0)
				{
					RectTransform firstTrans = this.usingItemList.First.Value;
					if (this.CanTouchViewMax(firstTrans))
					{
						this.isCreatingFirst = false;
						yield break;
					}
				}

				RectTransform transform = this.PopItemFromPool(this.minIndex);
				this.OnUpdateItemByIndex(transform, this.minIndex);
				float delta = 0f;
				if (!this.sizeDic.TryGetValue(this.minIndex, out float size))
				{
					yield return null;
					size = this.GetTemplateSize(transform);
					delta = size - DEFAULT_SIZE_F;
					this.contentSize += delta;
					this.sizeDic[this.minIndex] = size;
					this.ResizeContent();
				}
				//重新调整位置
				if (this.IsHorizontal)
				{
					LinkedListNode<RectTransform> head = this.usingItemList.First;
					while (head != null)
					{
						float newX = head.Value.anchoredPosition.x + delta;
						head.Value.anchoredPosition = new Vector2(newX, head.Value.anchoredPosition.y);
						head = head.Next;
					}

					float anchoredPosY = transform.anchoredPosition.y;
					float anchoredPosX = 0f;
					if (this.usingItemList.Count > 0)
					{
						anchoredPosX = this.usingItemList.First.Value.anchoredPosition.x;
					}
					transform.anchoredPosition = new Vector2(anchoredPosX - size, anchoredPosY);
				}
				else
				{
					LinkedListNode<RectTransform> head = this.usingItemList.First;
					while (head != null)
					{
						float newY = head.Value.anchoredPosition.y - delta;
						head.Value.anchoredPosition = new Vector2(head.Value.anchoredPosition.x, newY);
						head = head.Next;
					}

					float anchoredPosX = transform.anchoredPosition.x;
					float anchoredPosY = 0f;
					if (this.usingItemList.Count > 0)
					{
						anchoredPosY = this.usingItemList.First.Value.anchoredPosition.y;
					}
					transform.anchoredPosition = new Vector2(anchoredPosX, anchoredPosY + size);
				}
				
				this.usingItemList.AddFirst(transform);
				//transform.name = "using";
				this.minIndex--;
			}

			this.isCreatingFirst = false;
		}

		private int GetTemplateIndex(int index)
		{
			int templateIndex = this.TemplateIndexFunc?.Invoke(index) ?? 0;
			if (templateIndex >= this.Templates.Count || templateIndex < 0)
			{
				Debug.LogError("TemplateIndex is not valid");
			}

			return templateIndex;
		}

		private void OnInitItem(RectTransform transform)
		{
			this.OnInitItemEvent?.Invoke(transform.gameObject);
		}

		private void OnUpdateItemByIndex(RectTransform transform, int index)
		{
			this.OnUpdateItemEvent?.Invoke(transform.gameObject, index);
		}

		private float GetTemplateSize(RectTransform trans)
		{
			TemplateSize sizer = trans.GetComponent<TemplateSize>();
			if (this.IsHorizontal)
			{
				return sizer?.GetMaxWidth() ?? trans.rect.width;
			}
			else
			{
				return sizer?.GetMaxHeight() ?? trans.rect.height;
			}
		}
	}
}
