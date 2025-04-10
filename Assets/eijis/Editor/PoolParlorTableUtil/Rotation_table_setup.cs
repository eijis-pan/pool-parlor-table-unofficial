using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UdonSharp;
using VRC.Udon;

namespace EijisPoolParlorTableUtil
{
	public class RotationTableSetup : EditorWindow
	{
		private BilliardsModule _BilliardsModule;
		private BilliardsScoreScreen _scoreScreen;
		// private GameObject _woodFrameFbx;
		private Mesh _woodFrameMesh;
		
		private Vector2 _scrollPosition = Vector2.zero;

		private static readonly string EijisAssetPathBase = "Assets/eijis/Prefab/BilliardsModule";
		public static readonly Dictionary<string, string[]> AdditionalPrefabs = new Dictionary<string, string[]>
		{
			{
				"intl.balls",
				new []
				{
					"CushionTouch_0",
					"CushionTouch_1",
					"CushionTouch_2",
					"CushionTouch_3",
					"markerHeadSpot",
					"markerFootSpot",
					"markerCenterSpot",
					"requestBreakOrange",
					"requestBreakBlue",
					"calledBall_target"
				}
			},
			{
				"intl.controls",
				new []
				{
					"callShotLock",
					"pushOut",
					"callSafety"
				}
			},
			{
				"intl.desktop/desktop",
				new []
				{
					"desktop_callshot",
					"desktop_pushout",
					"desktop_pushout_doing",
					"desktop_safety",
					"desktop_safety_called"
				}
			},
			{
				"intl.menu/SettingsMenu",
				new []
				{
					"Goals6",
					"Goals9",
					"Goals15",
					// "60Win",
					// "90Win",
					// "120Win",
					// "180Win",
					// "240Win",
					"6Balls",
					"9Balls",
					"10Balls",
					"15Balls",
					"RackSheetToggle",
					"WoodFrameToggle",
					"PushOutToggle",
					"CallShotToggle",
					"SemiAutoCallToggle",
					"CallPassOptionToggle",
					"RackSheet",
					"WoodRackFrame",
					"Backboard_Rotation"
				}
			},
			{
				"intl.table/model.regular/table_artwork",
				new []
				{
					"PointPocketMarker_0",
					"PointPocketMarker_1",
					"PointPocketMarker_2",
					"PointPocketMarker_3",
					"PointPocketMarker_4",
					"PointPocketMarker_5"
				}
			}
		};

		public static readonly Dictionary<string, string> SetBilliardsModuleGameobjectProperties = new Dictionary<string, string>
		{
			{ "markerCalledBall", "intl.balls/calledBall_target" },
			{ "markerHeadSpot", "intl.balls/markerHeadSpot" },
			{ "markerCenterSpot", "intl.balls/markerCenterSpot" },
			{ "markerFootSpot", "intl.balls/markerFootSpot" },
			{ "requestBreakOrange", "intl.balls/requestBreakOrange" },
			{ "requestBreakBlue", "intl.balls/requestBreakBlue" }
		};
		
		public static readonly Dictionary<string, string> SetBilliardsModuleMaterialProperties = new Dictionary<string, string>
		{
			{ "calledBallMarkerBlue", "Assets/eijis/Materials/target_blue.mat" },
			{ "calledBallMarkerOrange", "Assets/eijis/Materials/target_orange.mat" },
			{ "calledBallMarkerWhite", "Assets/eijis/Materials/target_white.mat" }
		};

		public static readonly Dictionary<string, string> SetDesktopManagerGameObjectProperties = new Dictionary<string, string>
		{
			{ "callShot", "intl.desktop/desktop/desktop_callshot" },
			{ "safety", "intl.desktop/desktop/desktop_safety" },
			{ "safetyCalled", "intl.desktop/desktop/desktop_safety_called" },
			{ "pushOut", "intl.desktop/desktop/desktop_pushout" },
			{ "pushOutDoing", "intl.desktop/desktop/desktop_pushout_doing" }
		};

		public static readonly Dictionary<string, string[]> SetGraphicsManagerGameObjectArrayProperties = new Dictionary<string, string[]>
		{
			{ 
				"cushionTouch", 
				new []
				{
					"intl.balls/CushionTouch_0",
					"intl.balls/CushionTouch_1",
					"intl.balls/CushionTouch_2",
					"intl.balls/CushionTouch_3"
				}
			}
		};

