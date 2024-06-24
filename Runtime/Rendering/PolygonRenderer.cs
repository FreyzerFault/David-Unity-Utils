using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using DavidUtils.Geometry.MeshExtensions;
using DavidUtils.Rendering.Extensions;
using UnityEngine;
using UnityEngine.Serialization;

namespace DavidUtils.Rendering
{
	[Serializable]
	public class PolygonRenderer : DynamicRenderer<Polygon[]>
	{
		public enum PolygonRenderMode
		{
			Wire,
			Mesh,
			OutlinedMesh
		}

		[Serializable]
		public class PolygonRenderData
		{
			private Polygon _polygon;
			private Color _color;
			private float _thickness;
			private PolygonRenderMode _renderMode;

			// SCALE SLIDER
			[FormerlySerializedAs("centeredScale")]
			[SerializeField] [Range(.2f, 1)]
			private float _centeredScale = 1;

			public LineRenderer lineRenderer;
			public MeshFilter meshFilter;
			public MeshRenderer meshRenderer;

			public PolygonRenderData(
				Polygon polygon, Color color, float thickness, PolygonRenderMode renderMode, float centeredScale,
				LineRenderer lineRenderer,
				MeshFilter meshFilter, MeshRenderer meshRenderer
			)
			{
				_polygon = polygon;
				_color = color;
				_thickness = thickness;
				_renderMode = renderMode;
				_centeredScale = centeredScale;
				this.lineRenderer = lineRenderer;
				this.meshFilter = meshFilter;
				this.meshRenderer = meshRenderer;
			}

			public PolygonRenderData(
				Polygon polygon, Transform parent, string name = null, Color? color = null, float thickness = 1f,
				PolygonRenderMode renderMode = PolygonRenderMode.Mesh, float centeredScale = 1
			)
			{
				_polygon = polygon;
				_color = color ?? Color.white;
				_thickness = thickness;
				_renderMode = renderMode;
				_centeredScale = centeredScale;

				lineRenderer = polygon.ToLineRenderer(parent, $"{name ?? "Polygon"}", _color, _thickness);

				polygon.InstantiateMesh(
					out MeshRenderer mr,
					out MeshFilter mf,
					parent,
					$"{name ?? "Polygon"}",
					_color
				);
				meshRenderer = mr;
				meshFilter = mf;
			}

			public void Destroy()
			{
				UnityUtils.DestroySafe(meshFilter.gameObject);
				UnityUtils.DestroySafe(lineRenderer.gameObject);
			}

			public void ProjectOnTerrain(Terrain terrain, float offset = 0.1f)
			{
				terrain.ProjectMeshInTerrain(meshFilter.sharedMesh, meshFilter.transform, offset);
				lineRenderer.SetPoints(lineRenderer.GetPoints().Select(p => terrain.Project(p, offset)).ToArray());
			}

			private Polygon ScaledPolygon => _polygon.ScaleByCenter(_centeredScale);

			#region MODIFIABLE PROPERTIES

			public PolygonRenderMode Mode
			{
				get => _renderMode;
				set
				{
					_renderMode = value;
					bool line = _renderMode is PolygonRenderMode.Wire or PolygonRenderMode.OutlinedMesh;
					bool solid = _renderMode is PolygonRenderMode.Mesh or PolygonRenderMode.OutlinedMesh;
					lineRenderer.gameObject.SetActive(line);
					meshRenderer.gameObject.SetActive(solid);
					meshFilter.gameObject.SetActive(solid);
				}
			}


			public Polygon Polygon
			{
				get => _polygon;
				set
				{
					lineRenderer.SetPolygon(ScaledPolygon);
					meshFilter.mesh.SetPolygon(ScaledPolygon);
				}
			}

			public Color Color
			{
				get => _color;
				set
				{
					_color = value;
					lineRenderer.startColor = lineRenderer.endColor = _color;
					meshFilter.mesh.SetColors(_color.ToFilledArray(meshFilter.mesh.vertexCount).ToArray());
				}
			}

			public float Thickness
			{
				get => _thickness;
				set
				{
					_thickness = value;
					lineRenderer.startWidth = lineRenderer.endWidth = _thickness;
				}
			}

			public float CenteredScale
			{
				get => _centeredScale;
				set
				{
					_centeredScale = value;
					lineRenderer.SetPolygon(ScaledPolygon);
					meshFilter.mesh.SetPolygon(ScaledPolygon);
				}
			}

			#endregion
		}

