using System;
using System.Collections.Generic;
using System.Linq;
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
		public PostProcessLayer.Antialiasing Mode;
		public TemporalAntialiasing TemporalAntialiasing;
		public SubpixelMorphologicalAntialiasing SubpixelMorphologicalAntialiasing;
		public FastApproximateAntialiasing FastApproximateAntialiasing;

		public void Load(ConfigNode parentNode, string nodeName, PostProcessLayer.Antialiasing legacyAntialiasingMode = PostProcessLayer.Antialiasing.None)
		{
			Mode = legacyAntialiasingMode;
			parentNode.ReadObject(nodeName, ref this);

			// if there weren't nodes for these, create defaults
			if (TemporalAntialiasing == null) TemporalAntialiasing = new TemporalAntialiasing();
			if (SubpixelMorphologicalAntialiasing == null) SubpixelMorphologicalAntialiasing = new SubpixelMorphologicalAntialiasing();
			if (FastApproximateAntialiasing == null) FastApproximateAntialiasing = new FastApproximateAntialiasing();
		}
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

		const string PROFILE_NODE_NAME = "TUFX_PROFILE";

		ConfigNode SaveToNode()
		{
			ConfigNode node = new ConfigNode(PROFILE_NODE_NAME);
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
				// reload the file because it might have MM patches etc in there that we don't want to lose.
				UrlDir.UrlFile newFile = new UrlDir.UrlFile(urlConfig.parent.parent, new System.IO.FileInfo(urlConfig.parent.fullPath));

				for (int i = 0; i < newFile.configs.Count; ++i)
				{
					if (newFile.configs[i].type == PROFILE_NODE_NAME && newFile.configs[i].name == ProfileName)
					{
						newFile.configs[i].config = SaveToNode();
						newFile.SaveConfigs();
						urlConfig = newFile.configs[i];
						return true;
					}
				}
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

			// support loading legacy profiles
			PostProcessLayer.Antialiasing legacyAntialiasing = node.GetEnumValue("antialiasing", PostProcessLayer.Antialiasing.None);

			Antialiasing.Load(node, nameof(Antialiasing), legacyAntialiasing);
			SecondaryCameraAntialiasing.Load(node, nameof(SecondaryCameraAntialiasing));

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
