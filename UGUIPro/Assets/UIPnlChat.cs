using System;
using System.Collections;
using System.Collections.Generic;
using Engine.UI;
using UnityEngine;
using UnityEngine.UI;

public class UIPnlChat : MonoBehaviour
{
	class Item : UIListItem
	{
		private Text label;
		private UIPnlChat chatPnl;

		protected override void OnInit()
		{
			this.chatPnl = this.loopList.Parent.GetComponent<UIPnlChat>();

			GameObject sub = this.itemObj.transform.Find("Content/Text").gameObject;
			this.label = sub.GetComponent<Text>();
			Debug.Log($"OnInitItem: !!!!!!!!!!!!!!!!!!!!!!");
		}

		protected override void OnUpdate(int index, object data)
		{
			this.label.text = this.chatPnl.datas[index];
			/*if (index % 2 == 0)
			{
				this.label.text = DateTime.Now.ToUniversalTime().ToString();
			}
			else
			{
				this.label.text = $"dsfaghksdfkadsfnsadkfhndskafnasdkdsfnsadkfhndskafnasnsadkfhndskafnasdkdsfnsadkfhndskadkfhsadijfhdsfnsadkfhndskafnasdkfhsadijfhdiosafhiadsfhdiosafhiadsfhfnasdkfhsadijfhdiosafhiadsfhisdfhnasidfhnasifhasdiufdsafhds\n{index}";
			}*/
			Debug.Log($"OnUpdateItemByIndex: {index}");
		}
	}

	public VariableLoopList LoopList;
	public Button SendButton;
	public InputField Input;
	private GameVariableLoopList gameList;

	private List<string> datas = new List<string>();

	void Start()
    {
	    for (int i = 0; i < 10; i++)
	    {
		    this.datas.Add("hello world, this is chat content, 默认的聊天内容");
	    }

		this.gameList = new GameVariableLoopList(this.gameObject, this.LoopList).SetItemType(typeof(Item)).SetTemplateIndexFunc(this.GetTemplateIndex);
	    gameList.SetListNum(10, 9);

		this.SendButton.onClick.AddListener(() =>
		{
			if (!string.IsNullOrEmpty(this.Input.text))
			{
				this.datas.Add(this.Input.text);
				this.LoopList.AddOneAtLast();
			}
		});

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

		GUILayout.EndVertical();
	}
}
