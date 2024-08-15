using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UdonSharp;
using VRC.Udon;

namespace EijisPoolParlorTableUtil
{
	public class NoCushionFoulTableSetup : EditorWindow
	{
		private BilliardsModule _BilliardsModule;
		
		private Vector2 _scrollPosition = Vector2.zero;

		private static readonly string EijisAssetPathBase = "Assets/eijis/Prefab/BilliardsModule";
		public static readonly Dictionary<string, string[]> AdditionalPrefabs = new Dictionary<string, string[]>
		{
			{
				"intl.balls",
				new []
				{
					"CushionTouch"
				}
			},
			{
				"intl.menu/SettingsMenu",
				new []
				{
					"NoCushionFoulToggle"
				}
			}
		};

		public static readonly Dictionary<string, string> SetGraphicsManagerGameObjectProperties = new Dictionary<string, string>
		{
			{ 
				"cushionTouch", "intl.balls/CushionTouch"
			}
		};

		public static readonly Dictionary<string, string> SetMenuManagerUIButtonProperties = new Dictionary<string, string>
		{
			{ "buttonNoCushionFoulToggle", "intl.menu/SettingsMenu/NoCushionFoulToggle" }
		};

		private static readonly Type[] SetCallbackReferenceComponents =
		{
			typeof(UIButton)
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

		[MenuItem("CONTEXT/BilliardsModule/Eijis_NoCushionFoulTableSetup")]
		private static void Eijis_NoCushionFoulTableSetup_Menu(MenuCommand command)
		{
			try
			{
				BilliardsModule billiardsModule = (BilliardsModule)command.context;

				var window = CreateInstance<NoCushionFoulTableSetup>();
				window._BilliardsModule = billiardsModule;
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
			
			if (GUILayout.Button("開始",GUILayout.Height(30f)))
			{
				_helpBoxInfos.Clear();

				Debug.Log("Eijis_NoCushionFoulTableSetup 開始");

				Eijis_NoCushionFoulTableSetup();
				FixupMenuButtons(_BilliardsModule);
				
				var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

				Debug.Log("Eijis_NoCushionFoulTableSetup 終了");
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

		private void Eijis_NoCushionFoulTableSetup()
		{
			int warnCount = 0;
			int errCount = 0;

			//_BilliardsModule.scoreBoard = _scoreBoard;
			//_BilliardsModule.SetProgramVariable("scoreBoard", _scoreBoard);
			var serializedObject = new SerializedObject(_BilliardsModule);
			serializedObject.Update();
			serializedObject.FindProperty("useNoCushionFoulOption").boolValue = true;
			serializedObject.FindProperty("enableCushionTouchEffect").boolValue = true;
			
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

					SetMenuManagerReference(go, _BilliardsModule.menuManager);

					_helpBoxInfos.Add(new HelpBoxInfo(
						$"アセット {assetPath} を {_BilliardsModule.name} の {kvp.Key} に配置しました。", 
						MessageType.Info, true));
				}
			}
				
			serializedObject.ApplyModifiedProperties();

			var graphicsManagerSerializedObject = new SerializedObject(_BilliardsModule.graphicsManager);
			graphicsManagerSerializedObject.Update();
			foreach (var kvp in SetGraphicsManagerGameObjectProperties)
			{
				var tr = _BilliardsModule.transform.Find(kvp.Value);
				if (ReferenceEquals(null, tr))
				{
					continue;
				}

				graphicsManagerSerializedObject.FindProperty(kvp.Key).objectReferenceValue = tr.gameObject;
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

			EditorUtility.DisplayDialog("Custom Script Result", 
				"Eijis_NoCushionFoulTableSetup を完了しました。" + (0 < warnCount ? "（警告あり）" : "") + (0 < errCount ? "（エラーあり）" : ""), 
				"OK");
		}

		private GameObject LoadPrefabAsset (string assetPath)
		{
			var loadedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			return (GameObject)PrefabUtility.InstantiatePrefab(loadedAsset);
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
                { "NoCushionFoulToggle", new MenuButtonDef { TextureName = "NoCushionFoulOff.psd", DesktopOutline = _desktopOutline }},
                { "TimeRight", new MenuButtonDef { TextureName = "TriangleOnTemplate.psd", DesktopOutline = _desktopOutline }},
                { "TimeLeft", new MenuButtonDef { TextureName = "TriangleOnTemplate.psd", DesktopOutline = _desktopOutline }},
                { "StartButton", new MenuButtonDef { TextureName = "Play.psd", DesktopOutline = _desktopOutline }}
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
	}
}
