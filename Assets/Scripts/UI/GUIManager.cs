﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public static class GUIManager {
    private static Dictionary<string, KeyValuePair<GameObject, IView>> m_UIViewDic 
        = new Dictionary<string, KeyValuePair<GameObject, IView>>();

    private static GameObject InstantiatePanel(string prefabId) {
        GameObject prefab = ResourcesManager.Instance.GetUIPrefab(prefabId);
        if (prefab == null) {
            Debug.LogError("prefab is null ," + prefabId);
            return null;
        }

        GameObject UIPrefab = GameObject.Instantiate(prefab) as GameObject;
        UIPrefab.name = prefabId;

        Camera uiCamera = GameObject.FindWithTag("UICamera").GetComponent<Camera>();
        if (uiCamera == null) {
            Debug.LogError("UICamera is not find");
            return null;
        }

        UIPrefab.transform.parent = uiCamera.transform;
        UIPrefab.transform.localScale = new Vector3(1, 1, 1);
        UIPrefab.transform.localPosition = new Vector3(prefab.transform.localPosition.x,
                                                    prefab.transform.localPosition.y,
                                                    Mathf.Clamp(prefab.transform.localPosition.z, -2f, 2f));


        return UIPrefab;
    }

	public static void ShowView(string name){
		//TODO
        IView view = null;
        GameObject panel = null;

        KeyValuePair<GameObject, IView> found;
        if (!m_UIViewDic.TryGetValue(name, out found))
        {
            view = Assembly.GetExecutingAssembly().CreateInstance(name) as IView;
            panel = InstantiatePanel(name);

            if (view == null && panel == null)
            {
                return;
            }
            UIPanel[] childPanels = panel.GetComponentsInChildren<UIPanel>(true);
            foreach (UIPanel childPanel in childPanels)
            {
                //childPanel.depth += (int)view.UILayer;
            }
            m_UIViewDic.Add(name, new KeyValuePair<GameObject, IView>(panel, view));

            view.Start();
        }
        else {
            view = found.Value;
            panel = found.Key;
        }

        if (view == null || panel == null) {
            return;
        }

        foreach (KeyValuePair<string,KeyValuePair<GameObject,IView>> pair in m_UIViewDic)
        {
            if (view.UILayer != pair.Value.Value.UILayer)
            {
                continue;
            }

            if (!pair.Value.Key.activeSelf) {
                continue;
            }
            HideView(pair.Key);
        }

        UIPanel uiPanel = panel.GetComponent<UIPanel>();
        uiPanel.alpha = 1;

        panel.SetActive(true);
        view.Show();
	}

	public static void HideView(string name){
        KeyValuePair<GameObject, IView> pair;
        if (!m_UIViewDic.TryGetValue(name, out pair)) {
            return;
        }

        pair.Key.SetActive(false);
        pair.Value.Hide();
	}

	public static void DestoryAllview(){
        foreach (KeyValuePair<GameObject,IView> pair in m_UIViewDic.Values)
        {
            pair.Value.Destroy();
            GameObject.Destroy(pair.Key);
        }

        m_UIViewDic.Clear();
        Resources.UnloadUnusedAssets();
	}
    public static IView FindView(GameObject gameobject)
    {
        GameObject panel = GetRootPanel(gameobject);

        if (panel == null)
        {
            return null;
        }

        KeyValuePair<GameObject, IView> pair;

        if (!m_UIViewDic.TryGetValue(panel.name, out pair))
        {
            return null;
        }

        return pair.Value;
    }

    public static T FindView<T>(string name) where T : IView
    {
        KeyValuePair<GameObject, IView> pair;

        if (!m_UIViewDic.TryGetValue(name, out pair))
        {
            return null;
        }

        return pair.Value as T;
    }

    /// <summary>
    /// 获取父级最高层次Panel;
    /// </summary>
    /// <param name="gameobject"></param>
    /// <returns></returns>
    private static GameObject GetRootPanel(GameObject gameobject)
    {
        if (gameobject == null)
        {
            return null;
        }

        Transform parent = gameobject.transform.parent;
        if (parent == null)
        {
            UIPanel tempPanel = gameobject.GetComponent<UIPanel>();
            return tempPanel == null ? null : tempPanel.gameObject;
        }

        UIPanel parentPanel = null;

        while (parent != null)
        {
            UIPanel tempPanel = parent.GetComponent<UIPanel>();
            if (tempPanel != null)
            {
                parentPanel = tempPanel;
            }

            parent = parent.parent;
        }

        return parentPanel.gameObject;
    }

	public static T GetChild<T>(this IView view, string name) where T : MonoBehaviour
	{
		GameObject child = GetChild(view, name);
		
		if (child == null)
		{
			return null;
		}
		
		T temp = child.GetComponent<T>();
		if (temp == null)
		{
			Debug.LogError(name + " is not has component ");
		}
		
		return temp;
	}
	
	public static GameObject GetChild(this IView view, string name)
	{
		GameObject UIPrefab = null;
		
		foreach (KeyValuePair<GameObject, IView> pair in m_UIViewDic.Values)
		{
			if (pair.Value == view)
			{
				UIPrefab = pair.Key;
				break;
			}
		}
		
		if (UIPrefab == null)
		{
			return null;
		}
		
		Transform child = UIPrefab.transform.FindRecursively(name);
		if (child == null)
		{
			Debug.LogError(name + " is not child of " + UIPrefab.name);
			return null;
		}
		
		return child.gameObject;
	}

    public static void Update() {
        foreach (KeyValuePair<GameObject,IView> item in m_UIViewDic.Values)
        {
            if (item.Key.activeInHierarchy)
                item.Value.Update();
        }
    }
}
