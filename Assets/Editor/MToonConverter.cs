using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MToonConverter : EditorWindow
{

    private SerializedObject _serializedObject;

    [SerializeField] private GameObject _targetVRM;

    private Shader _fromShader;
    private string _fromShaderName = "VRM/MToon";
    private Shader _toShader;
    private string _toShaderName = "VRM10/Universal Render Pipeline/MToon10";

    private List<Material> _targetMaterials = new(); //ModelName.Materials下にあるマテリアル群
    private List<Material> _convertMaterials = new(); //

    private string _targetVrmFullPath;
    private string _targetVrmDirectoryName;
    private string _targetVrmFileName;


    [MenuItem("Window/mmm/MToon Converter")]
    static void Open()
    {
        var window = GetWindow<MToonConverter>();
        window.titleContent = new GUIContent("MToon-Converter forURP");
    }

    private void OnEnable()
    {
        _serializedObject = new SerializedObject(this);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_serializedObject.FindProperty("_targetVRM"));
        EditorGUILayout.Space();
        if (GUILayout.Button("Convert material")) CreateConvertedPrefab4URP();
    }


    private void CreateConvertedPrefab4URP()
    {
        _serializedObject.ApplyModifiedProperties(); //これを入れないと変数が更新されない

        _fromShader = Shader.Find(_fromShaderName);
        _toShader = Shader.Find(_toShaderName);

        if (_fromShader == null)
        {
            Debug.Log($"Can't find {_fromShaderName}!");
            return;
        }

        if (_toShader == null)
        {
            Debug.Log($"Can't find {_toShaderName}!");
            return;
        }

        ///---get VRM's materials.
        _targetVrmFullPath = AssetDatabase.GetAssetPath(_targetVRM);
        _targetVrmDirectoryName = Path.GetDirectoryName(_targetVrmFullPath);
        _targetVrmFileName = Path.GetFileNameWithoutExtension(_targetVrmFullPath);
        string[] filePaths = Directory.GetFiles($"{_targetVrmDirectoryName}/{_targetVrmFileName}.Materials", "*.asset",
            SearchOption.AllDirectories);

        CreateUrpMaterial(filePaths);

        //set material to vrm.
        GameObject objSrc = (GameObject)PrefabUtility.InstantiatePrefab(_targetVRM); //prefabを継承した状態でinstantiate.
        ReplaceMaterial(objSrc);

        var prefabPath = $"{_targetVrmDirectoryName}/{_targetVrmFileName}_urp.prefab";
        PrefabUtility.SaveAsPrefabAsset(objSrc, prefabPath); //create prefabVariant.
        DestroyImmediate(objSrc); //PrefabVariant作成のために作成したPrefabをSceneから削除
    }


    private void CreateUrpMaterial(string[] filePaths)
    {
        _targetMaterials.Clear();
        _convertMaterials.Clear();

        foreach (var filePath in filePaths)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(filePath);
            if (material != null)
            {
                _targetMaterials.Add(material);
            }
        }

        if (_targetMaterials.Count <= 0) return;

        foreach (var targetMaterial in _targetMaterials)
        {

            if (targetMaterial.shader != _fromShader)
            {
                Debug.Log($"MToon is not used! : {targetMaterial.name}");

                _convertMaterials.Add(targetMaterial);
                continue;
            }

            //get MToon-properties value.
            /*
            var renderTag = targetMaterial.GetTag("RenderType", true, "Nothing");
            int alphaMode = 0;
            int transparentWithZWrite = 0;
            switch (renderTag)
            {
                case "Opaque":
                    alphaMode = 0;
                    break;
                case "Cutout":
                    alphaMode = 1;
                    break;
                case "Transparent":
                    alphaMode = 2;
                    break;
                case "TransparentWithZWrite":
                    transparentWithZWrite = 1;
                    break;
                default:
                    alphaMode = 0;
                    break;
            }*/

            int cullMode = (int)GetFloatSafe(targetMaterial, "_CullMode");
            int doubleSided = 0;
            switch (cullMode)
            {
                case 0: //off
                    doubleSided = 0; //on
                    break;
                case 1: //front
                    doubleSided = 1; //off
                    break;
                case 2: //back
                    doubleSided = 1; //off
                    break;
                default:
                    doubleSided = 1; //off
                    break;
            }

            var queueTag = targetMaterial.GetTag("Queue", true, "Nothing");
            int renderQue = 0;
            switch (queueTag)
            {
                case "Background":
                    break;
                case "Geometry":
                    break;
                case "AlphaTest":
                    break;
                case "GeometryLast":
                    break;
                case "Transparent":
                    break;
                case "Overlay":
                    break;
                default:
                    break;
            }

            int outlineWidthMode = (int)GetFloatSafe(targetMaterial, "_OutlineWidthMode"); //そのまま

            var alphaMode = GetFloatSafe(targetMaterial, "_BlendMode");

            var cutoff = GetFloatSafe(targetMaterial, "_Cutoff");
            var mainColor = GetColorSafe(targetMaterial, "_Color");
            var shadeColor = GetColorSafe(targetMaterial, "_ShadeColor");
            var mainTex = GetTextureSafe(targetMaterial, "_MainTex");
            var shadeTex = GetTextureSafe(targetMaterial, "_ShadeTexture");
            var bumpScale = GetFloatSafe(targetMaterial, "_BumpScale");
            var bumpMap = GetTextureSafe(targetMaterial, "_BumpMap");
            var receiveShadow = GetFloatSafe(targetMaterial, "_ReceiveShadowRate");
            var receiveShadowTexture = GetTextureSafe(targetMaterial, "_ReceiveShadowTexture");
            var shadingGradeRate = GetFloatSafe(targetMaterial, "_ShadingGradeRate");
            var shadingGradeTexture = GetTextureSafe(targetMaterial, "_ShadingGradeTexture");
            var shadeShift = GetFloatSafe(targetMaterial, "_ShadeShift");
            var shadeToony = GetFloatSafe(targetMaterial, "_ShadeToony");
            var LightColorAttenuaation = GetFloatSafe(targetMaterial, "_LightColorAttenuation");
            var indirectLightIntensity = GetFloatSafe(targetMaterial, "_IndirectLightIntensity");
            var rimColor = GetColorSafe(targetMaterial, "_RimColor");
            var rimTexture = GetTextureSafe(targetMaterial, "_RimTexture");
            var rimLightingMix = GetFloatSafe(targetMaterial, "_RimLightingMix");
            var rimFresnelPower = GetFloatSafe(targetMaterial, "_RimFresnelPower");
            var rimLift = GetFloatSafe(targetMaterial, "_RimLift");
            var sphereAdd = GetTextureSafe(targetMaterial, "_SphereAdd");
            var emissionColor = GetColorSafe(targetMaterial, "_EmissionColor");
            var emissionMap = GetTextureSafe(targetMaterial, "_EmissionMap");
            var outlineWidthTexture = GetTextureSafe(targetMaterial, "_OutlineWidthTexture");
            var outlineWidth = GetFloatSafe(targetMaterial, "_OutlineWidth");
            var outlineScaledMaxDistance = GetFloatSafe(targetMaterial, "_OutlineScaledMaxDistance");
            var outlineColor = GetColorSafe(targetMaterial, "_OutlineColor");
            var outlineLightingMix = GetFloatSafe(targetMaterial, "_OutlineLightingMix");
            var uvAnimMaskTexture = GetTextureSafe(targetMaterial, "_UvAnimMaskTexture");
            var uvAnimScrollX = GetFloatSafe(targetMaterial, "_UvAnimScrollX");
            var uvAnimScrollY = GetFloatSafe(targetMaterial, "_UvAnimScrollY");
            var uvAnimRotation = GetFloatSafe(targetMaterial, "_UvAnimRotation");

            //set MToon-properties value to MToon10-properties.
            Material convertMaterial = new Material(_toShader);

            SetFloatSafe(convertMaterial, "_AlphaMode", alphaMode);
            if(alphaMode == 3.0f) SetFloatSafe(convertMaterial, "_TransparentWithZWrite", 1.0f);
            SetFloatSafe(convertMaterial, "_Cutoff", cutoff);
            //_RenderQueueOffset
            SetIntSafe(convertMaterial, "_DoubleSided", doubleSided);
            SetColorSafe(convertMaterial, "_Color", mainColor);
            if (mainTex != null) SetTextureSafe(convertMaterial, "_MainTex", mainTex);
            SetColorSafe(convertMaterial, "_ShadeColor", shadeColor);
            if (shadeTex != null) SetTextureSafe(convertMaterial, "_ShadeTex", shadeTex);
            if (bumpMap != null) SetTextureSafe(convertMaterial, "_BumpMap", bumpMap);
            SetFloatSafe(convertMaterial, "_BumpScale", bumpScale);
            SetFloatSafe(convertMaterial, "_ShadingShiftFactor", shadeShift);
            SetFloatSafe(convertMaterial, "_ReceiveShadowRate", receiveShadow);
            if (receiveShadowTexture != null)
                SetTextureSafe(convertMaterial, "_ReceiveShadowTexture", receiveShadowTexture);
            if (shadingGradeTexture != null) SetTextureSafe(convertMaterial, "_ShadingShiftTex", shadingGradeTexture);
            SetFloatSafe(convertMaterial, "_ShadingShiftTexScale", shadingGradeRate);
            SetFloatSafe(convertMaterial, "_ShadingToonyFactor", shadeToony);
            //_GiEqualization
            SetColorSafe(convertMaterial, "_EmissionColor", emissionColor);
            if (emissionMap != null) SetTextureSafe(convertMaterial, "_EmissionMap", emissionMap);
            //_MatcapColor
            if (sphereAdd != null) SetTextureSafe(convertMaterial, "_MatcapTex", sphereAdd);
            SetColorSafe(convertMaterial, "_RimColor", rimColor);
            SetFloatSafe(convertMaterial, "_RimFresnelPower", rimFresnelPower);
            SetFloatSafe(convertMaterial, "_RimLift", rimLift);
            if (rimTexture != null) SetTextureSafe(convertMaterial, "_RimTex", rimTexture);
            SetFloatSafe(convertMaterial, "_RimLightingMix", rimLightingMix);
            SetIntSafe(convertMaterial, "_OutlineWidthMode", outlineWidthMode);
            SetFloatSafe(convertMaterial, "_OutlineWidth", outlineWidth);
            if (outlineWidthTexture != null) SetTextureSafe(convertMaterial, "_OutlineWidthTex", outlineWidthTexture);
            SetColorSafe(convertMaterial, "_OutlineColor", outlineColor);
            SetFloatSafe(convertMaterial, "_OutlineLightingMix", outlineLightingMix);
            if (uvAnimMaskTexture != null) SetTextureSafe(convertMaterial, "_UvAnimMaskTex", uvAnimMaskTexture);
            SetFloatSafe(convertMaterial, "_UvAnimScrollXSpeed", uvAnimScrollX);
            SetFloatSafe(convertMaterial, "_UvAnimScrollYSpeed", uvAnimScrollY);
            SetFloatSafe(convertMaterial, "_UvAnimRotationSpeed", uvAnimRotation);
            //cullmode
            //srcblend
            //dsttblend
            //zwrite
            //alphatomask
            //debugmode
            SetFloatSafe(convertMaterial, "_M_EditMode", 1.0f);

            _convertMaterials.Add(convertMaterial);

            //create material file.
            var folderPath = $"{_targetVrmDirectoryName}/{_targetVrmFileName}.Materials_MToon10";
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            var filePath = $"{folderPath}/{targetMaterial.name}_urp.asset";
            AssetDatabase.CreateAsset(convertMaterial, filePath);
        }
    }

    private void ReplaceMaterial(GameObject objSrc)
    {
        foreach (Transform child in objSrc.transform) //meshLoop
        {
            GameObject childObj = child.gameObject;
            ReplaceMaterial(childObj); //再帰的に子供にも同じ処理

            if (!childObj.TryGetComponent<SkinnedMeshRenderer>(out var mesh)) continue;

            List<Material> changeMaterialList = new();

            for (int i = 0; i < mesh.sharedMaterials.Length; i++) //materialLoop
            {
                foreach (var targetMaterial in _targetMaterials) //nameSearchLoop
                {
                    if (mesh.sharedMaterials[i].name != targetMaterial.name) continue;

                    var index = _targetMaterials.IndexOf(targetMaterial);
                    changeMaterialList.Add(_convertMaterials[index]);
                    break;
                }
            }
            mesh.sharedMaterials = changeMaterialList.ToArray(); //要素をそれぞれ入れ替えだと出来ない。配列ごと入れ替える必要がある。
        }
    }

    private float GetFloatSafe(Material material, string propertyName)
    {
        if (material.HasProperty(propertyName)) return material.GetFloat(propertyName);
        return 0.0f;
    }

    private Color GetColorSafe(Material material, string propertyName)
    {
        if (material.HasProperty(propertyName)) return material.GetColor(propertyName);
        return Color.black;
    }

    private Texture GetTextureSafe(Material material, string propertyName)
    {
        if (material.HasProperty(propertyName) && material.HasTexture(propertyName))
            return material.GetTexture(propertyName);
        return null;
    }

    private void SetIntSafe(Material material, string propertyName, int value)
    {
        if (material.HasProperty(propertyName)) material.SetInt(propertyName, value);
    }

    private void SetFloatSafe(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName)) material.SetFloat(propertyName, value);
    }

    private void SetColorSafe(Material material, string propertyName, Color color)
    {
        if (material.HasProperty(propertyName)) material.SetColor(propertyName, color);
    }

    private void SetTextureSafe(Material material, string propertyName, Texture texture)
    {
        if (material.HasProperty(propertyName)) material.SetTexture(propertyName, texture);
    }
}
