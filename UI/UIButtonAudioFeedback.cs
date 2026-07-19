using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI;

internal sealed class UIButtonAudioFeedback
{
   private Signal _buttonClickSignal;

   public void Attach(Button button)
   {
      // Native button prefabs normally carry this sender. Keep it as the
      // authoritative source so a click is never reported twice.
      if (button.GetComponent<SignalOnUIButtonClick>() != null) return;

      button.onClick.AddListener(PlayClickSound);
   }

   private void PlayClickSound()
   {
      if (_buttonClickSignal == null) _buttonClickSignal = FindButtonClickSignal();
      _buttonClickSignal?.Raise();
   }

   private static Signal FindButtonClickSignal()
   {
      return Resources
         .FindObjectsOfTypeAll<BasicUIAudioManager>()
         .Where(audioManager => audioManager.isActiveAndEnabled && audioManager._buttonClickEvents != null)
         .SelectMany(audioManager => audioManager._buttonClickEvents)
         .FirstOrDefault(signal => signal != null);
   }
}