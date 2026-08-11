using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarketStall;

public static class SelectVars
{
    public static readonly int model = "MarketStall.NPC.Model".GetStableHashCode();
}

public class NPCSelect : MonoBehaviour, TextReceiver
{
    public GameObject m_haldor;
    public GameObject m_selected;
    public ZNetView m_nview;
    public Piece m_piece;

    public string m_currentModel = "Haldor";
    public void Awake()
    {
        m_haldor = transform.Find("Haldor").gameObject;
        m_piece = GetComponent<Piece>();
        m_nview = GetComponent<ZNetView>();
        SetAsPieceLayer();
        if (m_nview.GetZDO() == null || !m_nview.IsValid()) return;
        m_nview.Register(nameof(RPC_UpdateModel), RPC_UpdateModel);
    }

    public void Start()
    {
        if (m_nview.GetZDO() == null || !m_nview.IsValid()) return;
        InvokeRepeating(nameof(UpdateModel), 0f, 10f);
    }
    
    public void RPC_UpdateModel(long sender) => UpdateModel();

    public string GetHoverText()
    {
        return "[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $market_editmodel";
    }

    public void UpdateModel()
    {
        var model = m_nview.GetZDO().GetString(SelectVars.model, MarketStallPlugin.defaultModel.Value);
        if (m_currentModel == model) return;
        SetModel(model);
    }

    public void SetModel(string prefabName)
    {
        if (m_selected != null)
        {
            DestroyImmediate(m_selected);
            m_selected = null;
        }

        if (prefabName == "Haldor")
        {
            m_haldor.SetActive(true);
            m_currentModel = "Haldor";
            return;
        }
        
        var prefab = ZNetScene.instance.GetPrefab(prefabName);
        if (prefab == null)
        {
            MarketStallPlugin.MarketStallLogger.LogDebug($"{prefabName} not found, resetting to Haldor");
            m_haldor.SetActive(true);
            m_currentModel = "Haldor";
            m_nview.GetZDO().Set(SelectVars.model, "");
        }
        else
        {
            ZNetView.m_forceDisableInit = true;
            m_selected = Instantiate(prefab, transform);
            m_selected.SetActive(false);
            m_selected.name = prefabName;
            
            RemoveAllComponents(m_selected);
            
            if (m_selected.TryGetComponent(out Rigidbody rigidbody))
            {
                DestroyImmediate(rigidbody);
            }
            
            m_selected.transform.localPosition = m_haldor.transform.localPosition;
            m_selected.transform.localRotation = Quaternion.Euler(0, 180f, 0f);
            
            m_selected.SetActive(true);
            ZNetView.m_forceDisableInit = false;
            m_haldor.SetActive(false);
            m_currentModel = prefabName;
            
            SetAsPieceLayer();
        }
    }

    public bool IsCreator() => m_piece.IsCreator();
    
    public bool CanEditModel()
    {
        return IsCreator() || ZNet.instance.LocalPlayerIsAdminOrHost();
    }
    
    public void SetAsPieceLayer()
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; ++i)
        {
            var collider = colliders[i];
            collider.gameObject.layer = 10;
        }
    }

    public void RemoveAllComponents(GameObject go)
    {
        var components = go.GetComponentsInChildren<MonoBehaviour>(true);
        var queue = new Queue<MonoBehaviour>(components);
        int stalled = 0;

        while (queue.Count > 0)
        {
            var component = queue.Dequeue();
            var componentType = component.GetType();

            bool isStillDependedOn = queue.Any(other =>
            {
                var otherRequirements = other.GetType()
                    .GetCustomAttributes(typeof(RequireComponent), true)
                    .Cast<RequireComponent>()
                    .SelectMany(r => new[] { r.m_Type0, r.m_Type1, r.m_Type2 })
                    .Where(t => t != null);

                return otherRequirements.Any(r => r.IsAssignableFrom(componentType));
            });

            if (!isStillDependedOn)
            {
                DestroyImmediate(component);
                stalled = 0;
            }
            else
            {
                queue.Enqueue(component);
                ++stalled;

                if (stalled > queue.Count)
                {
                    break;
                }
            }
        }

        if (stalled > 0)
        {
            for (int i = 0; i < queue.Count; ++i)
            {
                var leftover = queue.Dequeue();
                DestroyImmediate(leftover);
            }
        }
    }

    public string GetText() => m_selected == null ? "Haldor" : m_selected.name;

    public void SetText(string text)
    {
        m_nview.GetZDO().Set(SelectVars.model, text);
        m_nview.InvokeRPC(ZNetView.Everybody, nameof(RPC_UpdateModel));
    }
}