		protected override string DefaultChildName => "Polygon";

		private const float DEFAULT_THICKNESS = 1f;
		private const PolygonRenderMode DEFAULT_RENDER_MODE = PolygonRenderMode.Mesh;
		private const float DEFAULT_CENTERED_SCALE = 1f;


		private List<PolygonRenderData> _data = new();


		#region SINGLE POLYGON

		public Polygon Polygon
		{
			get => _data[0].Polygon;
			set => _data[0].Polygon = value;
		}

		public float Thickness
		{
			get => _data[0].Thickness;
			set => _data.ForEach(d => d.Thickness = value);
		}

		public Color MainColor
		{
			get => _data[0].Color;
			set => _data.ForEach(d => d.Color = value);
		}

		public PolygonRenderMode RenderMode
		{
			get => _data[0].Mode;
			set => _data[0].Mode = value;
		}

		public float CenteredScale
		{
			get => _data[0].CenteredScale;
			set => _data[0].CenteredScale = value;
		}

		#endregion


		#region POLYGON GROUP

		public Polygon[] Polygons
		{
			get => _data.Select(d => d.Polygon).ToArray();
			set => _data.ForEach((d, i) => d.Polygon = value[i]);
		}

		public Color[] Colors
		{
			get => _data.Select(d => d.Color).ToArray();
			set => _data.ForEach((d, i) => d.Color = value[i]);
		}

		public float[] Thicknesses
		{
			get => _data.Select(d => d.Thickness).ToArray();
			set => _data.ForEach((d, i) => d.Thickness = value[i]);
		}

		public PolygonRenderMode[] AllRenderModes
		{
			get => _data.Select(d => d.Mode).ToArray();
			set => _data.ForEach((d, i) => d.Mode = value[i]);
		}


		public float[] AllCenteredScales
		{
			get => _data.Select(d => d.CenteredScale).ToArray();
			set => _data.ForEach((d, i) => d.CenteredScale = value[i]);
		}

		#endregion


		#region MODIFY INDIVIDUAL POLYGON

		// Set Individually
		public void SetPolygon(int i, Polygon polygon) => _data[i].Polygon = polygon;
		public void SetColor(int i, Color color) => _data[i].Color = color;
		public void SetThickness(int i, float thickness) => _data[i].Thickness = thickness;
		public void SetRenderMode(int i, PolygonRenderMode mode) => _data[i].Mode = mode;
		public void SetScale(int i, float scale) => _data[i].CenteredScale = scale;

		#endregion

		public override void Instantiate(Polygon[] points, string childName = null) =>
			UpdateGeometry(points);

		/// <summary>
		///     Update ALL Renderers
		///     Si hay mas Regions que Renderers, instancia nuevos
		///     Elimina los Renderers sobrantes
		/// </summary>
		public override void UpdateGeometry(Polygon[] points)
		{
			if (points.Length == _data.Count)
			{
				Polygons = points;
			}
			else
			{
				SetRainbowColors(points.Length);
				// Update or ADD polygon
				for (var i = 0; i < points.Length; i++)
					if (i < _data.Count)
						SetPolygon(i, points[i]);
					else
						InstatiatePolygon(points[i], DefaultChildName, colors[i]);
			}

			// Elimina los Poligonos sobrantes
			int removeCount = _data.Count - points.Length;
			if (removeCount <= 0) return;
			RemovePolygon(points.Length, removeCount);
		}


		private PolygonRenderData InstatiatePolygon(
			Polygon polygon, string polygonName = null, Color? color = null, float thickness = 1f,
			PolygonRenderMode renderMode = PolygonRenderMode.Mesh, float centeredScale = 1
		)
		{
			_data.Add(
				new PolygonRenderData(
					polygon,
					transform,
					polygonName ?? DefaultChildName,
					color,
					thickness,
					renderMode,
					centeredScale
				)
			);
			return _data[^1];
		}

		public bool RemovePolygon(int index, int count = 1)
		{
			if (index < 0 || index >= _data.Count) return false;

			for (var i = 0; i < count; i++) _data[index + i].Destroy();

			_data.RemoveRange(index, count);

			return true;
		}

		public override void Clear()
		{
			base.Clear();

			RemovePolygon(0, _data.Count);
		}


		#region PROJECTION ON TERRAIN

		public void ProjectAllOnTerrain(Terrain terrain, float offset = 0.1f) =>
			_data.ForEach(d => d.ProjectOnTerrain(terrain, offset));

		#endregion
	}
}
