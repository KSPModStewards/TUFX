using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TUFX
{
    public static class Utils
    {

        #region REGION - Config Node Extensions

        public static string GetStringValue(this ConfigNode node, string name, string defaultValue)
        {
            String value = node.GetValue(name);
            return value == null ? defaultValue : value;
        }

        public static string GetStringValue(this ConfigNode node, string name)
        {
            return GetStringValue(node, name, "");
        }

        public static bool GetBoolValue(this ConfigNode node, string name, bool defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return bool.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static bool GetBoolValue(this ConfigNode node, string name)
        {
            return GetBoolValue(node, name, false);
        }

        public static float GetFloatValue(this ConfigNode node, string name, float defaultValue)
        {
            String value = node.GetValue(name);
            if (value == null) { return defaultValue; }
            try
            {
                return float.Parse(value);
            }
            catch (Exception e)
            {
                MonoBehaviour.print(e.Message);
            }
            return defaultValue;
        }

        public static float GetFloatValue(this ConfigNode node, string name)
        {
            return GetFloatValue(node, name, 0);
        }

        public static T GetEnumValue<T>(this ConfigNode node, string name, T defaultValue)
        {
            string value = node.GetStringValue(name);
            if (string.IsNullOrEmpty(value)) { return defaultValue; }
            try
            {
                return (T)Enum.Parse(defaultValue.GetType(), value);
            }
            catch (Exception e)
            {
                Log.debug(e.ToString());
                return defaultValue;
            }
        }
        #endregion

        public static float[] safeParseFloatArray(string val)
        {
            string[] vals = val.Split(',');
            int len = vals.Length;
            float[] fVals = new float[len];
            for (int i = 0; i < len; i++)
            {
                if (!float.TryParse(vals[i], out float v)) { v = 0; }
                fVals[i] = v;
            }
            return fVals;
        }

    }

    public static class Log
    {
        [Conditional("DEBUG")]
        public static void debug(string msg)
        {
#if DEBUG
            MonoBehaviour.print("[TUFX-DEBUG] " + msg);
#endif
        }
        public static void log(string msg) { MonoBehaviour.print("[TUFX] " + msg); }
        public static void exception(Exception ex)
        {
            Debug.LogException(ex);
        }

        public static void error(string msg)
        {
            Debug.LogError(msg);
        }
    }
}
