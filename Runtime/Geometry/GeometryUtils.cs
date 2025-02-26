using UnityEngine;

namespace DavidUtils.Geometry
{
	public static class GeometryUtils
	{
		public const float Epsilon = 0.00000001f;

		public static bool Equals(float a, float b) => Mathf.Abs(a - b) < Epsilon;
		public static bool Equals(Vector2 v1, Vector2 v2) => Mathf.Abs((v1 - v2).magnitude) < Epsilon;

		/// <summary>
		///     AreaTri (p,begin,end) == NEGATIVO => Esta a la Derecha de la Arista (begin -> end)
		/// </summary>
		public static bool IsRight(Vector2 begin, Vector2 end, Vector2 p) => TriArea2(begin, end, p) < -Epsilon;

		public static bool IsRight(Vector3 begin, Vector3 end, Vector3 p) => TriArea2(begin, end, p) < -Epsilon;

		public static bool IsLeft(Vector2 begin, Vector2 end, Vector2 p) => TriArea2(begin, end, p) > Epsilon;
		public static bool IsLeft(Vector3 begin, Vector3 end, Vector3 p) => TriArea2(begin, end, p) > Epsilon;
		
		public static bool IsColinear(Vector2 begin, Vector2 end, Vector2 p) => Equals(TriArea2(begin, end, p), 0);

		/// <summary>
		///     Area del Triangulo al Cuadrado (para clasificar puntos a la derecha o izquierda de un segmento)
		///     Area POSITIVA => IZQUIERDA
		///     Area NEGATIVA => DERECHA
		/// </summary>
		public static float TriArea2(Vector2 p1, Vector2 p2, Vector2 p3)
			=> Det3X3(p1.x, p1.y, 1, p2.x, p2.y, 1, p3.x, p3.y, 1);

		public static float TriArea2(Vector3 p1, Vector3 p2, Vector3 p3)
			=> Det3X3(p1.x, p1.z, 1, p2.x, p2.z, 1, p3.x, p3.z, 1);

		/// <summary>
		///     Determinante de una Matriz 3x3
		///     ((a,b,c),(d,e,f),(g,h,i)
		/// </summary>
		private static float Det3X3(
			float a, float b, float c,
			float d, float e, float f,
			float g, float h, float i
		) => a * e * i + g * b * f + c * d * h - c * e * g - i * d * b - a * h * f;

		#region Point In Tests

		/// <summary>
		///     <para>Comprueba si el punto p esta dentro del Circulo formado por a,b,c</para>
		///     <para>
		///         Implicitamente lo que hace es comprobar si el Angulo(a,b,c) <= Angulo(p,b,c).
		///         Siendo el Angulo del punto a y p
		///     </para>
		///     <para>Si p pertenece a la Circunferencia, se considera FUERA</para>
		/// </summary>
		/// <param name="p">Punto fuera o dentro</param>
		/// <returns>FALSE si esta fuera o si los 3 puntos a,b,c son colineares</returns>
		public static bool PointInCirle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
		{
			Vector2? centro = CircleCenter(a, b, c);

			// Son colineares, no hay circulo
			if (centro == null) return false;

			// Si el radio es mayor que la distancia de P al Centro => DENTRO
			if ((a - (Vector2)centro).magnitude > (p - (Vector2)centro).magnitude) return true;

			return false;
			//return angle(a, b, c) < angle(p, b, c);
		}

		/// <summary>
		///     Comprueba si el Punto p esta en linea definida por los puntos A,B
		/// </summary>
		/// <returns></returns>
		public static bool PointOnLine(Vector2 a, Vector2 b, Vector2 p) => IsColinear(a, b, p);

		/// <summary>
		///     Comprueba si el Punto P esta en el segmento A-B
		/// </summary>
		/// <returns></returns>
		public static bool PointOnSegment(Vector2 p, Vector2 a, Vector2 b) =>
			PointOnLine(a, b, p) && (p - a).magnitude + (p - b).magnitude <= (a - b).magnitude + Epsilon;

		#endregion

		#region ANGLE

		/// <summary>
		///     Calculo del angulo entre 2 Vectores (a->b) y (a->c)
		///     como el Arcoseno del Producto Escalar de los angulos normalizados
		/// </summary>
		private static float Angle(Vector2 a, Vector2 b, Vector2 c)
		{
			Vector2 u = (b - a).normalized;
			Vector2 v = (c - a).normalized;

			return Mathf.Acos(Vector2.Dot(u, v));
		}

		#endregion

		#region CIRCLES

