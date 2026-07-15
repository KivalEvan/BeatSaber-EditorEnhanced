using System.Collections.Generic;
using UnityEngine;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo;

internal sealed class GizmoEffectContext
{
   public GizmoEffectContext(
      LightColorGroupEffectManager colorManager,
      LightRotationGroupEffectManager rotationManager,
      LightTranslationGroupEffectManager translationManager,
      FloatFxGroupEffectManager fxManager)
   {
      ColorManager = colorManager;
      RotationManager = rotationManager;
      TranslationManager = translationManager;
      FxManager = fxManager;
   }

   public LightColorGroupEffectManager ColorManager { get; }
   public LightRotationGroupEffectManager RotationManager { get; }
   public LightTranslationGroupEffectManager TranslationManager { get; }
   public FloatFxGroupEffectManager FxManager { get; }
   public Transform Root => ColorManager.transform.root;
}

internal sealed class GizmoEffectContextResolver
{
   private readonly HashSet<EventBoxGroupType> _reportedMissingTypes = [];
   private LightColorGroupEffectManager _colorManager;
   private FloatFxGroupEffectManager _fxManager;
   private LightRotationGroupEffectManager _rotationManager;
   private LightTranslationGroupEffectManager _translationManager;

   public bool TryResolve(EventBoxGroupType groupType, out GizmoEffectContext context)
   {
      ResolveManagers();
      context = null;

      var available = _colorManager != null
         && groupType switch
         {
            EventBoxGroupType.Color => true,
            EventBoxGroupType.Rotation => _rotationManager != null,
            EventBoxGroupType.Translation => _translationManager != null,
            EventBoxGroupType.FloatFx => _fxManager != null,
            _ => false
         };
      if (!available)
      {
         if (_reportedMissingTypes.Add(groupType))
            Plugin.Log.Warn($"Cannot create gizmos: effect managers for {groupType} are unavailable.");
         return false;
      }

      context = new GizmoEffectContext(_colorManager, _rotationManager, _translationManager, _fxManager);
      return true;
   }

   private void ResolveManagers()
   {
      if (_colorManager == null) _colorManager = Object.FindAnyObjectByType<LightColorGroupEffectManager>();
      if (_rotationManager == null) _rotationManager = Object.FindAnyObjectByType<LightRotationGroupEffectManager>();
      if (_translationManager == null)
         _translationManager = Object.FindAnyObjectByType<LightTranslationGroupEffectManager>();
      if (_fxManager == null) _fxManager = Object.FindAnyObjectByType<FloatFxGroupEffectManager>();
   }
}