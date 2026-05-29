
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ExtraTools
{

    public class ExtraTools : EditorWindow
    {

        static SceneView GetActiveSceneView()
        {
            //Return the focused window, if it is a SceneView
            if (EditorWindow.focusedWindow != null
                    && EditorWindow.focusedWindow.GetType() == typeof(SceneView))
            {
                return (SceneView)EditorWindow.focusedWindow;
            }
            //Otherwise return the first available SceneView
            ArrayList temp = SceneView.sceneViews;
            return (SceneView)temp[0];
        }
		
        [MenuItem("Tools/Select Small Objects")]
        static void SelectSmallObjects()
        {
            var objs = Selection.objects;

            List<Object> newSelection = new List<Object>();
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];
                if (obj.GetType() == typeof(GameObject))
                {
                    var go = (GameObject)obj;
                    var r =go.GetComponent<Renderer>();
                    if(r != null)
                    {
                        if (r.bounds.size.magnitude < 1 || r.GetComponent<MeshFilter>().sharedMesh.vertexCount < 4)
                        {
                            newSelection.Add(obj);
                        }
                    }
                }
            }
            Selection.objects = newSelection.ToArray();
        }

        static Texture2D RenderTexture(Texture2D src, string shaderPath, int width, int height, int channel = -1)
        {
            Texture2D res = new Texture2D(width, height, TextureFormat.ARGB32, true, true);
            RenderTexture tempRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            tempRT.DiscardContents();
            Shader s = Shader.Find(shaderPath);
            if (s == null)
            {
                Debug.LogError("Missing shader " + shaderPath);
                res.Apply();
                return res;
            }
            Material genMat = new Material(Shader.Find(shaderPath));
            if (channel >= 0)
            {
                genMat.SetInt("_Channel", channel);
            }

            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Graphics.Blit(src, tempRT, genMat);
            GL.sRGBWrite = false;

			UnityEngine.RenderTexture.active = tempRT;
            res.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            res.Apply();
			UnityEngine.RenderTexture.active = null;
            tempRT.Release();
            DestroyImmediate(tempRT);
            DestroyImmediate(genMat);
            return res;
        }

		[MenuItem("Tools/Create Normal From Diffuse")]
		static void CreateNormalFromDiffuse()
        {
            var objs = Selection.objects;
            for (int i = 0; i < objs.Length; ++i)
            {
                var obj = objs[i];
                if (obj.GetType() == typeof(Texture2D))
                {
                    Texture2D diffuse = (Texture2D)obj;
					//Debug.Log("Create Normal From Diffuse: " + diffuse.name);
					var normal = RenderTexture(diffuse, "Hidden/NormalFromDiffuse", diffuse.width, diffuse.height);
                    string fileName = AssetDatabase.GetAssetPath(diffuse);
                    fileName = Path.GetDirectoryName(fileName) + "/" + Path.GetFileNameWithoutExtension(fileName) + "_N.tga";
					if (!File.Exists(fileName))
					{
						Debug.Log("Write: " + fileName);
						png32totga.Program.Main(fileName, normal);
					}
					else
					{
						if (EditorUtility.DisplayDialog("Overwrite File", "Do you want to overwrite "+ Path.GetFileName(fileName) +" ?", "Yes", "No"))
						{
							Debug.Log("Write: " + fileName);
							png32totga.Program.Main(fileName, normal);
						}
					}
					AssetDatabase.Refresh();
				}
			}
		}

		[MenuItem("Tools/Create Height From Normal")]
        static void CreateHeightFromNormal()
        {
            var objs = Selection.objects;
            for (int i = 0; i < objs.Length; ++i)
            {
                var obj = objs[i];
                if (obj.GetType() == typeof(Texture2D))
                {
					Texture2D normal = (Texture2D)obj;
					//Debug.Log("Create Height From Normal: " + normal.name);
					var height = RenderTexture(normal, "Hidden/HeightFromNormal", normal.width, normal.height);
                    string fileName = AssetDatabase.GetAssetPath(normal);
                    fileName = Path.GetDirectoryName(fileName) + "/" + Path.GetFileNameWithoutExtension(fileName) + "_H.tga";
					if (!File.Exists(fileName))
					{
						Debug.Log("Write: " + fileName);
						png32totga.Program.Main(fileName, height);
					}
					else
					{
						if (EditorUtility.DisplayDialog("Overwrite File", "Do you want to overwrite " + Path.GetFileName(fileName) + " ?", "Yes", "No"))
						{
							Debug.Log("Write: " + fileName);
							png32totga.Program.Main(fileName, height);
						}
					}
					AssetDatabase.Refresh();
				}
            }
		}


		public static List<T> GetSubObjectsOfType<T>(Object asset) where T : Object
        {
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));
            List<T> ofType = new List<T>();
            foreach (Object o in objs)
            {
                if (o is T)
                {
                    ofType.Add(o as T);
                }
            }
            return ofType;
        }

        [MenuItem("Tools/Replace Meshes From Prefab")]
        static void ReplaceMeshes()
        {
            var objs = Selection.objects;
            if (objs.Length == 2)
            {
                int goNum = -1;
                for (int i = 0; i < objs.Length; ++i)
                {
                    if (objs[i].GetType() == typeof(GameObject))
                    {
                        goNum = i;
                    }
                }
                int meshNum = -1;
                for (int i = 0; i < objs.Length; ++i)
                {
                    if (objs[i].GetType() == typeof(Mesh))
                    {
                        meshNum = i;
                    }
                }

                if (goNum >= 0 && meshNum >= 0)
                {
                    var go = (GameObject)objs[goNum];
                    var mfs = go.GetComponentsInChildren<MeshFilter>();
                    var mesh = (Mesh)objs[meshNum];

                    var meshList = GetSubObjectsOfType<Mesh>(objs[meshNum]);
                    int length = meshList.Count - 1;

                    if (meshList.Count == mfs.Length)
                    {
                        Undo.RegisterFullObjectHierarchyUndo(go, "ReplaceMeshes " + go.name);
                        for (int i = 0; i < mfs.Length; i++)
                        {
                            mfs[i].sharedMesh = meshList[length - i];
                        }
                        Debug.Log("Replaced Meshes of " + go.name + " with " + mesh.name);
                    }
                }
            }
        }

		

		static void ConvertTextures(bool fixAlphaEdges = true, bool linear = false)
        {
            var objs = Selection.objects;
            for (int i = 0; i < objs.Length; ++i)
            {
                var obj = objs[i];
                if (obj.GetType() == typeof(Texture2D))
                {
                    Texture2D map = (Texture2D)obj;
                    int widthTex = ((Texture2D)obj).width;
                    int heightTex = ((Texture2D)obj).height;
                    Texture2D tex = new Texture2D(widthTex, heightTex, TextureFormat.ARGB32, false, linear);

                    string path = AssetDatabase.GetAssetPath(map);
                    TextureImporter A = (TextureImporter)AssetImporter.GetAtPath(path);
                    var oldSetting = A.sRGBTexture;
                    A.sRGBTexture = false;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

					RenderTexture rtTex = UnityEngine.RenderTexture.GetTemporary(widthTex, heightTex, 0, RenderTextureFormat.ARGB32, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);

                    //GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear) && !linear;
                    Graphics.Blit(map, rtTex);
					//GL.sRGBWrite = false;
					//Graphics.Blit(map, rtTex);

					RenderTexture oldRT = UnityEngine.RenderTexture.active;
					UnityEngine.RenderTexture.active = rtTex;
                    tex.ReadPixels(new Rect(0, 0, widthTex, heightTex), 0, 0);
					UnityEngine.RenderTexture.active = oldRT;

                    Color average = Color.black;
                    Color[] cols = tex.GetPixels();
                    if (fixAlphaEdges)
                    {
                        for (int c = 0; c < cols.Length; ++c)
                            average += cols[c];

                        average /= cols.Length * 2.0f;
                        average = (average * average) / Color.gray.r;

                        //Color[] newCols = new Color[cols.Length];
                        for (int c = 0; c < cols.Length; ++c)
                        {
                            cols[c].r *= cols[c].a;
                            cols[c].g *= cols[c].a;
                            cols[c].b *= cols[c].a;
                            cols[c].r += Mathf.Clamp(average.r - cols[c].a, 0.0f, 1.0f);
                            cols[c].g += Mathf.Clamp(average.g - cols[c].a, 0.0f, 1.0f);
                            cols[c].b += Mathf.Clamp(average.b - cols[c].a, 0.0f, 1.0f);
                        }
                    }
                    Texture2D destTex = new Texture2D(widthTex, heightTex, TextureFormat.ARGB32, false, linear);
                    destTex.SetPixels(cols);
                    destTex.Apply();

                    string fileName = AssetDatabase.GetAssetPath(map);
                    fileName = Path.GetDirectoryName(fileName) + "/" + Path.GetFileNameWithoutExtension(fileName) + ".tga";
                    Debug.Log("Write: " + fileName);
                    png32totga.Program.Main(fileName, destTex);

                    A.sRGBTexture = oldSetting;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
					UnityEngine.RenderTexture.ReleaseTemporary(rtTex);

                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(fileName, ImportAssetOptions.ForceUpdate);
                }
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Convert Textures To TGA")]
        static void ConvertTextures2TGA()
        {
            ConvertTextures();
        }
        [MenuItem("Tools/Convert Textures To TGA (No AlphaEdges Fix)")]
        static void ConvertTextures2TGA_NoAlphaFix()
        {
            ConvertTextures(false, false);
        }
        [MenuItem("Tools/Convert Textures To TGA (No AlphaEdges Fix and Linear)")]
        static void ConvertTextures2TGA_NoAlphaFixAndLinear()
        {
            ConvertTextures(false, true);
        }

        [MenuItem("Tools/Hide Selection")]
        static void HideSelection()
        {
            var go = Selection.activeGameObject;
            if (go != null)
                go.SetActive(go.activeSelf == false);
        }

    }

    public class InspectorLockToggle
    {
        /*
            [MenuItem("Tools/Select Inspector under mouse cursor (use hotkey) #&q")]
            static void SelectLockableInspector ()
            {
                    if (EditorWindow.mouseOverWindow.GetType ().Name == "InspectorWindow") {
                            _mouseOverWindow = EditorWindow.mouseOverWindow;
                            Type type = Assembly.GetAssembly (typeof(Editor)).GetType ("UnityEditor.InspectorWindow");
                            Object[] findObjectsOfTypeAll = Resources.FindObjectsOfTypeAll (type);
                            int indexOf = findObjectsOfTypeAll.ToList ().IndexOf (_mouseOverWindow);
                            EditorPrefs.SetInt ("LockableInspectorIndex", indexOf);
                    }
            }
        */
        public static EditorWindow GetWindowByName(string pName)
        {
            UnityEngine.Object[] objectList = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            foreach (UnityEngine.Object obj in objectList)
            {
                if (obj.GetType().ToString() == pName)
                {
                    return ((EditorWindow)obj);
                }
            }
            return (null);
        }

        private static EditorWindow _inspectorWindow = null;

        public static EditorWindow InspectorWindow
        {
            get
            {
                _inspectorWindow = _inspectorWindow ?? GetWindowByName("UnityEditor.InspectorWindow");
                return (_inspectorWindow);
            }
        }

        [MenuItem("Tools/Toggle Lock &q")]
        static void ToggleInspectorLock()
        {
            Type type = InspectorWindow.GetType();
            PropertyInfo propertyInfo = type.GetProperty("isLocked");
            bool value = (bool)propertyInfo.GetValue(InspectorWindow, null);
            propertyInfo.SetValue(InspectorWindow, !value, null);
            InspectorWindow.Repaint();
        }

        [MenuItem("Tools/Clear Console #&c")]
        static void ClearConsole()
        {
            Type type = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditorInternal.LogEntries");
            type.GetMethod("Clear").Invoke(null, null);
        }
    }

    //public class CopyComponentsClass
    //{

    //    static Component[] copies;

    //    static Component CopyComponent(Component original, GameObject destination)
    //    {
    //        System.Type type = original.GetType();
    //        Component copy = destination.AddComponent(type);
    //        // Copied fields can be restricted with BindingFlags
    //        System.Reflection.FieldInfo[] fields = type.GetFields();

    //        foreach (System.Reflection.FieldInfo field in fields)
    //            field.SetValue(copy, field.GetValue(original));

    //        return copy;
    //    }

    //    [MenuItem("CONTEXT/Component/Copy All Components")]
    //    private static void NewCopyAllComponents(MenuCommand menuCommand)
    //    {
    //        var comp = menuCommand.context as Component;
    //        copies = comp.gameObject.GetComponents<Component>();
    //    }

    //    [MenuItem("CONTEXT/Component/Paste All Components")]
    //    private static void NewPasteAllComponents(MenuCommand menuCommand)
    //    {
    //        var comp = menuCommand.context as Component;
    //        Undo.RecordObject(comp.gameObject, "Paste All Components");
    //        if (copies != null && copies.Length > 0)
    //        {
    //            //foreach (Component component in copies) {
    //            //for (int i = copies.Length-1; i >= 0; i--) {
    //            for (int i = 0; i < copies.Length; i++)
    //            {
    //                //Debug.Log(comp.GetType().Name+" "+comp.GetType().BaseType.Name);
    //                if (!(copies[i].GetType().Name.CompareTo("Transform") == 0)
    //                    && !(copies[i].GetType().Name.CompareTo("MeshRenderer") == 0)
    //                    && !(copies[i].GetType().Name.CompareTo("MeshFilter") == 0))
    //                {
    //                    //CopyComponent (copies[i], comp.gameObject);
    //                    UnityEditorInternal.ComponentUtility.CopyComponent(copies[i]);
    //                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(comp.gameObject);
    //                }
    //                Debug.Log(copies[i].GetType().Name);
    //            }
    //        }
    //    }

    //    [MenuItem("CONTEXT/Component/Remove All Components")]
    //    private static void NewRemoveAllComponents(MenuCommand menuCommand)
    //    {
    //        var comp = menuCommand.context as Component;
    //        Undo.RecordObject(comp.gameObject, "Remove All Components");
    //        var comps = comp.gameObject.GetComponents<Component>();
    //        if (comps != null && comps.Length > 0)
    //        {
    //            //foreach (Component component in copies) {
    //            for (int i = comps.Length - 1; i >= 0; --i)
    //            {
    //                if (!(comps[i].GetType().Name.CompareTo("Transform") == 0))
    //                {
    //                    Undo.DestroyObjectImmediate(comps[i]);
    //                }
    //            }
    //        }
    //    }
    //}
}
