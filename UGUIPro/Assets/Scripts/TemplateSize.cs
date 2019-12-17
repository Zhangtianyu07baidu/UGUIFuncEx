using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 复杂的自适应，无法保证取出正确尺寸
/// </summary>
public class TemplateSize : MonoBehaviour
{
	public List<RectTransform> Transforms;

	public float GetMaxHeight()
	{
		RectTransform rectTransform = this.transform as RectTransform;
		float height = rectTransform.rect.height;
		
		if (this.Transforms != null)
		{
			for (int i = 0; i < this.Transforms.Count; i++)
			{
				var item = this.Transforms[i];
				height = Math.Max(height, item.rect.height);
			}
		}

		return height;
	}
}
