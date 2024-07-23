using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUFX
{
	public class TUFXGameSettings : GameParameters.CustomParameterNode
	{

		public override string Title => "TUFX Profile Settings";

		public override string DisplaySection => "TUFX";

		public override string Section => "Scene Profiles";

		public override int SectionOrder => 1;

		public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

		public override bool HasPresets => false;

		public TUFXGameSettings()
		{
			var defaults = TexturesUnlimitedFXLoader.defaultConfiguration;
			FlightSceneProfile = defaults.FlightSceneProfile;
			IVAProfile = defaults.IVAProfile;
			MapSceneProfile = defaults.MapSceneProfile;
			EditorSceneProfile = defaults.EditorSceneProfile;
			SpaceCenterSceneProfile = defaults.SpaceCenterSceneProfile;
			TrackingStationProfile = defaults.TrackingStationProfile;
		}

		[GameParameters.CustomStringParameterUI("Flight Scene Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Flight Scene")]
		public string FlightSceneProfile;

		[GameParameters.CustomStringParameterUI("IVA Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile when in IVA")]
		public string IVAProfile;

		[GameParameters.CustomStringParameterUI("Map Scene Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Map Scene")]
		public string MapSceneProfile;

		[GameParameters.CustomStringParameterUI("Editor Scene Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Editor Scene")]
		public string EditorSceneProfile;

		[GameParameters.CustomStringParameterUI("Space Center Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Space Center Scene")]
		public string SpaceCenterSceneProfile;

		[GameParameters.CustomStringParameterUI("Tracking Station Profile: ", gameMode = GameParameters.GameMode.ANY, lines = 1, toolTip = "Active Profile in the Tracking Station Scene")]
		public string TrackingStationProfile;

		internal void SetProfileName(string profileName, TUFXScene scene)
		{
			switch (scene)
			{
				case TUFXScene.Flight: FlightSceneProfile = profileName; break;
				case TUFXScene.Internal: IVAProfile = profileName; break;
				case TUFXScene.Map: MapSceneProfile = profileName; break;
				case TUFXScene.Editor: EditorSceneProfile = profileName; break;
				case TUFXScene.SpaceCenter: SpaceCenterSceneProfile = profileName; break;
				case TUFXScene.TrackingStation: TrackingStationProfile = profileName; break;
			}
		}

		internal string GetProfileName(TUFXScene scene)
		{
			switch (scene)
			{
				case TUFXScene.Flight: return FlightSceneProfile;
				case TUFXScene.Internal: return IVAProfile;
				case TUFXScene.Map: return MapSceneProfile;
				case TUFXScene.Editor: return EditorSceneProfile;
				case TUFXScene.SpaceCenter: return SpaceCenterSceneProfile;
				case TUFXScene.TrackingStation: return TrackingStationProfile;
			}

			return string.Empty;
		}
	}
}
