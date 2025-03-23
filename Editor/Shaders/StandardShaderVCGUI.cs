using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DavidUtils.Editor.Shaders.Editor.Shaders
{
	public class StandardShaderVCGUI : ShaderGUI
	{
		private enum WorkflowMode
		{
			Specular,
			Metallic,
			Dielectric
		}

		public enum BlendMode
		{
			Opaque,
			Cutout,
			Fade, // Old school alpha-blending mode, fresnel does not affect amount of transparency
			Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
		}

		private static class Styles
		{
			public static GUIStyle optionsButton = "PaneOptions";
			public static readonly GUIContent uvSetLabel = new("UV Set");
			public static GUIContent[] uvSetOptions = { new("UV channel 0"), new("UV channel 1") };

			public static string emptyTootip = "";
			public static readonly GUIContent albedoText = new("Albedo", "Albedo (RGB) and Transparency (A)");
			public static readonly GUIContent alphaCutoffText = new("Alpha Cutoff", "Threshold for alpha cutoff");
			public static readonly GUIContent specularMapText = new("Specular", "Specular (RGB) and Smoothness (A)");
			public static readonly GUIContent metallicMapText = new("Metallic", "Metallic (R) and Smoothness (A)");
			public static readonly GUIContent smoothnessText = new("Smoothness", "");
			public static readonly GUIContent normalMapText = new("Normal Map", "Normal Map");
			public static readonly GUIContent heightMapText = new("Height Map", "Height Map (G)");
			public static readonly GUIContent occlusionText = new("Occlusion", "Occlusion (G)");
			public static readonly GUIContent emissionText = new("Emission", "Emission (RGB)");
			public static readonly GUIContent detailMaskText = new("Detail Mask", "Mask for Secondary Maps (A)");
			public static readonly GUIContent detailAlbedoText = new(
				"Detail Albedo x2",
				"Albedo (RGB) multiplied by 2"
			);
			public static readonly GUIContent detailNormalMapText = new("Normal Map", "Normal Map");

			public static string whiteSpaceString = " ";
			public static readonly string primaryMapsText = "Main Maps";
			public static readonly string secondaryMapsText = "Secondary Maps";
			public static readonly string renderingMode = "Rendering Mode";
			public static readonly GUIContent emissiveWarning = new(
				"Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive."
			);
			public static GUIContent emissiveColorWarning =
				new("Ensure emissive color is non-black for emission to have effect.");
			public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
			public static GUIContent vcLabel = new("Vertex Color", "Vertex Color Intensity");
		}

		private MaterialProperty blendMode;
		private MaterialProperty albedoMap;
		private MaterialProperty albedoColor;
		private MaterialProperty alphaCutoff;
		private MaterialProperty specularMap;
		private MaterialProperty specularColor;
		private MaterialProperty metallicMap;
		private MaterialProperty metallic;
		private MaterialProperty smoothness;
		private MaterialProperty bumpScale;
		private MaterialProperty bumpMap;
		private MaterialProperty occlusionStrength;
		private MaterialProperty occlusionMap;
		private MaterialProperty heigtMapScale;
		private MaterialProperty heightMap;
		private MaterialProperty emissionScaleUI;
		private MaterialProperty emissionColorUI;
		private MaterialProperty emissionColorForRendering;
		private MaterialProperty emissionMap;
		private MaterialProperty detailMask;
		private MaterialProperty detailAlbedoMap;
		private MaterialProperty detailNormalMapScale;
		private MaterialProperty detailNormalMap;
		private MaterialProperty uvSetSecondary;
		private MaterialProperty vertexColor;

		private MaterialEditor m_MaterialEditor;
		private WorkflowMode m_WorkflowMode = WorkflowMode.Specular;

		private bool m_FirstTimeApply = true;

		public void FindProperties(MaterialProperty[] props)
		{
			blendMode = FindProperty("_Mode", props);
			albedoMap = FindProperty("_MainTex", props);
			albedoColor = FindProperty("_Color", props);
			alphaCutoff = FindProperty("_Cutoff", props);
			specularMap = FindProperty("_SpecGlossMap", props, false);
			specularColor = FindProperty("_SpecColor", props, false);
			metallicMap = FindProperty("_MetallicGlossMap", props, false);
			metallic = FindProperty("_Metallic", props, false);
			if (specularMap != null && specularColor != null)
				m_WorkflowMode = WorkflowMode.Specular;
			else if (metallicMap != null && metallic != null)
				m_WorkflowMode = WorkflowMode.Metallic;
			else
				m_WorkflowMode = WorkflowMode.Dielectric;
			smoothness = FindProperty("_Glossiness", props);
			bumpScale = FindProperty("_BumpScale", props);
			bumpMap = FindProperty("_BumpMap", props);
			heigtMapScale = FindProperty("_Parallax", props);
			heightMap = FindProperty("_ParallaxMap", props);
			occlusionStrength = FindProperty("_OcclusionStrength", props);
			occlusionMap = FindProperty("_OcclusionMap", props);
			emissionScaleUI = FindProperty("_EmissionScaleUI", props);
			emissionColorUI = FindProperty("_EmissionColorUI", props);
			emissionColorForRendering = FindProperty("_EmissionColor", props);
			emissionMap = FindProperty("_EmissionMap", props);
			detailMask = FindProperty("_DetailMask", props);
			detailAlbedoMap = FindProperty("_DetailAlbedoMap", props);
			detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
			detailNormalMap = FindProperty("_DetailNormalMap", props);
			uvSetSecondary = FindProperty("_UVSec", props);
			vertexColor = FindProperty("_IntensityVC", props);
		}

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
		{
			FindProperties(
				props
			); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
			m_MaterialEditor = materialEditor;
			var material = materialEditor.target as Material;

			ShaderPropertiesGUI(material);

			// Make sure that needed keywords are set up if we're switching some existing
			// material to a standard shader.
			if (m_FirstTimeApply)
			{
				SetMaterialKeywords(material, m_WorkflowMode);
				m_FirstTimeApply = false;
			}
		}

		public void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;

			// Detect any changes to the material
			EditorGUI.BeginChangeCheck();
			{
				BlendModePopup();

				// Primary properties
				GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
				DoAlbedoArea(material);
				DoSpecularMetallicArea();
				m_MaterialEditor.TexturePropertySingleLine(
					Styles.normalMapText,
					bumpMap,
					bumpMap.textureValue != null ? bumpScale : null
				);
				m_MaterialEditor.TexturePropertySingleLine(
					Styles.heightMapText,
					heightMap,
					heightMap.textureValue != null ? heigtMapScale : null
				);
				m_MaterialEditor.TexturePropertySingleLine(
					Styles.occlusionText,
					occlusionMap,
					occlusionMap.textureValue != null ? occlusionStrength : null
				);
				DoEmissionArea(material);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
				EditorGUI.BeginChangeCheck();
				m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
				if (EditorGUI.EndChangeCheck())
					emissionMap.textureScaleAndOffset =
						albedoMap
							.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

				EditorGUILayout.Space();

				// Secondary properties
				GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
				m_MaterialEditor.TexturePropertySingleLine(
					Styles.detailNormalMapText,
					detailNormalMap,
					detailNormalMapScale
				);
				m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMap);
				m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);

				EditorGUILayout.Space();

				m_MaterialEditor.ShaderProperty(vertexColor, "Vertex Color");
			}
			if (EditorGUI.EndChangeCheck())
				foreach (Object obj in blendMode.targets)
					MaterialChanged((Material)obj, m_WorkflowMode);
		}

		internal void DetermineWorkflow(MaterialProperty[] props)
		{
			if (FindProperty("_SpecGlossMap", props, false) != null && FindProperty("_SpecColor", props, false) != null)
				m_WorkflowMode = WorkflowMode.Specular;
			else if (FindProperty("_MetallicGlossMap", props, false) != null
			         && FindProperty("_Metallic", props, false) != null)
				m_WorkflowMode = WorkflowMode.Metallic;
			else
				m_WorkflowMode = WorkflowMode.Dielectric;
		}

		private void BlendModePopup()
		{
			EditorGUI.showMixedValue = blendMode.hasMixedValue;
			var mode = (BlendMode)blendMode.floatValue;

			EditorGUI.BeginChangeCheck();
			mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
			if (EditorGUI.EndChangeCheck())
			{
				m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
				blendMode.floatValue = (float)mode;
			}

			EditorGUI.showMixedValue = false;
		}

		private void DoAlbedoArea(Material material)
		{
			m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
			if ((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout)
				m_MaterialEditor.ShaderProperty(
					alphaCutoff,
					Styles.alphaCutoffText.text,
					MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1
				);
		}

		private void DoEmissionArea(Material material)
		{
			bool showEmissionColorAndGIControls = emissionScaleUI.floatValue > 0f;
			bool hadEmissionTexture = emissionMap.textureValue != null;

			// Do controls
			m_MaterialEditor.TexturePropertySingleLine(
				Styles.emissionText,
				emissionMap,
				showEmissionColorAndGIControls ? emissionColorUI : null,
				emissionScaleUI
			);

			// Set default emissionScaleUI if texture was assigned
			if (emissionMap.textureValue != null && !hadEmissionTexture && emissionScaleUI.floatValue <= 0f)
				emissionScaleUI.floatValue = 1.0f;

			// Dynamic Lightmapping mode
			if (showEmissionColorAndGIControls)
			{
				bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(EvalFinalEmissionColor(material));
				EditorGUI.BeginDisabledGroup(!shouldEmissionBeEnabled);

				m_MaterialEditor.LightmapEmissionProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);

				EditorGUI.EndDisabledGroup();
			}

			if (!HasValidEmissiveKeyword(material))
				EditorGUILayout.HelpBox(Styles.emissiveWarning.text, MessageType.Warning);
		}

		private void DoSpecularMetallicArea()
		{
			if (m_WorkflowMode == WorkflowMode.Specular)
			{
				if (specularMap.textureValue == null)
					m_MaterialEditor.TexturePropertyTwoLines(
						Styles.specularMapText,
						specularMap,
						specularColor,
						Styles.smoothnessText,
						smoothness
					);
				else
					m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap);
			}
			else if (m_WorkflowMode == WorkflowMode.Metallic)
			{
				if (metallicMap.textureValue == null)
					m_MaterialEditor.TexturePropertyTwoLines(
						Styles.metallicMapText,
						metallicMap,
						metallic,
						Styles.smoothnessText,
						smoothness
					);
				else
					m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap);
			}
		}

		public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
		{
			switch (blendMode)
			{
				case BlendMode.Opaque:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = -1;
					break;
				case BlendMode.Cutout:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.EnableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 2450;
					break;
				case BlendMode.Fade:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.EnableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
				case BlendMode.Transparent:
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
			}
		}

		// Calculate final HDR _EmissionColor (gamma space) from _EmissionColorUI (LDR, gamma) & _EmissionScaleUI (gamma)
		private static Color EvalFinalEmissionColor(Material material) =>
			material.GetColor("_EmissionColorUI") * material.GetFloat("_EmissionScaleUI");

		private static bool ShouldEmissionBeEnabled(Color color) => color.grayscale > 0.1f / 255.0f;

		private static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
		{
			// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
			// (MaterialProperty value might come from renderer material property block)
			SetKeyword(
				material,
				"_NORMALMAP",
				material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap")
			);
			if (workflowMode == WorkflowMode.Specular)
				SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
			else if (workflowMode == WorkflowMode.Metallic)
				SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
			SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
			SetKeyword(
				material,
				"_DETAIL_MULX2",
				material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap")
			);

			bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled(material.GetColor("_EmissionColor"));
			SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

			// Setup lightmap emissive flags
			MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
			if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive
			              | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
			{
				flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
				if (!shouldEmissionBeEnabled)
					flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

				material.globalIlluminationFlags = flags;
			}

			float intensity = material.GetFloat("_IntensityVC");
			if (intensity <= 0f)
			{
				SetKeyword(material, "_VERTEXCOLOR_LERP", false);
				SetKeyword(material, "_VERTEXCOLOR", false);
			}
			else if (intensity > 0f && intensity < 1f)
			{
				SetKeyword(material, "_VERTEXCOLOR_LERP", true);
				SetKeyword(material, "_VERTEXCOLOR", false);
			}
			else
			{
				SetKeyword(material, "_VERTEXCOLOR_LERP", false);
				SetKeyword(material, "_VERTEXCOLOR", true);
			}
			//SetKeyword(material, intensity < 1f?"_VERTEXCOLOR_LERP":"_VERTEXCOLOR", intensity > 0f);
		}

		private bool HasValidEmissiveKeyword(Material material)
		{
			// Material animation might be out of sync with the material keyword.
			// So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
			// (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
			bool hasEmissionKeyword = material.IsKeywordEnabled("_EMISSION");
			if (!hasEmissionKeyword && ShouldEmissionBeEnabled(emissionColorForRendering.colorValue))
				return false;
			return true;
		}

		private static void MaterialChanged(Material material, WorkflowMode workflowMode)
		{
			// Clamp EmissionScale to always positive
			if (material.GetFloat("_EmissionScaleUI") < 0.0f)
				material.SetFloat("_EmissionScaleUI", 0.0f);

			// Apply combined emission value
			Color emissionColorOut = EvalFinalEmissionColor(material);
			material.SetColor("_EmissionColor", emissionColorOut);

			// Handle Blending modes
			SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

			SetMaterialKeywords(material, workflowMode);
		}

		private static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword(keyword);
			else
				m.DisableKeyword(keyword);
		}
	}
}
