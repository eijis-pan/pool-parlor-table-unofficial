#define MNBK_BACKOUT_PATCH

using System;
using UnityEngine;
using UnityEditor;

namespace EijisPoolParlorTableUtil
{
	public class StraightPackage
	{
		private static readonly string exportPackageFilePath = "PoolParlorTable_straight.unityPackage";
		static readonly string[] exportFilePaths = 
		{
			"Assets/eijis/Editor/PoolParlorTableUtil/Straight_table_setup.cs",

			"Assets/eijis/PoolParlorStraight.unity",
			
			"Assets/eijis/Fbx/【Free】ビリヤードVer.1.0【3Dモデル】（木製ラックフレーム改変） booth_items5248046.txt",
			"Assets/eijis/Fbx/Billiards/woodframerack.fbx",
			"Assets/eijis/Fbx/WoodFrame.png",

			"Assets/eijis/Materials/BallShadow_deny.mat",
 			"Assets/eijis/Materials/CallShotLockButtonBlue.mat",
			"Assets/eijis/Materials/CallShotLockButtonOrange.mat",
			"Assets/eijis/Materials/CallShotLockButtonWhite.mat",
			"Assets/eijis/Materials/DesktopAssets_callshot.mat",
			"Assets/eijis/Materials/InfoBoard_Straight_EN.mat",
			"Assets/eijis/Materials/InfoBoard_Straight_JP.mat",
			"Assets/eijis/Materials/mtable_standard_kitchenline.mat",
			"Assets/eijis/Materials/PocketMarkerBottom_Blue.mat",
			"Assets/eijis/Materials/PocketMarkerBottom_Orange.mat",
			"Assets/eijis/Materials/PocketMarker_Blue.mat",
			"Assets/eijis/Materials/PocketMarker_Orange.mat",
			"Assets/eijis/Materials/PocketMarker_White.mat",
			"Assets/eijis/Materials/point_blue.mat",
			"Assets/eijis/Materials/point_orange.mat",
			"Assets/eijis/Materials/point_penalty.mat",
			"Assets/eijis/Materials/point_star.mat",
			"Assets/eijis/Materials/racksheet.mat",
			"Assets/eijis/Materials/SpotMarker.mat",
			"Assets/eijis/Materials/SpotMarkerBall.mat",
			"Assets/eijis/Materials/target_blue.mat",
			"Assets/eijis/Materials/target_orange.mat",
			"Assets/eijis/Materials/target_white.mat",
			"Assets/eijis/Materials/UI_Straight.mat",
			"Assets/eijis/Materials/UI_Straight_Additive_CushionTouch.mat",
			"Assets/eijis/Materials/UI_Straight_Additive_CushionTouch2.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ForrowGuideline.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ForrowGuideline_bank.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallMarker.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallMarker_bank.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallShadow.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallShadow_bank.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/TargetGuideline.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/TargetGuideline_bank.mat",
			"Assets/eijis/Materials/WoodFrame.mat",
			
			"Assets/eijis/Prefab/DummyRack SimpleButton Toggle.prefab",
			"Assets/eijis/Prefab/DummyRack.prefab",
			"Assets/eijis/Prefab/ImageBall SimpleButton Toggle.prefab",
			"Assets/eijis/Prefab/ImageBall.prefab",
			"Assets/eijis/Prefab/ImageBallManager.prefab",
			"Assets/eijis/Prefab/ImageBallManager_bank.prefab",
			"Assets/eijis/Prefab/LanguageSwitch.prefab",
			"Assets/eijis/Prefab/InfoBoard_EN.prefab",
			"Assets/eijis/Prefab/InfoBoard_JP.prefab",
			"Assets/eijis/Prefab/InfoBoard_Straight_EN.prefab",
			"Assets/eijis/Prefab/InfoBoard_Straight_JP.prefab",
			"Assets/eijis/Prefab/Pool Parlor Table Straight.prefab",
			"Assets/eijis/Prefab/GameModeButton.prefab",
			"Assets/eijis/Prefab/ToggleButton.prefab",
			"Assets/eijis/Prefab/RackSheet.prefab",
			// "Assets/Billiards/WoodRackFrame.prefab",
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
			"Assets/eijis/Prefab/BilliardsModule/intl.controls/callShotLock.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.desktop/desktop/desktop_callshot.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/10Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/20Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/30Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/50Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/70Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/100Win.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/RackSheet.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/RackSheetToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/SemiAutoCallToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/WoodFrameToggle.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/WoodRackFrame.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.menu/SettingsMenu/Backboard_Straight.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_0.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_1.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_2.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_3.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_4.prefab",
			"Assets/eijis/Prefab/BilliardsModule/intl.table/model.regular/table_artwork/PointPocketMarker_5.prefab",
			"Assets/eijis/Prefab/ScoreScreen/PlayerRow.prefab",
			"Assets/eijis/Prefab/ScoreScreen/PointCell.prefab",
			"Assets/eijis/Prefab/ScoreScreen/ScoreScreen.prefab",
			"Assets/eijis/Prefab/ScoreScreen/ScoreScreenStraight.prefab",
			"Assets/eijis/Prefab/ScoreScreen/TeamPlayers.prefab",

			"Assets/eijis/Textures/BallShadow_x.png",
			"Assets/eijis/Textures/InfoBoard_straight_en.png",
			"Assets/eijis/Textures/InfoBoard_straight_jp.png",
			"Assets/eijis/Textures/racksheet.png",
			"Assets/eijis/Textures/tableDefault.png",
			"Assets/eijis/Textures/tdesktop_stuff_callshot.png",
			"Assets/eijis/Textures/UI_Straight.png",
			"Assets/eijis/Textures/PoolParlorImageBall/BallShadow_x2.png",
			"Assets/eijis/Textures/PoolParlorImageBall/ImageBallShadow.png",
			"Assets/eijis/Textures/100WinOff.psd",
			"Assets/eijis/Textures/100WinOn.psd",
			"Assets/eijis/Textures/20WinOff.psd",
			"Assets/eijis/Textures/20WinOn.psd",
			"Assets/eijis/Textures/30WinOff.psd",
			"Assets/eijis/Textures/30WinOn.psd",
			"Assets/eijis/Textures/50WinOff.psd",
			"Assets/eijis/Textures/50WinOn.psd",
			"Assets/eijis/Textures/70WinOff.psd",
			"Assets/eijis/Textures/70WinOn.psd",
			"Assets/eijis/Textures/RackSheetOff.psd",
			"Assets/eijis/Textures/RackSheettOn.psd",
			"Assets/eijis/Textures/SemiAutoCallOff.psd",
			"Assets/eijis/Textures/SemiAutoCallOn.psd",
			"Assets/eijis/Textures/WoodFrameOff.psd",
			"Assets/eijis/Textures/WoodFrameOn.psd",
			
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCallShotLock.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCallShotLock.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCueBallInKitchen.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonCueBallInKitchen.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonNextBallOnSpot.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonNextBallOnSpot.cs",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonRequestBreak.asset",
			"Assets/eijis/UdonScripts/BilliardsModuleAdditional/ButtonRequestBreak.cs",
			"Assets/eijis/UdonScripts/LanguageSwitch/LanguageSwitch.asset",
			"Assets/eijis/UdonScripts/LanguageSwitch/LanguageSwitch.cs",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallManager.asset",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallManager.cs",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallRepositioner.asset",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallRepositioner.cs",
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
		};
		
		[MenuItem("GameObject/TKCH/PoolParlor/ExportPackageStraight", false, 0)]
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
