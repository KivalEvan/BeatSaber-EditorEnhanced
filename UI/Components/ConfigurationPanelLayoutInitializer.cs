using System.Collections;
using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Components;

internal sealed class ConfigurationPanelLayoutInitializer : MonoBehaviour
{
   private RectTransform _hostContent;
   private RectTransform _innerContent;
   private GameObject _panelRoot;
   private ScrollView _scrollView;

   public void Configure(
      GameObject panelRoot,
      RectTransform innerContent,
      RectTransform hostContent,
      ScrollView scrollView)
   {
      _panelRoot = panelRoot;
      _innerContent = innerContent;
      _hostContent = hostContent;
      _scrollView = scrollView;
   }

   private void Start()
   {
      StartCoroutine(InitializeLayout());
   }

   private IEnumerator InitializeLayout()
   {
      yield return null;
      yield return new WaitForEndOfFrame();

      if (!HasValidReferences())
      {
         Destroy(this);
         yield break;
      }

      var panelWasActive = _panelRoot.activeSelf;
      var layoutElement = _panelRoot.GetComponent<LayoutElement>();
      var panelWasIgnored = layoutElement.ignoreLayout;
      layoutElement.ignoreLayout = true;
      var graphics = _panelRoot.GetComponentsInChildren<Graphic>(true);
      var graphicColors = new Color[graphics.Length];
      var graphicRaycastStates = new bool[graphics.Length];
      for (var i = 0; i < graphics.Length; i++)
      {
         graphicColors[i] = graphics[i].color;
         graphicRaycastStates[i] = graphics[i].raycastTarget;
         var hiddenColor = graphicColors[i];
         hiddenColor.a = 0f;
         graphics[i].color = hiddenColor;
         graphics[i].raycastTarget = false;
      }

      _panelRoot.SetActive(true);
      yield return null;
      yield return new WaitForEndOfFrame();

      if (HasValidReferences())
      {
         LayoutRebuilder.ForceRebuildLayoutImmediate(_hostContent);
         UpdatePanelWidth();
         RebuildLayoutBottomUp(_innerContent);

         if (TryLockPanelHeight(out var height))
         {
             layoutElement.ignoreLayout = false;
             LayoutRebuilder.ForceRebuildLayoutImmediate(_hostContent);
             Plugin.Log.Debug($"Locked the Editor Enhanced panel height to {height}px.");
         }
         else
         {
            if (layoutElement != null) layoutElement.ignoreLayout = panelWasIgnored;
            Plugin.Log.Warn("Could not measure the Editor Enhanced panel height after layout initialization.");
         }
      }
      else
      {
         if (layoutElement != null) layoutElement.ignoreLayout = panelWasIgnored;
      }

      if (_panelRoot != null) _panelRoot.SetActive(panelWasActive);
      for (var i = 0; i < graphics.Length; i++)
         if (graphics[i] != null)
         {
            graphics[i].color = graphicColors[i];
            graphics[i].raycastTarget = graphicRaycastStates[i];
         }
      if (_panelRoot != null && _scrollView != null)
      {
         var speedController = _panelRoot.GetComponent<ConfigurationPanelScrollSpeedController>()
            ?? _panelRoot.AddComponent<ConfigurationPanelScrollSpeedController>();
         speedController.Configure(_scrollView);
      }
      Destroy(this);
   }

   private bool TryLockPanelHeight(out float height)
   {
      var layoutGroup = _innerContent.GetComponent<VerticalLayoutGroup>();
      height = Mathf.Max(
         LayoutUtility.GetPreferredHeight(_innerContent),
         layoutGroup == null ? 0f : layoutGroup.preferredHeight);
      if (height <= 0f || float.IsNaN(height) || float.IsInfinity(height)) return false;

      height = Mathf.Ceil(height);
      var layoutElement = _panelRoot.GetComponent<LayoutElement>();
      layoutElement.minHeight = height;
      layoutElement.preferredHeight = height;
      layoutElement.flexibleHeight = 0f;
      ((RectTransform)_panelRoot.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
      return true;
   }

   private void UpdatePanelWidth()
   {
      var width = _hostContent.rect.width;
      if (width <= 0f) width = _hostContent.sizeDelta.x;
      if (width > 0f)
         ((RectTransform)_panelRoot.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
   }

   private static void RebuildLayoutBottomUp(RectTransform root)
   {
      for (var i = 0; i < root.childCount; i++)
      {
         if (root.GetChild(i) is RectTransform child && child.gameObject.activeInHierarchy)
            RebuildLayoutBottomUp(child);
      }

      LayoutRebuilder.ForceRebuildLayoutImmediate(root);
   }

   private bool HasValidReferences()
   {
      return _panelRoot != null && _innerContent != null && _hostContent != null;
   }
}