		public static readonly Dictionary<string, string> SetGraphicsManagerGameObjectProperties = new Dictionary<string, string>
		{
			// { 
			// 	"specialPenalty", "intl.balls/special_penalty"
			// }
		};

		public static readonly Dictionary<string, string> SetGraphicsManagerMaterialProperties = new Dictionary<string, string>
		{
			{ "calledPocketCalledBlue", "Assets/eijis/Materials/PocketMarker_Blue.mat" },
			{ "calledPocketCalledOrange", "Assets/eijis/Materials/PocketMarker_Orange.mat" },
			{ "calledPocketCalledWhite", "Assets/eijis/Materials/PocketMarker_White.mat" },
			{ "calleShotLockBlue", "Assets/eijis/Materials/CallShotLockButtonBlue.mat" },
			{ "calleShotLockOrange", "Assets/eijis/Materials/CallShotLockButtonOrange.mat" },
			{ "calleShotLockWhite", "Assets/eijis/Materials/CallShotLockButtonWhite.mat" },
			{ "pushOutDont", "Assets/eijis/Materials/PushOut.mat" },
			{ "pushOutDoing", "Assets/eijis/Materials/PushOutDoing.mat" }
		};

		public static readonly Dictionary<string, MeshAssetInfo> SetGraphicsManagerMeshProperties = new Dictionary<string, MeshAssetInfo>
		{
			// { "pocketPointMeshPlus", new MeshAssetInfo("Assets/metaphira/Modules/BilliardsModule/Mesh/4BallPoints.fbx", "plus1") },
			// { "pocketPointMeshMinus", new MeshAssetInfo("Assets/metaphira/Modules/BilliardsModule/Mesh/4BallPoints.fbx", "munus1") }
		};

		public class MeshAssetInfo
		{
			public string assetPath;
			public string meshName;

			public MeshAssetInfo(string assetPath, string meshName)
			{
				this.assetPath = assetPath;
				this.meshName = meshName;
			}
		}

		public static readonly Dictionary<string, string> SetMenuManagerUIButtonProperties = new Dictionary<string, string>
		{
			{ "button40Win", "intl.menu/SettingsMenu/Goals6/40Win" },
			// { "button60Win", "intl.menu/SettingsMenu/60Win" },
			// { "button90Win", "intl.menu/SettingsMenu/90Win" },
			// { "button120Win", "intl.menu/SettingsMenu/120Win" },
			// { "button180Win", "intl.menu/SettingsMenu/180Win" },
			{ "button240Win", "intl.menu/SettingsMenu/Goals15/240Win" },
			{ "rotation6ButtonsGroup", "intl.menu/SettingsMenu/Goals6" },
			{ "rotation9ButtonsGroup", "intl.menu/SettingsMenu/Goals9" },
			{ "rotation15ButtonsGroup", "intl.menu/SettingsMenu/Goals15" },
			{ "button6BallsToggle", "intl.menu/SettingsMenu/6Balls" },
			{ "button9BallsToggle", "intl.menu/SettingsMenu/9Balls" },
			{ "button10BallsToggle", "intl.menu/SettingsMenu/10Balls" },
			{ "button15BallsToggle", "intl.menu/SettingsMenu/15Balls" },
			{ "buttonRackSheetToggle", "intl.menu/SettingsMenu/RackSheetToggle" },
			{ "buttonWoodFrameToggle", "intl.menu/SettingsMenu/WoodFrameToggle" },
			{ "buttonPushOutToggle", "intl.menu/SettingsMenu/PushOutToggle" },
			{ "buttonCallShotToggle", "intl.menu/SettingsMenu/CallShotToggle" },
			{ "buttonSemiAutoCallToggle", "intl.menu/SettingsMenu/SemiAutoCallToggle" },
			{ "buttonCallPassOptionToggle", "intl.menu/SettingsMenu/CallPassOptionToggle" }
		};

