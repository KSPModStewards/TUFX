﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace TUFX
{

	/// <summary>
	/// Enumeration of the built-in effect classes, to provide mapping between the name of the class and creation of an instance of that class.
	/// Used in order to provide run-time profile creation from a ConfigNode based configuration system.
	/// </summary>
	public enum BuiltinEffect
	{
		AmbientOcclusion,
		AutoExposure,
		Bloom,
		ChromaticAberration,
		ColorGrading,
		DepthOfField,
		Grain,
		LensDistortion,
		MotionBlur,
		Scattering,
		Vignette
	}

	public class TUFXProfileManager
	{

		public static PostProcessEffectSettings CreateEmptySettingsForEffect(BuiltinEffect effect)
		{
			switch (effect)
			{
				case BuiltinEffect.AmbientOcclusion:
					return ScriptableObject.CreateInstance<AmbientOcclusion>();
				case BuiltinEffect.AutoExposure:
					return ScriptableObject.CreateInstance<AutoExposure>();
				case BuiltinEffect.Bloom:
					return ScriptableObject.CreateInstance<Bloom>();
				case BuiltinEffect.ChromaticAberration:
					return ScriptableObject.CreateInstance<ChromaticAberration>();
				case BuiltinEffect.ColorGrading:
					return ScriptableObject.CreateInstance<ColorGrading>();
				case BuiltinEffect.DepthOfField:
					return ScriptableObject.CreateInstance<DepthOfField>();
				case BuiltinEffect.Grain:
					return ScriptableObject.CreateInstance<Grain>();
				case BuiltinEffect.LensDistortion:
					return ScriptableObject.CreateInstance<LensDistortion>();
				case BuiltinEffect.MotionBlur:
					return ScriptableObject.CreateInstance<MotionBlur>();
				case BuiltinEffect.Scattering:
					return ScriptableObject.CreateInstance<TUBISEffect>();
				case BuiltinEffect.Vignette:
					return ScriptableObject.CreateInstance<Vignette>();
				default:
					break;
			}
			return null;
		}

		public static BuiltinEffect GetBuiltinEffect(PostProcessEffectSettings settings)
		{
			if (settings is AmbientOcclusion) { return BuiltinEffect.AmbientOcclusion; }
			else if (settings is AutoExposure) { return BuiltinEffect.AutoExposure; }
			else if (settings is Bloom) { return BuiltinEffect.Bloom; }
			else if (settings is ChromaticAberration) { return BuiltinEffect.ChromaticAberration; }
			else if (settings is ColorGrading) { return BuiltinEffect.ColorGrading; }
			else if (settings is DepthOfField) { return BuiltinEffect.DepthOfField; }
			else if (settings is Grain) { return BuiltinEffect.Grain; }
			else if (settings is LensDistortion) { return BuiltinEffect.LensDistortion; }
			else if (settings is MotionBlur) { return BuiltinEffect.MotionBlur; }
			else if (settings is TUBISEffect) { return BuiltinEffect.Scattering; }
			else if (settings is Vignette) { return BuiltinEffect.Vignette; }
			return BuiltinEffect.AmbientOcclusion;
		}

	}

	public struct AntialiasingParameters
	{
		public void Init()
		{
			Mode = PostProcessLayer.Antialiasing.None;
			TemporalAntialiasing = new TemporalAntialiasing();
			SubpixelMorphologicalAntialiasing = new SubpixelMorphologicalAntialiasing();
			FastApproximateAntialiasing = new FastApproximateAntialiasing();
		}

		public PostProcessLayer.Antialiasing Mode;
		public TemporalAntialiasing TemporalAntialiasing;
		public SubpixelMorphologicalAntialiasing SubpixelMorphologicalAntialiasing;
		public FastApproximateAntialiasing FastApproximateAntialiasing;
	}

	public class TUFXProfile
	{

		public string ProfileName { get; private set; }

		public bool HDREnabled;

		public AntialiasingParameters Antialiasing;

		public AntialiasingParameters SecondaryCameraAntialiasing;

		private UrlDir.UrlConfig urlConfig;

		public string CfgPath => urlConfig.parent.url;

		/// <summary>
		/// List of the override settings currently configured for this profile
		/// </summary>
		public readonly List<PostProcessEffectSettings> Settings = new List<PostProcessEffectSettings>();

		/// <summary>
		/// Profile constructor, takes a ConfigNode containing the profile configuration.
		/// </summary>
		/// <param name="node"></param>
		public TUFXProfile(UrlDir.UrlConfig config)
		{
			urlConfig = config;
			LoadProfile(config.config);
		}

		ConfigNode SaveToNode()
		{
			ConfigNode node = new ConfigNode("TUFX_PROFILE");
			node.SetValue("name", ProfileName, true);
			node.SetValue("hdr", HDREnabled, true);

			node.WriteObject(nameof(Antialiasing), Antialiasing);
			node.WriteObject(nameof(SecondaryCameraAntialiasing), SecondaryCameraAntialiasing);

			int len = Settings.Count;
			for (int i = 0; i < len; i++)
			{
				if (Settings[i].enabled)
				{
					ConfigNode effectNode = new ConfigNode("EFFECT");
					effectNode.SetValue("name", TUFXProfileManager.GetBuiltinEffect(Settings[i]).ToString(), true);
					Settings[i].Save(effectNode);
					node.AddNode(effectNode);
				}
			}

			return node;
		}

		public bool SaveToDisk()
		{
			try
			{
				urlConfig.config = SaveToNode();
				urlConfig.parent.SaveConfigs();
				return true;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			return false;
		}

		public void ReloadFromNode()
		{
			LoadProfile(urlConfig.config);
		}

		/// <summary>
		/// Loads the profile from the input configuration node.
		/// </summary>
		/// <param name="node"></param>
		void LoadProfile(ConfigNode node)
		{
			ProfileName = node.GetStringValue("name");
			HDREnabled = node.GetBoolValue("hdr", false);

			Antialiasing.Init();
			SecondaryCameraAntialiasing.Init();

			// support loading legacy profiles
			Antialiasing.Mode = node.GetEnumValue("antialiasing", PostProcessLayer.Antialiasing.None);

			node.ReadObject(nameof(Antialiasing), ref Antialiasing);
			node.ReadObject(nameof(SecondaryCameraAntialiasing), ref SecondaryCameraAntialiasing);

			Settings.Clear();
			ConfigNode[] effectNodes = node.GetNodes("EFFECT");
			int len = effectNodes.Length;
			for (int i = 0; i < len; i++)
			{
				BuiltinEffect effect = effectNodes[i].GetEnumValue("name", BuiltinEffect.AmbientOcclusion);
				PostProcessEffectSettings set = TUFXProfileManager.CreateEmptySettingsForEffect(effect);
				set.enabled.Override(true);
				set.Load(effectNodes[i]);
				Settings.Add(set);
			}
		}

		/// <summary>
		/// Returns a Unity PostProcessProfile instance with the settings contained in this TUFXProfile
		/// </summary>
		public PostProcessProfile CreatePostProcessProfile()
		{
			PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
			int len = Settings.Count;
			for (int i = 0; i < len; i++)
			{
				profile.settings.Add(Settings[i]);
			}
			profile.isDirty = true;
			profile.name = this.ProfileName;
			return profile;
		}

		/// <summary>
		/// Returns the PostProcessEffectSettings present in the settings list for the input Type, or null if no settings of that type are present.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetSettingsFor<T>() where T : PostProcessEffectSettings
		{
			return (T)Settings.FirstOrDefault(m => m is T);
		}

	}

}
