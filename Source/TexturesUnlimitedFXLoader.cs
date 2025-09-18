using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolbarControl_NS;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Log = KSPBuildTools.Log;

namespace TUFX
{
	public enum TUFXScene
	{
		MainMenu,
		SpaceCenter,
		Editor,
		Flight,
		Map,
		Internal,
		TrackingStation,
	}

	/// <summary>
	/// The main KSPAddon that holds profile loading and handling logic, resource reference storage,
	/// provides the public functions to update and enable profiles, and manages ApplicationLauncher button and GUI spawning.
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	public class TexturesUnlimitedFXLoader : MonoBehaviour
	{

		internal static TexturesUnlimitedFXLoader INSTANCE;
		private ConfigurationGUI configGUI;
		private DebugGUI debugGUI;

		private ToolbarControl mainToolbarControl;

		private Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
		private Dictionary<string, ComputeShader> computeShaders = new Dictionary<string, ComputeShader>();
		private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

		internal Dictionary<string, TUFXEffectTextureList> EffectTextureLists { get; private set; } = new Dictionary<string, TUFXEffectTextureList>();

		/// <summary>
		/// The currently loaded profiles.
		/// This will be cleared and reset whenever ModuleManagerPostLoad() is called (e.g. in-game config reload).
		/// </summary>
		internal Dictionary<string, TUFXProfile> Profiles { get; private set; } = new Dictionary<string, TUFXProfile>();

		public class Configuration
		{
			public const string NODE_NAME = "TUFX_CONFIGURATION";
			public const string EMPTY_PROFILE_NAME = "Default-Empty";

			[Persistent] public string MainMenuProfile = "Default-MainMenu";
			[Persistent] public string SpaceCenterSceneProfile = "Default-KSC";
			[Persistent] public string EditorSceneProfile = "Default-Editor";
			[Persistent] public string FlightSceneProfile = "Default-Flight";
			[Persistent] public string MapSceneProfile = "Default-Tracking";
			[Persistent] public string IVAProfile = "Default-Internal";
			[Persistent] public string TrackingStationProfile = "Default-Tracking";
			[Persistent] public bool ShowToolbarButton = true;

			public string GetProfileName(TUFXScene scene)
			{
				switch (scene)
				{
					case TUFXScene.MainMenu: return MainMenuProfile;
					case TUFXScene.SpaceCenter: return SpaceCenterSceneProfile;
					case TUFXScene.Editor: return EditorSceneProfile;
					case TUFXScene.Flight: return FlightSceneProfile;
					case TUFXScene.Map: return MapSceneProfile;
					case TUFXScene.Internal: return IVAProfile;
					case TUFXScene.TrackingStation: return TrackingStationProfile;
				}
				return string.Empty;
			}
		}

		internal static UrlDir.UrlConfig defaultConfigUrl = null;
		internal static readonly Configuration defaultConfiguration = new Configuration();

		private PostProcessVolume mainVolume;

		/// <summary>
		/// The currently active profile.  Private field to enforce use of the 'setProfileForScene' method.
		/// </summary>
		private TUFXProfile currentProfile;
		/// <summary>
		/// Return a reference to the currently active profile.  To update the current profile, use the <see cref="setProfileForScene(string name, GameScenes scene, bool map, bool apply)"/> method.
		/// </summary>
		internal TUFXProfile CurrentProfile => currentProfile;
		/// <summary>
		/// Return the name of the currently active profile.
		/// </summary>
		internal string CurrentProfileName => currentProfile == null ? string.Empty : currentProfile.ProfileName;

		/// <summary>
		/// Reference to the Unity Post Processing 'Resources' class.  Used to store references to the shaders and textures used by the post-processing system internals.
		/// Does not include references to the 'included' but 'external' resources such as the built-in lens-dirt textures or any custom LUTs.
		/// </summary>
		public static PostProcessResources Resources { get; private set; }

		private PostProcessVolume CreateVolume(int layer)
		{
			var childObject = new GameObject("Postprocessing Volume");
			childObject.layer = layer;
			childObject.transform.SetParent(transform, false);
			var volume = childObject.AddComponent<PostProcessVolume>();
			volume.isGlobal = true;
			return volume;
		}

		public void Start()
		{
			MonoBehaviour.print("TUFXLoader - Start()");
			INSTANCE = this;
			DontDestroyOnLoad(this);

			// set up toolbar
			if (defaultConfiguration.ShowToolbarButton)
			{
				const string toolbarMainName = "TUFX";
				const string toolbarDebugName = "TUFX-Debug";

				ToolbarControl.RegisterMod(toolbarMainName);

				mainToolbarControl = gameObject.AddComponent<ToolbarControl>();
				mainToolbarControl.AddToAllToolbars(
					configGuiEnable,
					configGuiDisable,
					ApplicationLauncher.AppScenes.ALWAYS,
					toolbarMainName,
					toolbarMainName,
					"TUFX/Assets/TUFX-Icon1",
					"TUFX/Assets/TUFX-Icon1",
					toolbarMainName);

#if DEBUG
				ToolbarControl.RegisterMod(toolbarDebugName);
				var debugToolbarControl = gameObject.AddComponent<ToolbarControl>();
				debugToolbarControl.AddToAllToolbars(
					debugGuiEnable,
					debugGuiDisable,
					ApplicationLauncher.AppScenes.ALWAYS,
					toolbarDebugName,
					toolbarDebugName,
					"TUFX/Assets/TUFX-Icon2",
					"TUFX/Assets/TUFX-Icon2",
					toolbarDebugName);
#endif
			}
			mainVolume = CreateVolume(0);
		}
		public void ModuleManagerPostLoad()
		{
			Log.Message("TUFXLoader - MMPostLoad()");

			//only load resources once.  In case of MM reload...
			if (Resources == null)
			{
				loadResources();

				GameEvents.onLevelWasLoaded.Add(new EventData<GameScenes>.OnEvent(onLevelLoaded));
				GameEvents.OnCameraChange.Add(new EventData<CameraManager.CameraMode>.OnEvent(cameraChange));
			}

			loadTextures();

			loadProfiles();
		}

		/// <summary>
		/// Loads the mandatory shaders and textures required by the post-processing stack codebase and effects from the AssetBundles included in the mod.
		/// </summary>
		private void loadResources()
		{
			Resources = ScriptableObject.CreateInstance<PostProcessResources>();
			Resources.shaders = new PostProcessResources.Shaders();
			Resources.computeShaders = new PostProcessResources.ComputeShaders();
			Resources.blueNoise64 = new Texture2D[64];
			Resources.blueNoise256 = new Texture2D[8];
			Resources.smaaLuts = new PostProcessResources.SMAALuts();

			//previously this did not work... but appears to with these bundles/Unity version
			AssetBundle bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Shaders/tufx-universal.ssf");
			Shader[] shaders = bundle.LoadAllAssets<Shader>();
			int len = shaders.Length;
			for (int i = 0; i < len; i++)
			{
				if (!this.shaders.ContainsKey(shaders[i].name)) { this.shaders.Add(shaders[i].name, shaders[i]); }
			}
			ComputeShader[] compShaders = bundle.LoadAllAssets<ComputeShader>();
			len = compShaders.Length;
			for (int i = 0; i < len; i++)
			{
				if (!this.computeShaders.ContainsKey(compShaders[i].name)) { this.computeShaders.Add(compShaders[i].name, compShaders[i]); }
			}
			bundle.Unload(false);

			//TODO -- cleanup loading
			try
			{
				bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Shaders/tufx-scattering.ssf");
				shaders = bundle.LoadAllAssets<Shader>();
				len = shaders.Length;
				for (int i = 0; i < len; i++)
				{
					if (!this.shaders.ContainsKey(shaders[i].name))
					{
						this.shaders.Add(shaders[i].name, shaders[i]);
						Log.Debug("Loading scattering shader: " + shaders[i].name);
					}
				}
				compShaders = bundle.LoadAllAssets<ComputeShader>();
				len = compShaders.Length;
				for (int i = 0; i < len; i++)
				{
					if (!this.computeShaders.ContainsKey(compShaders[i].name))
					{
						this.computeShaders.Add(compShaders[i].name, compShaders[i]);
						Log.Debug("Loading scattering compute shader: " + compShaders[i].name);
					}
				}
				bundle.Unload(false);
				TUFXScatteringResources.PrecomputeShader = getComputeShader("Precomputation"); ;
				TUFXScatteringResources.ScatteringShader = getShader("TU/BIS");
			}
			catch (Exception e)
			{
				Log.Debug(e.ToString());
			}

			#region REGION - Load standard Post Process Effect Shaders
			Resources.shaders.bloom = getShader("Hidden/PostProcessing/Bloom");
			Resources.shaders.copy = getShader("Hidden/PostProcessing/Copy");
			Resources.shaders.copyStd = getShader("Hidden/PostProcessing/CopyStd");
			Resources.shaders.copyStdFromDoubleWide = getShader("Hidden/PostProcessing/CopyStdFromDoubleWide");
			Resources.shaders.copyStdFromTexArray = getShader("Hidden/PostProcessing/CopyStdFromTexArray");
			Resources.shaders.deferredFog = getShader("Hidden/PostProcessing/DeferredFog");
			Resources.shaders.depthOfField = getShader("Hidden/PostProcessing/DepthOfField");
			Resources.shaders.discardAlpha = getShader("Hidden/PostProcessing/DiscardAlpha");
			Resources.shaders.finalPass = getShader("Hidden/PostProcessing/FinalPass");
			Resources.shaders.gammaHistogram = getShader("Hidden/PostProcessing/Debug/Histogram");//TODO - part of debug shaders?
			Resources.shaders.grainBaker = getShader("Hidden/PostProcessing/GrainBaker");
			Resources.shaders.lightMeter = getShader("Hidden/PostProcessing/Debug/LightMeter");//TODO - part of debug shaders?
			Resources.shaders.lut2DBaker = getShader("Hidden/PostProcessing/Lut2DBaker");
			Resources.shaders.motionBlur = getShader("Hidden/PostProcessing/MotionBlur");
			Resources.shaders.multiScaleAO = getShader("Hidden/PostProcessing/MultiScaleVO");
			Resources.shaders.scalableAO = getShader("Hidden/PostProcessing/ScalableAO");
			Resources.shaders.screenSpaceReflections = getShader("Hidden/PostProcessing/ScreenSpaceReflections");
			Resources.shaders.subpixelMorphologicalAntialiasing = getShader("Hidden/PostProcessing/SubpixelMorphologicalAntialiasing");
			Resources.shaders.temporalAntialiasing = getShader("Hidden/PostProcessing/TemporalAntialiasing");
			Resources.shaders.texture2dLerp = getShader("Hidden/PostProcessing/Texture2DLerp");
			Resources.shaders.uber = getShader("Hidden/PostProcessing/Uber");
			Resources.shaders.vectorscope = getShader("Hidden/PostProcessing/Debug/Vectorscope");//TODO - part of debug shaders?
			Resources.shaders.waveform = getShader("Hidden/PostProcessing/Debug/Waveform");//TODO - part of debug shaders?
			#endregion

			#region REGION - Load compute shaders
			Resources.computeShaders.autoExposure = getComputeShader("AutoExposure");
			Resources.computeShaders.exposureHistogram = getComputeShader("ExposureHistogram");
			Resources.computeShaders.gammaHistogram = getComputeShader("GammaHistogram");//TODO - part of debug shaders?
			Resources.computeShaders.gaussianDownsample = getComputeShader("GaussianDownsample");
			Resources.computeShaders.lut3DBaker = getComputeShader("Lut3DBaker");
			Resources.computeShaders.multiScaleAODownsample1 = getComputeShader("MultiScaleVODownsample1");
			Resources.computeShaders.multiScaleAODownsample2 = getComputeShader("MultiScaleVODownsample2");
			Resources.computeShaders.multiScaleAORender = getComputeShader("MultiScaleVORender");
			Resources.computeShaders.multiScaleAOUpsample = getComputeShader("MultiScaleVOUpsample");
			Resources.computeShaders.texture3dLerp = getComputeShader("Texture3DLerp");
			Resources.computeShaders.vectorscope = getComputeShader("Vectorscope");//TODO - part of debug shaders?
			Resources.computeShaders.waveform = getComputeShader("Waveform");//TODO - part of debug shaders?
			#endregion

			#region REGION - Load textures
			bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Textures/tufx-tex-bluenoise64.ssf");
			Texture2D[] tex = bundle.LoadAllAssets<Texture2D>();
			len = tex.Length;
			for (int i = 0; i < len; i++)
			{
				string idxStr = tex[i].name.Substring(tex[i].name.Length - 2).Replace("_", "");
				int idx = int.Parse(idxStr);
				Resources.blueNoise64[idx] = tex[i];
			}
			bundle.Unload(false);

			bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Textures/tufx-tex-bluenoise256.ssf");
			tex = bundle.LoadAllAssets<Texture2D>();
			len = tex.Length;
			for (int i = 0; i < len; i++)
			{
				string idxStr = tex[i].name.Substring(tex[i].name.Length - 2).Replace("_", "");
				int idx = int.Parse(idxStr);
				Resources.blueNoise256[idx] = tex[i];
			}
			bundle.Unload(false);

			bundle = AssetBundle.LoadFromFile(KSPUtil.ApplicationRootPath + "GameData/TUFX/Textures/tufx-tex-smaa.ssf");
			tex = bundle.LoadAllAssets<Texture2D>();
			len = tex.Length;
			for (int i = 0; i < len; i++)
			{
				if (tex[i].name == "AreaTex") { Resources.smaaLuts.area = tex[i]; }
				else { Resources.smaaLuts.search = tex[i]; }
			}
			bundle.Unload(false);
			#endregion
		}

		private void loadTextures()
		{
			//yeah, wow, that got ugly fast...
			EffectTextureLists.Clear();
			ConfigNode[] textureListNodes = GameDatabase.Instance.GetConfigNodes("TUFX_TEXTURES");
			int len = textureListNodes.Length;
			for (int i = 0; i < len; i++)
			{
				Log.Debug("Loading TUFX_TEXTURES[" + textureListNodes[i].GetValue("name") + "]");
				ConfigNode[] effectTextureLists = textureListNodes[i].GetNodes("EFFECT");
				int len2 = effectTextureLists.Length;
				for (int k = 0; k < len2; k++)
				{
					string effectName = effectTextureLists[k].GetValue("name");
					Log.Debug("Loading EFFECT[" + effectName + "]");
					if (!this.EffectTextureLists.TryGetValue(effectName, out TUFXEffectTextureList etl))
					{
						this.EffectTextureLists[effectName] = etl = new TUFXEffectTextureList();
					}
					string[] names = effectTextureLists[k].values.DistinctNames();
					int len3 = names.Length;
					for (int m = 0; m < len3; m++)
					{
						string propName = names[m];
						if (propName == "name") { continue; }//don't load textures for the 'name=' entry in the configs
						Log.Debug("Loading Textures for property [" + propName + "]");
						string[] values = effectTextureLists[k].GetValues(propName);
						int len4 = values.Length;
						for (int r = 0; r < len4; r++)
						{
							string texName = values[r];
							Log.Debug("Loading Texture for name [" + texName + "]");
							Texture2D tex = GameDatabase.Instance.GetTexture(texName, false);
							if (tex != null)
							{
								if (!etl.ContainsTexture(propName, tex))
								{
									etl.AddTexture(propName, tex);
								}
								else
								{
									Log.Message("Ignoring duplicate texture: " + texName + " for effect: " + effectName + " property: " + propName);
								}
							}
							else
							{
								Log.Error("Texture specified by path: " + texName + " was not found when attempting to load textures for effect: " + effectName + " propertyName: " + propName);
							}
						}
					}
				}
			}
		}

		private void loadProfiles()
		{
			//discard the existing profile reference, if any
			currentProfile = null;
			//clear profiles in case of in-game reload
			Profiles.Clear();
			//grab all profiles detected in global scope config nodes, load them into local storage
			foreach (var profileConfig in GameDatabase.Instance.root.GetConfigs("TUFX_PROFILE"))
			{
				TUFXProfile profile = new TUFXProfile(profileConfig);
				if (!Profiles.ContainsKey(profile.ProfileName))
				{
					Profiles.Add(profile.ProfileName, profile);
				}
				else
				{
					Log.Error("TUFX Profiles already contains profile for name: " + profile.ProfileName + ".  This is the result of a configuration with" +
						" a duplicate name; please check your configurations and remove any duplicates.  Only the first configuration parsed for any one name will be loaded.");
				}
			}
			defaultConfigUrl = GameDatabase.Instance.GetConfigs(Configuration.NODE_NAME).FirstOrDefault();
			if (defaultConfigUrl != null)
			{
				ConfigNode.LoadObjectFromConfig(defaultConfiguration, defaultConfigUrl.config);
			}
		}

		/// <summary>
		/// Internal function to retrieve a shader from the dictionary, by name.  These names will include the package level prefixing, e.g. 'Unity/Foo/Bar/ShaderName'
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal Shader getShader(string name)
		{
			shaders.TryGetValue(name, out Shader s);
			return s;
		}

		/// <summary>
		/// Internal function to retrieve a compute shader from the dictionary, by name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal ComputeShader getComputeShader(string name)
		{
			computeShaders.TryGetValue(name, out ComputeShader s);
			return s;
		}

		/// <summary>
		/// Attempts to retrieve a built-in texture by name.  The list of built-in textures is limited to lens dirt and a few LUTs; only those textures that were included in the Unity Post Process Package.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal Texture2D getTexture(string name)
		{
			textures.TryGetValue(name, out Texture2D tex);
			return tex;
		}

		/// <summary>
		/// Returns true if the input texture is present in the list of built-in textures that were loaded from asset bundles.
		/// </summary>
		/// <param name="tex"></param>
		/// <returns></returns>
		internal bool isBuiltinTexture(Texture2D tex)
		{
			return textures.Values.Contains(tex);
		}

		/// <summary>
		/// Callback for when a scene has been fully loaded.
		/// </summary>
		/// <param name="scene"></param>
		private void onLevelLoaded(GameScenes gameScene)
		{
			Log.Debug("TUFXLoader - onLevelLoaded( " + gameScene + " )");

			CloseConfigGui();

			TUFXScene tufxScene = Utils.GetCurrentScene();
			string profileName = GetProfileNameForScene(tufxScene);
			TUFXProfile profile = GetProfileByName(profileName);
			ApplyProfile(profile, tufxScene);
		}

		private void cameraChange(CameraManager.CameraMode newCameraMode)
		{
			TUFXScene tufxScene = Utils.GetTUFXSceneForCameraMode(newCameraMode);
			string profileName = GetProfileNameForScene(tufxScene);
			TUFXProfile profile = GetProfileByName(profileName);
			ApplyProfile(profile, tufxScene);
		}

		// top-level function for switching to a different profile from the config gui
		internal void ChangeProfileForScene(string profileName, TUFXScene tufxScene)
		{
			if (tufxScene == TUFXScene.MainMenu)
			{
				defaultConfiguration.MainMenuProfile = profileName;
				if (defaultConfigUrl != null)
				{
					defaultConfigUrl.config = new ConfigNode(Configuration.NODE_NAME);
					ConfigNode.WriteObject(defaultConfiguration, defaultConfigUrl.config, 0);
					defaultConfigUrl.parent.SaveConfigs();
				}
			}
			else
			{
				HighLogic.CurrentGame.Parameters.CustomParams<TUFXGameSettings>().SetProfileName(profileName, tufxScene);
			}

			TUFXProfile profile = GetProfileByName(profileName);
			ApplyProfile(profile, tufxScene);
		}

		public string GetProfileNameForScene(TUFXScene tufxScene)
		{
			if (tufxScene == TUFXScene.MainMenu)
			{
				return defaultConfiguration.MainMenuProfile;
			}
			else
			{
				TUFXGameSettings settings = HighLogic.CurrentGame?.Parameters?.CustomParams<TUFXGameSettings>();

				if (settings != null)
				{
					return settings.GetProfileName(tufxScene);
				}
				else
				{
					return defaultConfiguration.GetProfileName(tufxScene);
				}
			}
		}

		private PostProcessLayer SetCameraHDR(Camera camera, bool hdr)
		{
			var layer = camera.gameObject.AddOrGetComponent<PostProcessLayer>();
			layer.Init(Resources);
			camera.allowHDR = hdr;
			return layer;
		}

		private void ApplyProfileToCamera(Camera camera, TUFXProfile tufxProfile, bool isFinalCamera, bool isPrimaryCamera)
		{
			if (camera == null) return;

			PostProcessLayer layer = SetCameraHDR(camera, tufxProfile.HDREnabled);

			// In general all postprocessing should be done on the "final" camera - local when in flight, internal when in IVA, etc.
			// But we don't want to apply TAA (and possibly motion blur and DoF) to anything but the local space camera since 
			// motion vectors aren't shared between cameras, and applying TAA on multiple cameras will produce smearing
			layer.volumeLayer = isFinalCamera ? 1 : 0;

			AntialiasingParameters aaParameters = isPrimaryCamera ? tufxProfile.Antialiasing : tufxProfile.SecondaryCameraAntialiasing;

			layer.antialiasingMode = aaParameters.Mode;
			layer.temporalAntialiasing = aaParameters.TemporalAntialiasing;
			layer.subpixelMorphologicalAntialiasing = aaParameters.SubpixelMorphologicalAntialiasing;
			layer.fastApproximateAntialiasing = aaParameters.FastApproximateAntialiasing;
		}

		private void ApplyProfile(TUFXProfile profile, TUFXScene tufxScene)
		{
			if (profile == null) return;

			currentProfile = profile;

			var bloomSettings = currentProfile.GetSettingsFor<Bloom>();
			if (currentProfile.HDREnabled && bloomSettings != null && bloomSettings.active)
			{
				QualitySettings.antiAliasing = 0;
			}

			mainVolume.sharedProfile = currentProfile.CreatePostProcessProfile();

			Camera galaxyCamera = ScaledCamera.Instance?.galaxyCamera;
			if (galaxyCamera != null)
			{
				SetCameraHDR(galaxyCamera, profile.HDREnabled);
			}

			if (tufxScene == TUFXScene.Editor)
			{
				var editorCameras = EditorCamera.Instance.cam.gameObject.GetComponentsInChildren<Camera>();
				foreach (var cam in editorCameras)
				{
					// don't apply to main camera since that will be done later
					if (!object.ReferenceEquals(cam, Camera.main))
					{
						ApplyProfileToCamera(cam, currentProfile, false, false);
					}
				}
			}
			bool scaledCameraIsPrimary = tufxScene == TUFXScene.Map || tufxScene == TUFXScene.TrackingStation;
			bool internalCameraIsFinal = tufxScene == TUFXScene.Internal;

			ApplyProfileToCamera(InternalCamera.Instance?.GetComponent<Camera>(), currentProfile, internalCameraIsFinal, false);
			ApplyProfileToCamera(ScaledCamera.Instance?.cam, currentProfile, scaledCameraIsPrimary, scaledCameraIsPrimary);
			ApplyProfileToCamera(Camera.main, currentProfile, !internalCameraIsFinal, !scaledCameraIsPrimary);
		}

		// called from the UI when HDR or antialiasing settings have changed; which need to change settings on the camera itself
		internal void RefreshCameras()
		{
			ApplyProfile(currentProfile, Utils.GetCurrentScene());
		}

		// returns the profile or default-empty
		private TUFXProfile GetProfileByName(string profileName)
		{
			if (Profiles.TryGetValue(profileName, out var profile))
			{
				return profile;
			}



			Log.Error($"Profile {profileName} not found; falling back to {Configuration.EMPTY_PROFILE_NAME}");
			return Profiles[Configuration.EMPTY_PROFILE_NAME];
		}

		/// <summary>
		/// Callback for when the ApplicationLauncher button is clicked.
		/// </summary>
		private void configGuiEnable()
		{
			if (configGUI == null)
			{
				configGUI = new GameObject("TUFX Config GUI").AddComponent<ConfigurationGUI>();
			}
		}

		/// <summary>
		/// Callback for when the ApplicationLauncher button is clicked.  Can also be called from within the GUI itself from a 'Close' button.
		/// </summary>
		private void configGuiDisable()
		{
			if (configGUI != null)
			{
				GameObject.Destroy(configGUI.gameObject);
				configGUI = null;
			}
		}

		internal void CloseConfigGui()
		{
			mainToolbarControl.SetFalse(true);
		}

		private void debugGuiEnable()
		{
#if DEBUG
			debugGUI = GetComponent<DebugGUI>();
			if (debugGUI == null)
			{
				debugGUI = this.gameObject.AddComponent<DebugGUI>();
			}
#endif
		}

		internal void debugGuiDisable()
		{
#if DEBUG
			if (debugGUI != null)
			{
				GameObject.Destroy(debugGUI);
				debugGUI = null;
			}
#endif
		}
	}
}