		public static readonly Dictionary<string, string[]> SetMenuManagerUIButtonArrayProperties = new Dictionary<string, string[]>
		{
			{ 
				"button60Win", 
				new []
				{
					"intl.menu/SettingsMenu/Goals6/60Win",
					"intl.menu/SettingsMenu/Goals9/60Win"
				}
			},
			{ 
				"button90Win", 
				new []
				{
					"intl.menu/SettingsMenu/Goals6/90Win",
					"intl.menu/SettingsMenu/Goals9/90Win",
					"intl.menu/SettingsMenu/Goals15/90Win"
				}
			},
			{ 
				"button120Win", 
				new []
				{
					"intl.menu/SettingsMenu/Goals6/120Win",
					"intl.menu/SettingsMenu/Goals9/120Win",
					"intl.menu/SettingsMenu/Goals15/120Win"
				}
			},
			{ 
				"button180Win", 
				new []
				{
					"intl.menu/SettingsMenu/Goals9/180Win",
					"intl.menu/SettingsMenu/Goals15/180Win"
				}
			}
		};

		private static readonly Type[] SetTableReferenceComponents =
		{
			typeof(ButtonCallShotLock)
		};
		
		private static readonly Type[] SetCallbackReferenceComponents =
		{
			typeof(UIButton)
		};

		private static readonly string[] SetDisableGameObjects = new[]
		{
			// "intl.table/model.regular/table_artwork/PointPocketMarker_0",
			// "intl.table/model.regular/table_artwork/PointPocketMarker_1",
			// "intl.table/model.regular/table_artwork/PointPocketMarker_2",
			// "intl.table/model.regular/table_artwork/PointPocketMarker_3",
			// "intl.table/model.regular/table_artwork/PointPocketMarker_4",
			// "intl.table/model.regular/table_artwork/PointPocketMarker_5",
			"intl.menu/SettingsMenu/Backboard",
			"intl.menu/SettingsMenu/8Ball",
			"intl.menu/SettingsMenu/9Ball",
			"intl.menu/SettingsMenu/4Ball",
			"intl.menu/SettingsMenu/4BallJP",
			"intl.menu/SettingsMenu/4BallKR",
			// "intl.controls/callShotLock"
		};

		public static readonly Dictionary<string, string> ReplaceMaterials = new Dictionary<string, string>
		{
			{ "intl.table/model.regular/table_artwork/table", "Assets/eijis/Materials/mtable_standard_kitchenline.mat" }
		};

		private List<HelpBoxInfo> _helpBoxInfos = new List<HelpBoxInfo>(3);

		public class HelpBoxInfo
		{
			public string message;
			public MessageType messageType;
			public bool wide;

			public HelpBoxInfo(string message, MessageType messageType, bool wide)
			{
				this.message = message;
				this.messageType = messageType;
				this.wide = wide;
			}
		}

		[MenuItem("CONTEXT/BilliardsModule/Eijis_RotationTableSetup")]
		private static void Eijis_RotationTableSetup_Menu(MenuCommand command)
		{
			try
			{
				BilliardsModule billiardsModule = (BilliardsModule)command.context;

				var window = CreateInstance<RotationTableSetup>();
				window._BilliardsModule = billiardsModule;
				
				var serializedObject = new SerializedObject(billiardsModule);
				serializedObject.Update();
				window._scoreScreen = (BilliardsScoreScreen)serializedObject.FindProperty("scoreScreen").objectReferenceValue;
				// window._scoreScreen = billiardsModule.GetComponentInChildren<BilliardsScoreScreen>();
				window.ShowUtility();
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Custom Script Exception", ex.ToString(), "OK");
			}
		}
		
		private void OnGUI()
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("対応させる scoreScreen を設定してください。");
			_scoreScreen = (BilliardsScoreScreen)EditorGUILayout.ObjectField("scoreScreen", _scoreScreen, typeof(BilliardsScoreScreen), true);
			EditorGUILayout.Space();
			// EditorGUILayout.LabelField("Billiards の Assets から woodframerack（ラックフレーム）のmeshを設定してください。");
			// _woodFrameMesh = (Mesh)EditorGUILayout.ObjectField("WoodFrameRack(mesh)", _woodFrameMesh, typeof(Mesh), false);
			// EditorGUILayout.Space();
			
