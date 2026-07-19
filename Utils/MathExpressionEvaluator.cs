using System;
using System.Data;
using System.Globalization;

namespace EditorEnhanced.Utils;

public static class MathExpressionEvaluator
{
   public static bool TryEvaluate(string input, out string result)
   {
      result = input;
      if (string.IsNullOrWhiteSpace(input)) return false;

      try
      {
         var table = new DataTable { Locale = CultureInfo.InvariantCulture };
         var computed = table.Compute(input, string.Empty);
         if (computed == null || computed == DBNull.Value) return false;

         result = Convert.ToString(computed, CultureInfo.InvariantCulture);
         return !string.IsNullOrWhiteSpace(result);
      }
      catch (Exception)
      {
         return false;
      }
   }
}
