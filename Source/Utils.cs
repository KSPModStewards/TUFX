using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        private static void WriteField(ConfigNode node, string fieldName, object fieldValue)
        {
            if (fieldValue == null) return;

            Type fieldType = fieldValue.GetType();

            if (fieldValue is IList list)
            {
                // TODO: handle this
            }
			// this should handle primitive types and strings
			else if (ConfigNode.WriteValue(fieldName, fieldValue.GetType(), fieldValue, node))
			{
				// cool
			}
			else
			{
				// This should be a struct or class
                WriteObject(node, fieldName, fieldValue);
			}
		}

        readonly static Type[] k_emptyTypeArray = new Type[0];
        readonly static object[] k_emptyObjectArray = new object[0];

        private static object ReadField(ConfigNode node, Type fieldType, string fieldName)
        {
			var cfgValue = node.GetValue(fieldName);

			if (typeof(IList).IsAssignableFrom(fieldType))
            {
                // TODO: handle this
            }
            else if (cfgValue != null)
            {
				// this should handle primitive types and strings
				object fieldValue = ConfigNode.ReadValue(fieldType, cfgValue);
                if (fieldValue != null)
                {
                    return fieldValue;
                }
			}
            else
            {
                var objNode = node.GetNode(fieldName);

                if (objNode != null)
                {
                    // this should be a struct or class
                    ConstructorInfo constructor = fieldType.GetConstructor(k_emptyTypeArray);
                    object obj = constructor.Invoke(k_emptyObjectArray);
                    ReadObject(node, fieldName, ref obj);
                    return obj;
                }
            }

            // TODO: this will be returning null for value types, when really we want the default value for that type...how does that work?
            return default;
		}

        public static void WriteObject(this ConfigNode node, string name, object obj)
        {
            ConfigNode objNode = node.AddNode(name);
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            foreach (FieldInfo field in fields)
            {
                WriteField(objNode, field.Name, field.GetValue(obj));
            }

            foreach (PropertyInfo property in properties)
            {
                var setMethod = property.GetSetMethod();

				if (setMethod != null && setMethod.IsPublic)
                {
                    WriteField(objNode, property.Name, property.GetValue(obj));
                }
            }
        }

        public static void ReadObject<T>(this ConfigNode node, string name, ref T obj)
        {
            ConfigNode objNode = node.GetNode(name);
            if (objNode == null) return;

            var type = obj.GetType();
            
            // If obj is a value type, then calling field.SetValue is going to operate on a boxed copy and won't affect the original
            // so therefore we need to box a copy and then copy it back to the ref param.
            // If obj is a reference type, this isn't even a copy so it's no big deal.
            object objCopy = obj;

			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            foreach (FieldInfo field in fields)
            {
                object fieldValue = ReadField(objNode, field.FieldType, field.Name);

                field.SetValue(objCopy, fieldValue);
            }

            foreach (PropertyInfo property in properties)
            {
				var setMethod = property.GetSetMethod();

				if (setMethod != null && setMethod.IsPublic)
				{
					object fieldValue = ReadField(objNode, property.PropertyType, property.Name);
                    property.SetValue(objCopy, fieldValue);
				}
            }

			obj = (T)objCopy;
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