			if (GUILayout.Button("開始",GUILayout.Height(30f)))
			{
				_helpBoxInfos.Clear();

				Debug.Log("Eijis_RotationTableSetup 開始");
				Undo.RecordObject(_scoreScreen, "開始");

				Eijis_RotationTableSetup();
				FixupMenuButtons(_BilliardsModule);
				/*
				if (!ReferenceEquals(null, _woodFrameMesh))
				{
					FixupWoodRackFrameMesh(_BilliardsModule, _woodFrameMesh);
				}
				*/
				
				//EditorUtility.SetDirty(_scoreBoard);
				var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
				//UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

				Debug.Log("Eijis_RotationTableSetup 終了");
			}
			
			if (GUILayout.Button("閉じる",GUILayout.Height(30f)))
			{
				Close();
			}

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			
			foreach (var helpBoxInfo in _helpBoxInfos)
			{
				EditorGUILayout.HelpBox(helpBoxInfo.message, helpBoxInfo.messageType, helpBoxInfo.wide);
			}
			
			EditorGUILayout.EndScrollView();
		}

		private void Eijis_RotationTableSetup()
		{
			int warnCount = 0;
			int errCount = 0;

			//_BilliardsModule.scoreBoard = _scoreBoard;
			//_BilliardsModule.SetProgramVariable("scoreBoard", _scoreBoard);
			var serializedObject = new SerializedObject(_BilliardsModule);
			serializedObject.Update();
			// serializedObject.FindProperty("defaultGameModeToRotation").boolValue = true;

			if (ReferenceEquals(null, _scoreScreen))
			{
				_helpBoxInfos.Add(new HelpBoxInfo("_scoreScreen が指定されていません。", MessageType.Warning, true));
				warnCount++;
			}
			else
			{
				serializedObject.FindProperty("scoreScreen").objectReferenceValue = _scoreScreen;
				
				_helpBoxInfos.Add(new HelpBoxInfo(
					$"{_scoreScreen.name} のオブジェクト参照を {_BilliardsModule.name} の scoreScreen プロパティに設定しました。", 
					MessageType.Info, true));
			}

			foreach (var kvp in AdditionalPrefabs)
			{
				var parent = _BilliardsModule.transform.Find(kvp.Key);
				if (ReferenceEquals(null, parent))
				{
					_helpBoxInfos.Add(new HelpBoxInfo($"{_BilliardsModule.name} に {kvp.Key} が見つかりません。", MessageType.Error, true));
					errCount++;
					continue;						
				}
				foreach (var prefabName in kvp.Value)
				{
					var exist = parent.Find(prefabName);
					if (!ReferenceEquals(null, exist))
					{
						_helpBoxInfos.Add(new HelpBoxInfo($"既に {_BilliardsModule.name} に {kvp.Key}/{prefabName} が存在します。（スキップ）", MessageType.Warning, true));
						warnCount++;
						continue;						
					}
					
					var assetPath = String.Join("/", new string[] {EijisAssetPathBase, kvp.Key, prefabName + ".prefab"});
					var go = LoadPrefabAsset(assetPath);
					if (ReferenceEquals(null, go))
					{
						_helpBoxInfos.Add(new HelpBoxInfo($"アセット {assetPath} のロードに失敗しました。", MessageType.Error, true));
						errCount++;
						continue;						
					}
					var localPosition = go.transform.localPosition;
					var localRotation = go.transform.localRotation;
					var localScale = go.transform.localScale;
					go.transform.parent = parent;
					go.transform.localPosition = localPosition;
					go.transform.localRotation = localRotation;
					go.transform.localScale = localScale;

					SetBilliardsModuleReference(go, _BilliardsModule);
					// Debug.Log(menuManager);
					SetMenuManagerReference(go, _BilliardsModule.menuManager);
						
					_helpBoxInfos.Add(new HelpBoxInfo(
						$"アセット {assetPath} を {_BilliardsModule.name} の {kvp.Key} に配置しました。", 
						MessageType.Info, true));
				}
			}
				
			foreach (var kvp in SetBilliardsModuleGameobjectProperties)
			{
				var tr = _BilliardsModule.transform.Find(kvp.Value);
				if (ReferenceEquals(null, tr))
				{
					continue;
				}

				serializedObject.FindProperty(kvp.Key).objectReferenceValue = tr.gameObject;
			}

			foreach (var kvp in SetBilliardsModuleMaterialProperties)
			{
				var material = AssetDatabase.LoadAssetAtPath<Material>(kvp.Value);
				if (ReferenceEquals(null, material))
				{
					continue;
				}

				serializedObject.FindProperty(kvp.Key).objectReferenceValue = material;
			}

			serializedObject.ApplyModifiedProperties();

			var desktopManagerSerializedObject = new SerializedObject(_BilliardsModule.desktopManager);
			desktopManagerSerializedObject.Update();
			foreach (var kvp in SetDesktopManagerGameObjectProperties)
			{
				var tr = _BilliardsModule.transform.Find(kvp.Value);
				if (ReferenceEquals(null, tr))
				{
					continue;
				}

				desktopManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = tr.gameObject;
			}
			desktopManagerSerializedObject.ApplyModifiedProperties();
			
			var graphicsManagerSerializedObject = new SerializedObject(_BilliardsModule.graphicsManager);
			graphicsManagerSerializedObject.Update();
			foreach (var kvp in SetGraphicsManagerGameObjectArrayProperties)
			{
				var arrayProp = graphicsManagerSerializedObject.FindProperty(kvp.Key);
				if (ReferenceEquals(null, arrayProp) /* || arrayProp.arraySize == 0 */ )
				{
					continue;
				}

				arrayProp.arraySize = kvp.Value.Length;
				//GameObject[] gameObjectArray = new GameObject[kvp.Value.Length];
				int gameObjectArrayIndex = 0;
				foreach (var path in kvp.Value)
				{
					var tr = _BilliardsModule.transform.Find(path);
					if (ReferenceEquals(null, tr))
					{
						continue;
					}

					//gameObjectArray[gameObjectArrayIndex++] = tr.gameObject;
					var itemProp = arrayProp.GetArrayElementAtIndex(gameObjectArrayIndex++);
					if (ReferenceEquals(null, itemProp))
					{
						break;
					}
					itemProp.objectReferenceValue = tr.gameObject;
				}
				//graphicsManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = gameObjectArray;
			}
			foreach (var kvp in SetGraphicsManagerGameObjectProperties)
			{
				var tr = _BilliardsModule.transform.Find(kvp.Value);
				if (ReferenceEquals(null, tr))
				{
					continue;
				}

				graphicsManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = tr.gameObject;
			}
			foreach (var kvp in SetGraphicsManagerMaterialProperties)
			{
				var material = AssetDatabase.LoadAssetAtPath<Material>(kvp.Value);
				if (ReferenceEquals(null, material))
				{
					continue;
				}

				graphicsManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = material;
			}
			foreach (var kvp in SetGraphicsManagerMeshProperties)
			{
				var meshAssetInfo = kvp.Value;
				var assets = AssetDatabase.LoadAllAssetsAtPath(meshAssetInfo.assetPath);
				if (ReferenceEquals(null, assets))
				{
					continue;
				}

				foreach (var asset in assets)
				{
					if (asset.GetType() == typeof(Mesh) && asset.name == meshAssetInfo.meshName)
					{
						graphicsManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = asset;
						break;
					}
				}
			}
			graphicsManagerSerializedObject.ApplyModifiedProperties();
			
			var menuManagerSerializedObject = new SerializedObject(_BilliardsModule.menuManager);
			menuManagerSerializedObject.Update();
			foreach (var kvp in SetMenuManagerUIButtonProperties)
			{
				var tr = _BilliardsModule.transform.Find(kvp.Value);
				if (ReferenceEquals(null, tr))
				{
					continue;
				}

				menuManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = tr.gameObject;
			}
			foreach (var kvp in SetMenuManagerUIButtonArrayProperties)
			{
				var arrayProp = menuManagerSerializedObject.FindProperty(kvp.Key);
				if (ReferenceEquals(null, arrayProp) /* || arrayProp.arraySize == 0 */ )
				{
					continue;
				}

				arrayProp.arraySize = kvp.Value.Length;
				//GameObject[] gameObjectArray = new GameObject[kvp.Value.Length];
				int gameObjectArrayIndex = 0;
				foreach (var path in kvp.Value)
				{
					var tr = _BilliardsModule.transform.Find(path);
					if (ReferenceEquals(null, tr))
					{
						continue;
					}

					//gameObjectArray[gameObjectArrayIndex++] = tr.gameObject;
					var itemProp = arrayProp.GetArrayElementAtIndex(gameObjectArrayIndex++);
					if (ReferenceEquals(null, itemProp))
					{
						break;
					}
					itemProp.objectReferenceValue = tr.gameObject;
				}
				//graphicsManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = gameObjectArray;
			}
			menuManagerSerializedObject.ApplyModifiedProperties();
			
			foreach (var disableGameObject in SetDisableGameObjects)
			{
				var tr = _BilliardsModule.transform.Find(disableGameObject);
				if (ReferenceEquals(null, tr))
				{
					continue;
				}
				tr.gameObject.SetActive(false);
			}

			foreach (var kvp in ReplaceMaterials)
			{
				var tr = _BilliardsModule.transform.Find(kvp.Key);
				var material = AssetDatabase.LoadAssetAtPath<Material>(kvp.Value);
				if (ReferenceEquals(null, tr) || ReferenceEquals(null, material))
				{
					continue;
				}
				var mr = tr.GetComponent<MeshRenderer>();
				if (ReferenceEquals(null, mr))
				{
					continue;
				}
				var materials = mr.sharedMaterials;
				if (ReferenceEquals(null, materials) || materials.Length < 1)
				{
					continue;
				}
				materials[0] = material;
				mr.sharedMaterials = materials;
			}

			EditorUtility.DisplayDialog("Custom Script Result", 
				"Eijis_RotationTableSetup を完了しました。" + (0 < warnCount ? "（警告あり）" : "") + (0 < errCount ? "（エラーあり）" : ""), 
				"OK");
		}

