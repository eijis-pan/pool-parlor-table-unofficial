using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UdonSharp;
using VRC.Udon;

namespace EijisPoolParlorTableUtil
{
	public class StraightTableSetup : EditorWindow
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
					"calledBall_target",
					"CushionTouch_0",
					"CushionTouch_1",
					"CushionTouch_2",
					"CushionTouch_3",
					"markerCenterSpot",
					"markerFootSpot",
					"markerHeadSpot",
					"pocket_point_0",
					"pocket_point_1",
					"pocket_point_2",
					"pocket_point_3",
					"pocket_point_4",
					"pocket_point_5",
					"requestBreakBlue",
					"requestBreakOrange",
					"special_penalty"
				}
			},
			{
				"intl.controls",
				new []
				{
					"callShotLock"
				}
			},
			{
				"intl.desktop/desktop",
				new []
				{
					"desktop_callshot"
				}
			},
			{
				"intl.menu/SettingsMenu",
				new []
				{
					"10Win",
					"20Win",
					"30Win",
					"50Win",
					"70Win",
					"100Win",
					"RackSheet",
					"RackSheetToggle",
					"SemiAutoCallToggle",
					"WoodFrameToggle",
					"WoodRackFrame",
					"Backboard_Straight"
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
			},
			{ 
				"pocketPoint", 
				new []
				{
					"intl.balls/pocket_point_0",
					"intl.balls/pocket_point_1",
					"intl.balls/pocket_point_2",
					"intl.balls/pocket_point_3",
					"intl.balls/pocket_point_4",
					"intl.balls/pocket_point_5"
				}
			}
		};

		public static readonly Dictionary<string, string> SetGraphicsManagerGameObjectProperties = new Dictionary<string, string>
		{
			{ 
				"specialPenalty", "intl.balls/special_penalty"
			}
		};

		public static readonly Dictionary<string, string> SetGraphicsManagerMaterialProperties = new Dictionary<string, string>
		{
			{ "calledPocketBlue", "Assets/eijis/Materials/PocketMarkerBottom_Blue.mat" },
			{ "calledPocketOrange", "Assets/eijis/Materials/PocketMarkerBottom_Orange.mat" },
			{ "calledPocketSphereBlue", "Assets/eijis/Materials/PocketMarker_Blue.mat" },
			{ "calledPocketSphereOrange", "Assets/eijis/Materials/PocketMarker_Orange.mat" },
			{ "calledPocketSphereWhite", "Assets/eijis/Materials/PocketMarker_White.mat" },
			{ "calleShotLockBlue", "Assets/eijis/Materials/CallShotLockButtonBlue.mat" },
			{ "calleShotLockOrange", "Assets/eijis/Materials/CallShotLockButtonOrange.mat" },
			{ "calleShotLockWhite", "Assets/eijis/Materials/CallShotLockButtonWhite.mat" },
			{ "pocketPointBlue", "Assets/eijis/Materials/point_blue.mat" },
			{ "pocketPointOrange", "Assets/eijis/Materials/point_orange.mat" },
			{ "denyBallShadowMaterial", "Assets/eijis/Materials/BallShadow_deny.mat" }
		};

		public static readonly Dictionary<string, MeshAssetInfo> SetGraphicsManagerMeshProperties = new Dictionary<string, MeshAssetInfo>
		{
			{ "pocketPointMeshPlus", new MeshAssetInfo("Assets/metaphira/Modules/BilliardsModule/Mesh/4BallPoints.fbx", "plus1") },
			{ "pocketPointMeshMinus", new MeshAssetInfo("Assets/metaphira/Modules/BilliardsModule/Mesh/4BallPoints.fbx", "munus1") }
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
			{ "button10Win", "intl.menu/SettingsMenu/10Win" },
			{ "button20Win", "intl.menu/SettingsMenu/20Win" },
			{ "button30Win", "intl.menu/SettingsMenu/30Win" },
			{ "button50Win", "intl.menu/SettingsMenu/50Win" },
			{ "button70Win", "intl.menu/SettingsMenu/70Win" },
			{ "button100Win", "intl.menu/SettingsMenu/100Win" },
			{ "buttonRackSheetToggle", "intl.menu/SettingsMenu/RackSheetToggle" },
			{ "buttonWoodFrameToggle", "intl.menu/SettingsMenu/WoodFrameToggle" },
			{ "buttonSemiAutoCallToggle", "intl.menu/SettingsMenu/SemiAutoCallToggle" }
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
			"intl.table/model.regular/table_artwork/PointPocketMarker_0",
			"intl.table/model.regular/table_artwork/PointPocketMarker_1",
			"intl.table/model.regular/table_artwork/PointPocketMarker_2",
			"intl.table/model.regular/table_artwork/PointPocketMarker_3",
			"intl.table/model.regular/table_artwork/PointPocketMarker_4",
			"intl.table/model.regular/table_artwork/PointPocketMarker_5",
			// "intl.menu/SettingsMenu/Backboard",
			// "intl.menu/SettingsMenu/8Ball",
			// "intl.menu/SettingsMenu/9Ball",
			// "intl.menu/SettingsMenu/4Ball",
			// "intl.menu/SettingsMenu/4BallJP",
			// "intl.menu/SettingsMenu/4BallKR",
			"intl.controls/callShotLock"
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

		[MenuItem("CONTEXT/BilliardsModule/Eijis_StraightTableSetup")]
		private static void Eijis_StraightTableSetup_Menu(MenuCommand command)
		{
			try
			{
				BilliardsModule billiardsModule = (BilliardsModule)command.context;

				var window = CreateInstance<StraightTableSetup>();
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

				Debug.Log("Eijis_StraightTableSetup 開始");
				Undo.RecordObject(_scoreScreen, "開始");

				Eijis_StraightTableSetup();
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

				Debug.Log("Eijis_StraightTableSetup 終了");
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

		private void Eijis_StraightTableSetup()
		{
			int warnCount = 0;
			int errCount = 0;

			//_BilliardsModule.scoreBoard = _scoreBoard;
			//_BilliardsModule.SetProgramVariable("scoreBoard", _scoreBoard);
			var serializedObject = new SerializedObject(_BilliardsModule);
			serializedObject.Update();
			serializedObject.FindProperty("defaultGameModeToStraight").boolValue = true;

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

			EditorUtility.DisplayDialog("Custom Script Result", 
				"Eijis_StraightTableSetup を完了しました。" + (0 < warnCount ? "（警告あり）" : "") + (0 < errCount ? "（エラーあり）" : ""), 
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
                { "RackSheetToggle", new MenuButtonDef { TextureName = "RackSheetOff.psd", DesktopOutline = _desktopOutline }},
                { "WoodFrameToggle", new MenuButtonDef { TextureName = "WoodFrameOff.psd", DesktopOutline = _desktopOutline }},
                { "SemiAutoCallToggle", new MenuButtonDef { TextureName = "SemiAutoCallOff.psd", DesktopOutline = _desktopOutline }},
                { "20Win", new MenuButtonDef { TextureName = "20WinOff.psd", DesktopOutline = _desktopOutline }},
                { "30Win", new MenuButtonDef { TextureName = "30WinOff.psd", DesktopOutline = _desktopOutline }},
                { "50Win", new MenuButtonDef { TextureName = "50WinOff.psd", DesktopOutline = _desktopOutline }},
                { "70Win", new MenuButtonDef { TextureName = "70WinOff.psd", DesktopOutline = _desktopOutline }},
                { "100Win", new MenuButtonDef { TextureName = "100WinOff.psd", DesktopOutline = _desktopOutline }}
            };

            foreach (var menuPath in new string[] { "intl.menu/SettingsMenu", "intl.menu/StartMenu" })
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
				// "/Pool Parlor Table Straight Variant/DummyRack SimpleButton Toggle/WoodRackFrame",
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