		/// <summary>
		///     Calcula el Centro de un Circulo que pasa por 3 puntos (a,b,c)
		/// </summary>
		/// <returns>NULL si son colineares</returns>
		public static Vector2? CircleCenter(Vector2 a, Vector2 b, Vector2 c)
		{
			Vector2 abMediatriz = Vector2.Perpendicular(b - a).normalized;
			Vector2 bcMediatriz = Vector2.Perpendicular(b - c).normalized;

			Vector2 abMedio = a + (b - a) / 2;
			Vector2 bcMedio = b + (c - b) / 2;

			return IntersectionLineLine(abMedio, abMedio + abMediatriz, bcMedio, bcMedio + bcMediatriz);
		}

		#endregion


		#region PLANES

		public static bool IsAny2DPlane(this Plane plane) => plane.IsXYPlane() || plane.IsXZPlane() || plane.IsYZPlane();
		
		public static bool IsXYPlane(this Plane plane) => Mathf.Approximately(Mathf.Abs(plane.normal.z), 1);
		public static bool IsXZPlane(this Plane plane) => Mathf.Approximately(Mathf.Abs(plane.normal.y), 1);
		public static bool IsYZPlane(this Plane plane) => Mathf.Approximately(Mathf.Abs(plane.normal.x), 1);
		
		public static Vector3 GetPointProjected(this Plane plane, Vector3 p) 
			=> p + -plane.normal * plane.GetDistanceToPoint(p); 

		#endregion
		

		#region INTERSECTIONS

		#region LINES