		private GameObject LoadPrefabAsset (string assetPath)
		{
			var loadedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			return (GameObject)PrefabUtility.InstantiatePrefab(loadedAsset);
		}

		private void SetBilliardsModuleReference(GameObject go, BilliardsModule billiardsModule)
		{
			foreach (var type in SetTableReferenceComponents)
			{
				Component c = go.GetComponent(type);
				if (!ReferenceEquals(null, c) && c.GetType().IsSubclassOf(typeof(UdonSharpBehaviour)))
				{
					((UdonSharpBehaviour)c).SetProgramVariable("table", billiardsModule);
				}
			}
		}
		
		private void SetMenuManagerReference(GameObject go, MenuManager menuManager)
		{
			foreach (var type in SetCallbackReferenceComponents)
			{
				Component[] components = go.GetComponentsInChildren(type);
				foreach (var c in components)
				{
					if (!ReferenceEquals(null, c) && c.GetType().IsSubclassOf(typeof(UdonSharpBehaviour)))
					{
						((UdonSharpBehaviour)c).SetProgramVariable("callback", menuManager.transform.GetComponent<UdonBehaviour>());
					}
				}
			}
		}
		
		private static Color _desktopOutline = new Color(0x93 / 255f, 0xBD / 255f, 0xEC / 255f);

