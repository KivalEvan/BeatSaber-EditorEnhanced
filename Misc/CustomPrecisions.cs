using System.Collections.Generic;
using BeatmapEditor3D.Types;

namespace EditorEnhanced.Misc;

public static class CustomPrecisions
{
   public static readonly Dictionary<PrecisionType, float> NoPrecisionFloat = new()
   {
      {
         PrecisionType.Ultra, 1f
      },
      {
         PrecisionType.High, 1f
      },
      {
         PrecisionType.Standard, 1f
      },
      {
         PrecisionType.Low, 1f
      }
   };

   public static readonly Dictionary<PrecisionType, int> NoPrecisionInt = new()
   {
      {
         PrecisionType.Ultra, 1
      },
      {
         PrecisionType.High, 1
      },
      {
         PrecisionType.Standard, 1
      },
      {
         PrecisionType.Low, 1
      }
   };
   
   public static readonly Dictionary<PrecisionType, float> TimePrecisionFloat = new()
   {
      {
         PrecisionType.Ultra, 0.01f
      },
      {
         PrecisionType.High, 0.1f
      },
      {
         PrecisionType.Standard, 0.5f
      },
      {
         PrecisionType.Low, 1f
      }
   };
   
   public static readonly Dictionary<PrecisionType, float> PercentPrecisionFloat = new()
   {
      {
         PrecisionType.Ultra, 1f
      },
      {
         PrecisionType.High, 5f
      },
      {
         PrecisionType.Standard, 10f
      },
      {
         PrecisionType.Low, 50f
      }
   };
   
   public static readonly Dictionary<PrecisionType, int> PercentPrecisionInt = new()
   {
      {
         PrecisionType.Ultra, 1
      },
      {
         PrecisionType.High, 5
      },
      {
         PrecisionType.Standard, 10
      },
      {
         PrecisionType.Low, 50
      }
   };
}