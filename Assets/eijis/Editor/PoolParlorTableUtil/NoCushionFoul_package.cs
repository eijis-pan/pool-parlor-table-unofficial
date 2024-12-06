using System;
using UnityEngine;
using UnityEditor;

namespace EijisPoolParlorTableUtil
{
	public class NoCushionFoulPackage
	{
		private static readonly string exportPackageFilePath = "PoolParlorTable_noCushionFoul.unityPackage";
		static readonly string[] exportFilePaths = 
		{
			"Assets/eijis/Editor/PoolParlorTableUtil/NoCushionFoul_table_setup.cs",

			"Assets/eijis/PoolParlorNoCushionFoul.unity",
			"Assets/eijis/Materials/UI_Additive_CushionTouch.mat",
			"Assets/eijis/Prefab/Pool Parlor Table NoCushionFoul.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/CushionTouch.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/NoCushionFoulToggle.prefab",
			"Assets/eijis/Textures/UI_NoCushion.png",
			"Assets/eijis/Textures/NoCushionFoulOff.psd",
			"Assets/eijis/Textures/NoCushionFoulOn.psd",
			
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/BetaPhysicsManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/BilliardsModule.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/GraphicsManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/LegacyPhysicsManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/MenuManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/NetworkingManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/StandardPhysicsManager.cs",
			"Assets/metaphira/Modules/CameraOverrideModule/UdonScripts/CameraOverrideModule.cs",
		};
		
		[MenuItem("GameObject/TKCH/PoolParlor/ExportPackageNoCushionFoul", false, 0)]
		private static void ExportPackage_Menu(MenuCommand command)
		{
			try
			{
				Debug.Log("ExportPackage");

				AssetDatabase.ExportPackage(exportFilePaths, exportPackageFilePath, ExportPackageOptions.Default);

				EditorUtility.DisplayDialog ("Custom Script Result", "ExportPackage end", "OK");
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog ("Custom Script Exception", ex.ToString(), "OK");
			}
		}

	}
}