		private class MenuButtonDef
		{
			public string TextureName { set; get; }
			public Color DesktopOutline { set; get; }
		}

		static readonly string buttonAssetPath = "Assets/metaphira/Modules/BilliardsModule/Textures/Buttons/";
		static readonly string buttonAssetPathEijis = "Assets/eijis/Textures/";
		static readonly int _MainTexId = Shader.PropertyToID("_MainTex");
		static readonly int _ColorId = Shader.PropertyToID("_Color");

		[MenuItem("CONTEXT/BilliardsModule/Eijis_FixupMenuButtons")]
		private static void Eijis_FixupMenuButtons_Menu(MenuCommand command)
		{
			try
			{
				BilliardsModule billiardsModule = (BilliardsModule)command.context;
				FixupMenuButtons(billiardsModule);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Custom Script Exception", ex.ToString(), "OK");
			}
		}
		
		private static void FixupMenuButtons(BilliardsModule billiardsModule)
		{
            var menuButtonDefDict = new Dictionary<string, MenuButtonDef>
            {
                { "LeaveButton", new MenuButtonDef { TextureName = "Leave.psd", DesktopOutline = _desktopOutline }},
                { "PlayButton", new MenuButtonDef { TextureName = "Play.psd", DesktopOutline = _desktopOutline }},
                { "JoinOrange", new MenuButtonDef { TextureName = "JoinOrange.psd", DesktopOutline = _desktopOutline }},
                { "JoinBlue", new MenuButtonDef { TextureName = "JoinBlue.psd", DesktopOutline = _desktopOutline }},
                { "8Ball", new MenuButtonDef { TextureName = "8BallOff.psd", DesktopOutline = _desktopOutline }},
                { "9Ball", new MenuButtonDef { TextureName = "9BallOff.psd", DesktopOutline = _desktopOutline }},
                { "4Ball", new MenuButtonDef { TextureName = "4BallOff.psd", DesktopOutline = _desktopOutline }},
                { "4BallJP", new MenuButtonDef { TextureName = "4BallJapaneseOff.psd", DesktopOutline = _desktopOutline }},
                { "4BallKR", new MenuButtonDef { TextureName = "4BallKoreanOff.psd", DesktopOutline = _desktopOutline }},
                { "TeamsToggle", new MenuButtonDef { TextureName = "TeamsOff.psd", DesktopOutline = _desktopOutline }},
                { "GuidelineToggle", new MenuButtonDef { TextureName = "GuidelineOff.psd", DesktopOutline = _desktopOutline }},
                { "LockingToggle", new MenuButtonDef { TextureName = "LockingOff.psd", DesktopOutline = _desktopOutline }},
                { "TimeRight", new MenuButtonDef { TextureName = "TriangleOnTemplate.psd", DesktopOutline = _desktopOutline }},
                { "TimeLeft", new MenuButtonDef { TextureName = "TriangleOnTemplate.psd", DesktopOutline = _desktopOutline }},
                { "StartButton", new MenuButtonDef { TextureName = "Play.psd", DesktopOutline = _desktopOutline }},
                { "6Balls", new MenuButtonDef { TextureName = "6BallsOff.psd", DesktopOutline = _desktopOutline }},
                { "9Balls", new MenuButtonDef { TextureName = "9BallsOff.psd", DesktopOutline = _desktopOutline }},
                { "10Balls", new MenuButtonDef { TextureName = "10BallsOff.psd", DesktopOutline = _desktopOutline }},
                { "15Balls", new MenuButtonDef { TextureName = "15BallsOff.psd", DesktopOutline = _desktopOutline }},
                { "RackSheetToggle", new MenuButtonDef { TextureName = "RackSheetOff.psd", DesktopOutline = _desktopOutline }},
                { "WoodFrameToggle", new MenuButtonDef { TextureName = "WoodFrameOff.psd", DesktopOutline = _desktopOutline }},
                { "PushOutToggle", new MenuButtonDef { TextureName = "PushOutOff.psd", DesktopOutline = _desktopOutline }},
                { "CallShotToggle", new MenuButtonDef { TextureName = "CallShotOff.psd", DesktopOutline = _desktopOutline }},
                { "SemiAutoCallToggle", new MenuButtonDef { TextureName = "SemiAutoCallOff.psd", DesktopOutline = _desktopOutline }},
                { "SemiAutoCallBallToggle", new MenuButtonDef { TextureName = "SemiAutoCallBallOff.psd", DesktopOutline = _desktopOutline }},
                { "SemiAutoCallPocketToggle", new MenuButtonDef { TextureName = "SemiAutoCallPocketOff.psd", DesktopOutline = _desktopOutline }},
                { "CallPassOptionToggle", new MenuButtonDef { TextureName = "CallPassOptionOff.psd", DesktopOutline = _desktopOutline }},
                { "40Win", new MenuButtonDef { TextureName = "40WinOff.psd", DesktopOutline = _desktopOutline }},
                { "60Win", new MenuButtonDef { TextureName = "60WinOff.psd", DesktopOutline = _desktopOutline }},
                { "90Win", new MenuButtonDef { TextureName = "90WinOff.psd", DesktopOutline = _desktopOutline }},
                { "120Win", new MenuButtonDef { TextureName = "120WinOff.psd", DesktopOutline = _desktopOutline }},
                { "180Win", new MenuButtonDef { TextureName = "180WinOff.psd", DesktopOutline = _desktopOutline }},
                { "240Win", new MenuButtonDef { TextureName = "240WinOff.psd", DesktopOutline = _desktopOutline }}
            };

            foreach (var menuPath in new string[] { "intl.menu/SettingsMenu", "intl.menu/StartMenu", "intl.menu/SettingsMenu/Goals15", "intl.menu/SettingsMenu/Goals9", "intl.menu/SettingsMenu/Goals6" })
            {
                var menuTr = billiardsModule.transform.Find(menuPath);
                foreach ( KeyValuePair<string, MenuButtonDef> kvp in menuButtonDefDict)
                {
                    var buttonTr = menuTr.Find(kvp.Key);
                    if (ReferenceEquals(null, buttonTr))
                    {
                        continue;
                    }
                    
                    var visualTr = buttonTr.Find("Visual/Button");
                    var mr = visualTr.GetComponent<MeshRenderer>();
                    Texture t = AssetDatabase.LoadAssetAtPath(buttonAssetPath + kvp.Value.TextureName, typeof(Texture)) as Texture;
                    if (ReferenceEquals(null, t))
                    {
	                    t = AssetDatabase.LoadAssetAtPath(buttonAssetPathEijis + kvp.Value.TextureName, typeof(Texture)) as Texture;
                    }
                    var m = new Material(Shader.Find("Unlit/Texture"));
                    m.SetTexture(_MainTexId, t);
                    var materials = mr.sharedMaterials;
                    materials[0] = m;
                    mr.sharedMaterials = materials;
                
                    visualTr = buttonTr.Find("Visual/DesktopOutline");
                    mr = visualTr.GetComponent<MeshRenderer>();
                    m = new Material(Shader.Find("Unlit/Color"));
                    m.SetColor(_ColorId, kvp.Value.DesktopOutline);
                    materials = mr.sharedMaterials;
                    materials[0] = m;
                    mr.sharedMaterials = materials;
                }
            }
		}
		