		/// <summary>
		///     Calcula la interseccion de dos rectas definidas por los puntos (a,b) y (c,d)
		/// </summary>
		/// <returns>NULL si son paralelas</returns>
		public static Vector2? IntersectionLineLine(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		{
			Vector2 ab = b - a;
			Vector2 cd = d - c;
			Vector2 ac = c - a;

			// t = (cd x ac) / (ab x cd)
			// s = (ab x ap) / (ab x cd)
			// (x: Cross Product)

			float denominador = cd.x * ab.y - ab.x * cd.y;

			// Colinear
			if (Mathf.Abs(denominador) < Epsilon) return null;

			float t = (cd.x * ac.y - ac.x * cd.y) / denominador;

			return a + ab * t;
		}

		#endregion


		#region SEGMENTS

		/// <summary>
		///     Calcula el Punto de Interseccion entre la Linea definida por (a,b) y el Segmento (c,d)
		///     Si s esta en [0,1] => Interseccion en el segmento
		/// </summary>
		/// <returns>null if no intersection, or colinear</returns>
		public static Vector2? IntersectionLineSegment(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		{
			Vector2 ab = b - a;
			Vector2 cd = d - c;
			Vector2 ac = c - a;

			// t = (cd x ac) / (ab x cd)
			// s = (ab x ac) / (ab x cd)
			// (x: Cross Product)

			float denominador = cd.x * ab.y - ab.x * cd.y;

			// Colinear
			if (Mathf.Abs(denominador) < Epsilon) return null;

			float s = (ab.x * ac.y - ac.x * ab.y) / denominador;

			// Interseccion fuera del segmento, si se extienden intersectarian
			if (s is < 0 or > 1) return null;

			return c + cd * s;
		}

		/// <summary>
		///     Calcula el Punto de Interseccion entre dos segmentos (a,b) y (c,d)
		///     Si s y t estan en [0,1] => Interseccion en el segmento
		/// </summary>
		/// <returns>null if no intersection, or colinear</returns>
		public static Vector2? IntersectionSegmentSegment(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
		{
			Vector2 ab = b - a;
			Vector2 cd = d - c;
			Vector2 ac = c - a;

			// t = (cd x ac) / (ab x cd)
			// s = (ab x ap) / (ab x cd)
			// (x: Cross Product)

			float denominador = cd.x * ab.y - ab.x * cd.y;

			// Colinear
			if (Mathf.Abs(denominador) < Epsilon) return null;

			float t = (cd.x * ac.y - ac.x * cd.y) / denominador;
			float s = (ab.x * ac.y - ac.x * ab.y) / denominador;

			// Interseccion fuera del segmento, si se extienden intersectarian
			if (t is < 0 or > 1 || s is < 0 or > 1) return null;

			return a + ab * t;
		}

		#endregion

		
		#region RAYS

		/// <summary>
		///     Calcula el Punto de Interseccion de un Rayo (p,dir) sobre la linea definida por (a,b)
		///     Si s == 0 => La linea pasa por detras del rayo
		/// </summary>
		/// <returns></returns>
		public static Vector2? IntersectionRayLine(Vector2 p, Vector2 dir, Vector2 c, Vector2 d)
		{
			Vector2 a = p;
			Vector2 b = p + dir;
			Vector2 ab = b - a;
			Vector2 cd = d - c;
			Vector2 ac = c - a;

			// t = (cd x ac) / (ab x cd)
			// s = (ab x ap) / (ab x cd)
			// (x: Cross Product)

			float denominador = cd.x * ab.y - ab.x * cd.y;

			// Colinear
			if (Mathf.Abs(denominador) < Epsilon) return null;

			float t = (cd.x * ac.y - ac.x * cd.y) / denominador;
			float s = (ab.x * ac.y - ac.x * ab.y) / denominador;

			// La linea pasa por detras del rayo
			if (t < 0) return null;

			return p + dir * t;
		}

		/// <summary>
		///     Calcula el Punto de Interseccion de un Rayo (p,dir) con un segmento (a,b)
		/// </summary>
		/// <returns></returns>
		public static Vector2? IntersectionRaySegment(Vector2 p, Vector2 dir, Vector2 c, Vector2 d)
		{
			Vector2 a = p;
			Vector2 b = p + dir;
			Vector2 ab = b - a;
			Vector2 cd = d - c;
			Vector2 ac = c - a;

			// t = (cd x ac) / (ab x cd)
			// s = (ab x ap) / (ab x cd)
			// (x: Cross Product)

			float denominador = cd.x * ab.y - ab.x * cd.y;

			// Colinear
			if (Mathf.Abs(denominador) < Epsilon) return null;

			float t = (cd.x * ac.y - ac.x * cd.y) / denominador;
			float s = (ab.x * ac.y - ac.x * ab.y) / denominador;

			// El segmento esta detras del rayo o justo el rayo no lo intersecta
			if (t < 0 || s is < 0 or > 1) return null;

			return p + dir * t;
		}

		#endregion


		#region RAYS on PLANES

		public static bool IntersectionRayPlane(Ray ray, Plane plane, out Vector3 intersection)
		{
			intersection = Vector3.zero;
			
			// Dot Product == 0 => Ray is parallel to plane
			// Dot Product > 0 => Ray is pointing away from the plane
			float dot = Vector3.Dot(plane.normal, ray.direction);
			if (dot >= 0) return false;

			float t;
            
			// Raycast with any other Plane
			if (!plane.Raycast(ray, out t)) return false;
            
			intersection = ray.GetPoint(t);
            
			// Debug.Log($"p in Hex Plane is {intersection.ToString()}" +
			//           (plane.IsAny2DPlane() ? $"Plane was {(plane.IsXYPlane() ? "XY" : plane.IsXZPlane() ? "XZ" : "YZ")}" : ""));
            
			return true;
		}

		public static bool IntersectionRayPlane(Vector3 p, Vector3 dir, Vector3 pNormal, Vector3 pPoint,
			out Vector3 intersection) =>
			IntersectionRayPlane(new Ray(p, dir), new Plane(pNormal, pPoint), out intersection);

		#endregion

		#endregion


		#region PROJECTION

		/// <summary>
		/// Projection of a point P over a line defined by A and B
		/// </summary>
		/// <param name="p"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Vector2 Projection(Vector2 p, Vector2 a, Vector2 b)
		{
			Vector2 ab = b - a;
			Vector2 ap = p - a;

			float s = Vector2.Dot(ap, ab) / ab.sqrMagnitude;

			return a + ab * s;
		}

		#endregion
		

		#region DISTANCE TO LINE

		/// <summary>
		///     Distancia más corta desde el punto P a la recta definida por los puntos A y B
		/// </summary>
		/// <param name="p"></param>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>Distancia más corta</returns>
		public static float DistanceToLine(Vector2 p, Vector2 a, Vector2 b) => 
			(p - Projection(p,a,b)).magnitude;

		#endregion

		
		#region CONVEXITY & CONCAVITY

		public static bool IsConvex(Vector2 a, Vector2 b, Vector2 c) => IsLeft(a, b, c);
		public static bool IsConcave(Vector2 a, Vector2 b, Vector2 c) => IsRight(a, b, c);

		public static bool IsConvex(this Vector2[] vertices)
		{
			switch (vertices.Length)
			{
				case < 3:
					return false;
				case 3:
					return IsConvex(vertices[0], vertices[1], vertices[2]);
			}

			for (var i = 0; i < vertices.Length; i++)
			{
				if (IsRight(vertices[i], vertices[(i + 1) % vertices.Length], vertices[(i + 2) % vertices.Length]))
					return false;
			}

			return true;
		}

		public static bool IsConcave(this Vector2[] vertices) => !IsConvex(vertices);

		#endregion
	}
}
