#define MNBK_BACKOUT_PATCH

using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace EijisPoolParlorTableUtil
{
	public class RotationPackage
	{
		private static readonly string exportPackageFilePath = "PoolParlorTable_rotation.unityPackage";
		static readonly string[] exportFilePaths = 
		{
			"Assets/eijis/Editor/PoolParlorTableUtil/Rotation_table_setup.cs",

			"Assets/eijis/PoolParlorRotation.unity",
			
			"Assets/eijis/Fbx/【Free】ビリヤードVer.1.0【3Dモデル】（木製ラックフレーム改変） booth_items5248046.txt",
			"Assets/eijis/Fbx/Billiards/woodframerack.fbx",
			"Assets/eijis/Fbx/WoodFrame.png",

			// "Assets/eijis/Materials/BallShadow_deny.mat",
 			"Assets/eijis/Materials/CallShotLockButtonBlue.mat",
			"Assets/eijis/Materials/CallShotLockButtonOrange.mat",
			"Assets/eijis/Materials/CallShotLockButtonWhite.mat",
			"Assets/eijis/Materials/DesktopAssets_callshot.mat",
			"Assets/eijis/Materials/DesktopAssets_pushout.mat",
			"Assets/eijis/Materials/DesktopAssets_pushout_doing.mat",
			"Assets/eijis/Materials/DesktopAssets_safety.mat",
			"Assets/eijis/Materials/DesktopAssets_safetycalled.mat",
			"Assets/eijis/Materials/InfoBoard_Rotation_EN.mat",
			"Assets/eijis/Materials/InfoBoard_Rotation_JP.mat",
			"Assets/eijis/Materials/mtable_standard_kitchenline.mat",
			"Assets/eijis/Materials/PocketMarker_Blue.mat",
			"Assets/eijis/Materials/PocketMarker_Orange.mat",
			"Assets/eijis/Materials/PocketMarker_White.mat",
			"Assets/eijis/Materials/PocketMarkerBottom_Blue.mat",
			"Assets/eijis/Materials/PocketMarkerBottom_Orange.mat",
			// "Assets/eijis/Materials/point_blue.mat",
			// "Assets/eijis/Materials/point_orange.mat",
			// "Assets/eijis/Materials/point_penalty.mat",
			// "Assets/eijis/Materials/point_star.mat",
			"Assets/eijis/Materials/PushOut.mat",
			"Assets/eijis/Materials/PushOutDoing.mat",
			"Assets/eijis/Materials/racksheet.mat",
			"Assets/eijis/Materials/SpotMarker.mat",
			// "Assets/eijis/Materials/SpotMarkerBall.mat",
			"Assets/eijis/Materials/target_blue.mat",
			"Assets/eijis/Materials/target_orange.mat",
			"Assets/eijis/Materials/target_white.mat",
			"Assets/eijis/Materials/UI_Rotation.mat",
			"Assets/eijis/Materials/UI_Rotation_Additive_CushionTouch.mat",
			"Assets/eijis/Materials/UI_Rotation_Additive_CushionTouch2.mat",
			"Assets/eijis/Materials/UI_Rotation_Additive_NoCallPocket.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/ForrowGuideline.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/ForrowGuideline_bank.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/ImageBallMarker.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/ImageBallMarker_bank.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/ImageBallShadow.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/ImageBallShadow_bank.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/TargetGuideline.mat",
			// "Assets/eijis/Materials/PoolParlorImageBall/TargetGuideline_bank.mat",
			"Assets/eijis/Materials/WoodFrame.mat",
			
			// "Assets/eijis/Prefab/DummyRack SimpleButton Toggle.prefab",
			// "Assets/eijis/Prefab/DummyRack.prefab",
			// "Assets/eijis/Prefab/ImageBall SimpleButton Toggle.prefab",
			// "Assets/eijis/Prefab/ImageBall.prefab",
			// "Assets/eijis/Prefab/ImageBallManager.prefab",
			// "Assets/eijis/Prefab/ImageBallManager_bank.prefab",
			// "Assets/eijis/Prefab/LanguageSwitch.prefab",
			// "Assets/eijis/Prefab/InfoBoard_EN.prefab",
			// "Assets/eijis/Prefab/InfoBoard_JP.prefab",
			"Assets/eijis/Prefab/InfoBoard_Rotation_EN.prefab",
			"Assets/eijis/Prefab/InfoBoard_Rotation_JP.prefab",
			"Assets/eijis/Prefab/PointPocketMarker.prefab",
			"Assets/eijis/Prefab/Pool Parlor Table Rotation.prefab",
			"Assets/eijis/Prefab/GameModeButton.prefab",
			"Assets/eijis/Prefab/RackSheet.prefab",
			"Assets/eijis/Prefab/RequestBreak.prefab",
			"Assets/eijis/Prefab/ToggleButton.prefab",
			"Assets/eijis/Prefab/WoodRackFrame.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/calledBall_target.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/CushionTouch_0.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/CushionTouch_1.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/CushionTouch_2.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/CushionTouch_3.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/markerCenterSpot.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/markerFootSpot.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/markerHeadSpot.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/pocket_point_0.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/pocket_point_1.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/pocket_point_2.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/pocket_point_3.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/pocket_point_4.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/pocket_point_5.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/requestBreakBlue.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/requestBreakOrange.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.balls/special_penalty.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.controls/callSafety.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.controls/callShotLock.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.controls/pushOut.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.desktop/desktop/desktop_callshot.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.desktop/desktop/desktop_pushout.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.desktop/desktop/desktop_pushout_doing.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.desktop/desktop/desktop_safety.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.desktop/desktop/desktop_safety_called.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/60Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/90Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/120Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/180Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/240Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/Backboard_Rotation.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/CallPassOptionToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/PushOutToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/RackSheet.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/RackSheetToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/SemiAutoCallBallToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/SemiAutoCallPocketToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/SemiAutoCallToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/WoodFrameToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/WoodRackFrame.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_0.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_1.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_2.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_3.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_4.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_5.prefab",
			"Assets/eijis/Prefab/ScoreScreen/PlayerRow.prefab",
			"Assets/eijis/Prefab/ScoreScreen/PointCell.prefab",
			"Assets/eijis/Prefab/ScoreScreen/ScoreScreen.prefab",
			"Assets/eijis/Prefab/ScoreScreen/ScoreScreenRotation.prefab",
			"Assets/eijis/Prefab/ScoreScreen/TeamPlayers.prefab",

			// "Assets/eijis/Textures/BallShadow_x.png",
			// "Assets/eijis/Textures/PoolParlorImageBall/BallShadow_x2.png",
			// "Assets/eijis/Textures/PoolParlorImageBall/ImageBallShadow.png",
			"Assets/eijis/Textures/120WinOff.psd",
			"Assets/eijis/Textures/120WinOn.psd",
			"Assets/eijis/Textures/180WinOff.psd",
			"Assets/eijis/Textures/180WinOn.psd",
			"Assets/eijis/Textures/240WinOff.psd",
			"Assets/eijis/Textures/240WinOn.psd",
			"Assets/eijis/Textures/60WinOff.psd",
			"Assets/eijis/Textures/60WinOn.psd",
			"Assets/eijis/Textures/90WinOff.psd",
			"Assets/eijis/Textures/90WinOn.psd",
			"Assets/eijis/Textures/CallPassOptionOff.psd",
			"Assets/eijis/Textures/CallPassOptionOn.psd",
			"Assets/eijis/Textures/InfoBoard_rotation_en.png",
			"Assets/eijis/Textures/InfoBoard_rotation_jp.png",
			"Assets/eijis/Textures/racksheet.png",
			"Assets/eijis/Textures/RackSheetOff.psd",
			"Assets/eijis/Textures/RackSheettOn.psd",
			"Assets/eijis/Textures/SemiAutoCallBallOff.psd",
			"Assets/eijis/Textures/SemiAutoCallBallOn.psd",
			"Assets/eijis/Textures/SemiAutoCallOff.psd",
			"Assets/eijis/Textures/SemiAutoCallOn.psd",
			"Assets/eijis/Textures/SemiAutoCallPocketOff.psd",
			"Assets/eijis/Textures/SemiAutoCallPocketOn.psd",
			"Assets/eijis/Textures/tableDefault.png",
			"Assets/eijis/Textures/tdesktop_stuff_call_safety.png",
			"Assets/eijis/Textures/tdesktop_stuff_callshot.png",
			"Assets/eijis/Textures/tdesktop_stuff_pushout.png",
			"Assets/eijis/Textures/tdesktop_stuff_pushout_doing.png",
			"Assets/eijis/Textures/tdesktop_stuff_safety_called.png",
			"Assets/eijis/Textures/UI_Rotation.png",
			"Assets/eijis/Textures/WoodFrameOff.psd",
			"Assets/eijis/Textures/WoodFrameOn.psd",
			
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCallSafety.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCallSafety.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCallShotLock.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCallShotLock.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCueBallInKitchen.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCueBallInKitchen.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonNextBallOnSpot.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonNextBallOnSpot.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonPushOut.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonPushOut.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonRequestBreak.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonRequestBreak.cs",
			// "Assets/eijis/UdonScripts/LanguageSwitch/LanguageSwitch.asset",
			// "Assets/eijis/UdonScripts/LanguageSwitch/LanguageSwitch.cs",
			// "Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallManager.asset",
			// "Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallManager.cs",
			// "Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallRepositioner.asset",
			// "Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallRepositioner.cs",
			"Assets/eijis/UdonScripts/ScoreScreen/BilliardsScoreScreen.asset",
			"Assets/eijis/UdonScripts/ScoreScreen/BilliardsScoreScreen.cs",
			"Assets/eijis/UdonScripts/ScoreScreen/PlayerRow.asset",
			"Assets/eijis/UdonScripts/ScoreScreen/PlayerRow.cs",
			"Assets/eijis/UdonScripts/ScoreScreen/TeamPlayers.asset",
			"Assets/eijis/UdonScripts/ScoreScreen/TeamPlayers.cs",

			"Assets/metaphira/Modules/BilliardsModule/Materials/InfoBoard_JP.mat",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/BetaPhysicsManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/BilliardsModule.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/DesktopManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/GraphicsManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/LegacyPhysicsManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/MenuManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/NetworkingManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/StandardPhysicsManager.cs",
			"Assets/metaphira/Modules/BilliardsModule/UdonScripts/UIButton.cs",
			"Assets/metaphira/Modules/CameraOverrideModule/UdonScripts/CameraOverrideModule.cs",
		};
		
		[MenuItem("GameObject/TKCH/PoolParlor/ExportPackageRotation", false, 2)]
		private static void ExportPackage_Menu(MenuCommand command)
		{
			try
			{
				Debug.Log("ExportPackage");

				
				var sb = new StringBuilder();
				foreach (var exportFilePath in exportFilePaths)
				{
					if (!File.Exists(exportFilePath))
					{
						sb.AppendLine(exportFilePath);
						Debug.LogWarning("ファイルが見つかりません。 " + exportFilePath);
					}
				}

				bool cancel = false;
				if (0 < sb.Length)
				{
					cancel = EditorUtility.DisplayDialog("Custom Script Warning",
						"Export file(s) nod found.\n" + sb.ToString(), "Ignore", "Cancel");
				}

				if (!cancel)
				{
					AssetDatabase.ExportPackage(exportFilePaths, exportPackageFilePath, ExportPackageOptions.Default);
				}

				EditorUtility.DisplayDialog ("Custom Script Result", "ExportPackage end", "OK");
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog ("Custom Script Exception", ex.ToString(), "OK");
			}
		}

	}
}