		/*
		[MenuItem("CONTEXT/BilliardsModule/Eijis_FixupWoodRackFrameMesh")]
		private static void Eijis_FixupWoodRackFrameMesh_Menu(MenuCommand command)
		{
			try
			{
				BilliardsModule billiardsModule = (BilliardsModule)command.context;
				FixupWoodRackFrameMesh(billiardsModule);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Custom Script Exception", ex.ToString(), "OK");
			}
		}
		*/
		
		private static void FixupWoodRackFrameMesh(BilliardsModule billiardsModule, Mesh woodFrameMesh)
		{
			string[] SetWoodFrameMeshObjectsInBilliardsModule = new[]
			{
				// "/Pool Parlor Table Rotation Variant/DummyRack SimpleButton Toggle/WoodRackFrame",
				"intl.menu/SettingsMenu/WoodRackFrame"
			};
			
			foreach (var woodFrameGameObject in SetWoodFrameMeshObjectsInBilliardsModule)
			{
				var tr = billiardsModule.transform.Find(woodFrameGameObject);
				if (ReferenceEquals(null, tr))
				{
					continue;
				}

				var meshFilter = tr.GetComponent<MeshFilter>();
				if (ReferenceEquals(null, meshFilter))
				{
					continue;
				}

				meshFilter.sharedMesh = woodFrameMesh;
			}

			string[] SetWoodFrameMeshObjectsOnScene = new[]
			{
				"DummyRack SimpleButton Toggle"
			};

			foreach (var gameObjectName in SetWoodFrameMeshObjectsOnScene)
			{
				var gameObject = GameObject.Find(gameObjectName);
				if (ReferenceEquals(null, gameObject))
				{
					continue;
				}
				
				var tr = gameObject.transform.Find("WoodRackFrame");
				if (ReferenceEquals(null, tr))
				{
					continue;
				}

				var meshFilter = tr.GetComponent<MeshFilter>();
				if (ReferenceEquals(null, meshFilter))
				{
					continue;
				}

				meshFilter.sharedMesh = woodFrameMesh;
			}

		}
		
	}
}
