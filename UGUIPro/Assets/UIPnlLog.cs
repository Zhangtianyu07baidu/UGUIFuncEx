using System;
using System.Collections;
using System.Collections.Generic;
using Engine.UI;
using UnityEngine;
using UnityEngine.UI;

public class UIPnlLog : MonoBehaviour
{
	class Item : UIListItem
	{
		private Text label;

		protected override void OnInit()
		{
			GameObject sub = this.itemObj.transform.Find("Text").gameObject;
			this.label = sub.GetComponent<Text>();
			Debug.Log($"OnInitItem: !!!!!!!!!!!!!!!!!!!!!!");
		}

		protected override void OnUpdate(int index, object data)
		{
			if (index % 2 == 0)
			{
				this.label.text = DateTime.Now.ToUniversalTime().ToString();
			}
			else
			{
				this.label.text = $"dsfaghksdfkadsfnsadkfhndskafnasdkdsfnsadkfhndskafnasnsadkfhndskafnasdkdsfnsadkfhndskadkfhsadijfhdsfnsadkfhndskafnasdkfhsadijfhdiosafhiadsfhdiosafhiadsfhfnasdkfhsadijfhdiosafhiadsfhisdfhnasidfhnasifhasdiufdsafhds\n{index}";
			}
			Debug.Log($"OnUpdateItemByIndex: {index}");
		}
	}

	public VariableLoopList LoopList;
	private GameVariableLoopList gameList;

	void Start()
    {
		this.gameList = new GameVariableLoopList(this.gameObject, this.LoopList).SetItemType(typeof(Item)).SetTemplateIndexFunc(this.GetTemplateIndex);
	    gameList.SetListNum(0);
		//this.gameList.SetListNum(100, 99);

	}

	private int GetTemplateIndex(int index)
	{
		return index % 2;
	}

	void OnGUI()
	{
		GUILayout.BeginVertical();
		if (GUILayout.Button("Click Test"))
		{
			this.LoopList.RefreshList();
		}

		if (GUILayout.Button("Add To Last"))
		{
			this.LoopList.AddOneAtLast();
		}

		GUILayout.EndVertical();
	}
}