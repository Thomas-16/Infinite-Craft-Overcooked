// This file is automatically generated by a script based on the CommandBuilder API.
// This file adds additional overloads to the CommandBuilder API.
using Unity.Burst;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using static Pathfinding.Drawing.CommandBuilder;

namespace Pathfinding.Drawing {
	public partial struct CommandBuilder2D {
		/// <summary>\copydocref{CommandBuilder.WithMatrix(Matrix4x4)}</summary>
		[BurstDiscard]
		public ScopeMatrix WithMatrix (Matrix4x4 matrix) {
			return draw.WithMatrix(matrix);
		}
		/// <summary>\copydocref{CommandBuilder.WithMatrix(float3x3)}</summary>
		[BurstDiscard]
		public ScopeMatrix WithMatrix (float3x3 matrix) {
			return draw.WithMatrix(matrix);
		}
		/// <summary>\copydocref{CommandBuilder.WithColor(Color)}</summary>
		[BurstDiscard]
		public ScopeColor WithColor (Color color) {
			return draw.WithColor(color);
		}

		/// <summary>\copydocref{CommandBuilder.WithLineWidth(float,bool)}</summary>
		[BurstDiscard]
		public ScopeLineWidth WithLineWidth (float pixels, bool automaticJoins = true) {
			return draw.WithLineWidth(pixels, automaticJoins);
		}


		/// <summary>\copydocref{CommandBuilder.PushMatrix(Matrix4x4)}</summary>
		public void PushMatrix (Matrix4x4 matrix) {
			draw.PushMatrix(matrix);
		}
		/// <summary>\copydocref{CommandBuilder.PushMatrix(float4x4)}</summary>
		public void PushMatrix (float4x4 matrix) {
			draw.PushMatrix(matrix);
		}


		/// <summary>\copydocref{CommandBuilder.PopMatrix()}</summary>
		public void PopMatrix () {
			draw.PopMatrix();
		}








		/// <summary>\copydocref{CommandBuilder.Line(Vector3,Vector3)}</summary>
		public void Line (Vector3 a, Vector3 b) {
			draw.Line(a, b);
		}
		/// <summary>\copydocref{CommandBuilder.Line(Vector3,Vector3)}</summary>
		public void Line (Vector2 a, Vector2 b) {
			Line(xy ? new Vector3(a.x, a.y, 0) : new Vector3(a.x, 0, a.y), xy ? new Vector3(b.x, b.y, 0) : new Vector3(b.x, 0, b.y));
		}
		/// <summary>\copydocref{CommandBuilder.Line(Vector3,Vector3,Color)}</summary>
		public void Line (Vector3 a, Vector3 b, Color color) {
			draw.Line(a, b, color);
		}
		/// <summary>\copydocref{CommandBuilder.Line(Vector3,Vector3,Color)}</summary>
		public void Line (Vector2 a, Vector2 b, Color color) {
			Line(xy ? new Vector3(a.x, a.y, 0) : new Vector3(a.x, 0, a.y), xy ? new Vector3(b.x, b.y, 0) : new Vector3(b.x, 0, b.y), color);
		}
		/// <summary>\copydocref{CommandBuilder.Ray(float3,float3)}</summary>
		public void Ray (float3 origin, float3 direction) {
			draw.Ray(origin, direction);
		}
		/// <summary>\copydocref{CommandBuilder.Ray(float3,float3)}</summary>
		public void Ray (float2 origin, float2 direction) {
			Ray(xy ? new float3(origin, 0) : new float3(origin.x, 0, origin.y), xy ? new float3(direction, 0) : new float3(direction.x, 0, direction.y));
		}
		/// <summary>\copydocref{CommandBuilder.Ray(Ray,float)}</summary>
		public void Ray (Ray ray, float length) {
			draw.Ray(ray, length);
		}
		/// <summary>\copydocref{CommandBuilder.Arc(float3,float3,float3)}</summary>
		public void Arc (float3 center, float3 start, float3 end) {
			draw.Arc(center, start, end);
		}
		/// <summary>\copydocref{CommandBuilder.Arc(float3,float3,float3)}</summary>
		public void Arc (float2 center, float2 start, float2 end) {
			Arc(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), xy ? new float3(start, 0) : new float3(start.x, 0, start.y), xy ? new float3(end, 0) : new float3(end.x, 0, end.y));
		}




		/// <summary>\copydocref{CommandBuilder.Polyline(List<Vector3>,bool)}</summary>
		[BurstDiscard]
		public void Polyline (List<Vector3> points, bool cycle = false) {
			draw.Polyline(points, cycle);
		}
		/// <summary>\copydocref{CommandBuilder.Polyline(Vector3[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (Vector3[] points, bool cycle = false) {
			draw.Polyline(points, cycle);
		}
		/// <summary>\copydocref{CommandBuilder.Polyline(float3[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (float3[] points, bool cycle = false) {
			draw.Polyline(points, cycle);
		}
		/// <summary>\copydocref{CommandBuilder.Polyline(NativeArray<float3>,bool)}</summary>
		public void Polyline (NativeArray<float3> points, bool cycle = false) {
			draw.Polyline(points, cycle);
		}



		/// <summary>\copydocref{CommandBuilder.Cross(float3,float)}</summary>
		public void Cross (float3 position, float size = 1) {
			draw.Cross(position, size);
		}
		/// <summary>\copydocref{CommandBuilder.Bezier(float3,float3,float3,float3)}</summary>
		public void Bezier (float3 p0, float3 p1, float3 p2, float3 p3) {
			draw.Bezier(p0, p1, p2, p3);
		}
		/// <summary>\copydocref{CommandBuilder.Bezier(float3,float3,float3,float3)}</summary>
		public void Bezier (float2 p0, float2 p1, float2 p2, float2 p3) {
			Bezier(xy ? new float3(p0, 0) : new float3(p0.x, 0, p0.y), xy ? new float3(p1, 0) : new float3(p1.x, 0, p1.y), xy ? new float3(p2, 0) : new float3(p2.x, 0, p2.y), xy ? new float3(p3, 0) : new float3(p3.x, 0, p3.y));
		}



		/// <summary>\copydocref{CommandBuilder.Arrow(float3,float3)}</summary>
		public void Arrow (float3 from, float3 to) {
			ArrowRelativeSizeHead(from, to, xy ? XY_UP : XZ_UP, 0.2f);
		}
		/// <summary>\copydocref{CommandBuilder.Arrow(float3,float3)}</summary>
		public void Arrow (float2 from, float2 to) {
			Arrow(xy ? new float3(from, 0) : new float3(from.x, 0, from.y), xy ? new float3(to, 0) : new float3(to.x, 0, to.y));
		}
		/// <summary>\copydocref{CommandBuilder.Arrow(float3,float3,float3,float)}</summary>
		public void Arrow (float3 from, float3 to, float3 up, float headSize) {
			draw.Arrow(from, to, up, headSize);
		}
		/// <summary>\copydocref{CommandBuilder.Arrow(float3,float3,float3,float)}</summary>
		public void Arrow (float2 from, float2 to, float2 up, float headSize) {
			Arrow(xy ? new float3(from, 0) : new float3(from.x, 0, from.y), xy ? new float3(to, 0) : new float3(to.x, 0, to.y), xy ? new float3(up, 0) : new float3(up.x, 0, up.y), headSize);
		}
		/// <summary>\copydocref{CommandBuilder.ArrowRelativeSizeHead(float3,float3,float3,float)}</summary>
		public void ArrowRelativeSizeHead (float3 from, float3 to, float3 up, float headFraction) {
			draw.ArrowRelativeSizeHead(from, to, up, headFraction);
		}
		/// <summary>\copydocref{CommandBuilder.ArrowRelativeSizeHead(float3,float3,float3,float)}</summary>
		public void ArrowRelativeSizeHead (float2 from, float2 to, float2 up, float headFraction) {
			ArrowRelativeSizeHead(xy ? new float3(from, 0) : new float3(from.x, 0, from.y), xy ? new float3(to, 0) : new float3(to.x, 0, to.y), xy ? new float3(up, 0) : new float3(up.x, 0, up.y), headFraction);
		}




		/// <summary>\copydocref{CommandBuilder.ArrowheadArc(float3,float3,float,float)}</summary>
		public void ArrowheadArc (float3 origin, float3 direction, float offset, float width = 60) {
			if (!math.any(direction)) return;
			if (offset < 0) throw new System.ArgumentOutOfRangeException(nameof(offset));
			if (offset == 0) return;

			var rot = Quaternion.LookRotation(direction, xy ? XY_UP : XZ_UP);
			PushMatrix(Matrix4x4.TRS(origin, rot, Vector3.one));
			var a1 = math.PI * 0.5f - width * (0.5f * Mathf.Deg2Rad);
			var a2 = math.PI * 0.5f + width * (0.5f * Mathf.Deg2Rad);
			draw.CircleXZInternal(float3.zero, offset, a1, a2);
			var p1 = new float3(math.cos(a1), 0, math.sin(a1)) * offset;
			var p2 = new float3(math.cos(a2), 0, math.sin(a2)) * offset;
			const float sqrt2 = 1.4142f;
			var p3 = new float3(0, 0, sqrt2 * offset);
			Line(p1, p3);
			Line(p3, p2);
			PopMatrix();
		}
		/// <summary>\copydocref{CommandBuilder.ArrowheadArc(float3,float3,float,float)}</summary>
		public void ArrowheadArc (float2 origin, float2 direction, float offset, float width = 60) {
			ArrowheadArc(xy ? new float3(origin, 0) : new float3(origin.x, 0, origin.y), xy ? new float3(direction, 0) : new float3(direction.x, 0, direction.y), offset, width);
		}


		/// <summary>\copydocref{CommandBuilder.WireRectangle(float3,quaternion,float2)}</summary>
		public void WireRectangle (float3 center, quaternion rotation, float2 size) {
			draw.WireRectangle(center, rotation, size);
		}
		/// <summary>\copydocref{CommandBuilder.WireRectangle(float3,quaternion,float2)}</summary>
		public void WireRectangle (float2 center, quaternion rotation, float2 size) {
			WireRectangle(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), rotation, size);
		}
























		/// <summary>\copydocref{Ray(float3,float3)}</summary>
		public void Ray (float3 origin, float3 direction, Color color) {
			draw.Ray(origin, direction, color);
		}
		/// <summary>\copydocref{Ray(float2,float2)}</summary>
		public void Ray (float2 origin, float2 direction, Color color) {
			Ray(xy ? new float3(origin, 0) : new float3(origin.x, 0, origin.y), xy ? new float3(direction, 0) : new float3(direction.x, 0, direction.y), color);
		}
		/// <summary>\copydocref{Ray(Ray,float)}</summary>
		public void Ray (Ray ray, float length, Color color) {
			draw.Ray(ray, length, color);
		}
		/// <summary>\copydocref{Arc(float3,float3,float3)}</summary>
		public void Arc (float3 center, float3 start, float3 end, Color color) {
			draw.Arc(center, start, end, color);
		}
		/// <summary>\copydocref{Arc(float2,float2,float2)}</summary>
		public void Arc (float2 center, float2 start, float2 end, Color color) {
			Arc(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), xy ? new float3(start, 0) : new float3(start.x, 0, start.y), xy ? new float3(end, 0) : new float3(end.x, 0, end.y), color);
		}






		/// <summary>\copydocref{Polyline(List<Vector3>,bool)}</summary>
		[BurstDiscard]
		public void Polyline (List<Vector3> points, bool cycle, Color color) {
			draw.Polyline(points, cycle, color);
		}
		/// <summary>\copydocref{Polyline(List<Vector3>,bool)}</summary>
		[BurstDiscard]
		public void Polyline (List<Vector3> points, Color color) {
			Polyline(points, false, color);
		}
		/// <summary>\copydocref{Polyline(Vector3[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (Vector3[] points, bool cycle, Color color) {
			draw.Polyline(points, cycle, color);
		}
		/// <summary>\copydocref{Polyline(Vector3[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (Vector3[] points, Color color) {
			Polyline(points, false, color);
		}
		/// <summary>\copydocref{Polyline(float3[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (float3[] points, bool cycle, Color color) {
			draw.Polyline(points, cycle, color);
		}
		/// <summary>\copydocref{Polyline(float3[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (float3[] points, Color color) {
			Polyline(points, false, color);
		}
		/// <summary>\copydocref{Polyline(NativeArray<float3>,bool)}</summary>
		public void Polyline (NativeArray<float3> points, bool cycle, Color color) {
			draw.Polyline(points, cycle, color);
		}
		/// <summary>\copydocref{Polyline(NativeArray<float3>,bool)}</summary>
		public void Polyline (NativeArray<float3> points, Color color) {
			Polyline(points, false, color);
		}



		/// <summary>\copydocref{Cross(float3,float)}</summary>
		public void Cross (float3 position, float size, Color color) {
			draw.Cross(position, size, color);
		}
		/// <summary>\copydocref{Cross(float3,float)}</summary>
		public void Cross (float3 position, Color color) {
			Cross(position, 1, color);
		}
		/// <summary>\copydocref{Bezier(float3,float3,float3,float3)}</summary>
		public void Bezier (float3 p0, float3 p1, float3 p2, float3 p3, Color color) {
			draw.Bezier(p0, p1, p2, p3, color);
		}
		/// <summary>\copydocref{Bezier(float2,float2,float2,float2)}</summary>
		public void Bezier (float2 p0, float2 p1, float2 p2, float2 p3, Color color) {
			Bezier(xy ? new float3(p0, 0) : new float3(p0.x, 0, p0.y), xy ? new float3(p1, 0) : new float3(p1.x, 0, p1.y), xy ? new float3(p2, 0) : new float3(p2.x, 0, p2.y), xy ? new float3(p3, 0) : new float3(p3.x, 0, p3.y), color);
		}



		/// <summary>\copydocref{Arrow(float3,float3)}</summary>
		public void Arrow (float3 from, float3 to, Color color) {
			ArrowRelativeSizeHead(from, to, xy ? XY_UP : XZ_UP, 0.2f, color);
		}
		/// <summary>\copydocref{Arrow(float2,float2)}</summary>
		public void Arrow (float2 from, float2 to, Color color) {
			Arrow(xy ? new float3(from, 0) : new float3(from.x, 0, from.y), xy ? new float3(to, 0) : new float3(to.x, 0, to.y), color);
		}
		/// <summary>\copydocref{Arrow(float3,float3,float3,float)}</summary>
		public void Arrow (float3 from, float3 to, float3 up, float headSize, Color color) {
			draw.Arrow(from, to, up, headSize, color);
		}
		/// <summary>\copydocref{Arrow(float2,float2,float2,float)}</summary>
		public void Arrow (float2 from, float2 to, float2 up, float headSize, Color color) {
			Arrow(xy ? new float3(from, 0) : new float3(from.x, 0, from.y), xy ? new float3(to, 0) : new float3(to.x, 0, to.y), xy ? new float3(up, 0) : new float3(up.x, 0, up.y), headSize, color);
		}
		/// <summary>\copydocref{ArrowRelativeSizeHead(float3,float3,float3,float)}</summary>
		public void ArrowRelativeSizeHead (float3 from, float3 to, float3 up, float headFraction, Color color) {
			draw.ArrowRelativeSizeHead(from, to, up, headFraction, color);
		}
		/// <summary>\copydocref{ArrowRelativeSizeHead(float2,float2,float2,float)}</summary>
		public void ArrowRelativeSizeHead (float2 from, float2 to, float2 up, float headFraction, Color color) {
			ArrowRelativeSizeHead(xy ? new float3(from, 0) : new float3(from.x, 0, from.y), xy ? new float3(to, 0) : new float3(to.x, 0, to.y), xy ? new float3(up, 0) : new float3(up.x, 0, up.y), headFraction, color);
		}




		/// <summary>\copydocref{ArrowheadArc(float3,float3,float,float)}</summary>
		public void ArrowheadArc (float3 origin, float3 direction, float offset, float width, Color color) {
			if (!math.any(direction)) return;
			if (offset < 0) throw new System.ArgumentOutOfRangeException(nameof(offset));
			if (offset == 0) return;
			draw.PushColor(color);

			var rot = Quaternion.LookRotation(direction, xy ? XY_UP : XZ_UP);
			PushMatrix(Matrix4x4.TRS(origin, rot, Vector3.one));
			var a1 = math.PI * 0.5f - width * (0.5f * Mathf.Deg2Rad);
			var a2 = math.PI * 0.5f + width * (0.5f * Mathf.Deg2Rad);
			draw.CircleXZInternal(float3.zero, offset, a1, a2);
			var p1 = new float3(math.cos(a1), 0, math.sin(a1)) * offset;
			var p2 = new float3(math.cos(a2), 0, math.sin(a2)) * offset;
			const float sqrt2 = 1.4142f;
			var p3 = new float3(0, 0, sqrt2 * offset);
			Line(p1, p3);
			Line(p3, p2);
			PopMatrix();
			draw.PopColor();
		}
		/// <summary>\copydocref{ArrowheadArc(float3,float3,float,float)}</summary>
		public void ArrowheadArc (float3 origin, float3 direction, float offset, Color color) {
			ArrowheadArc(origin, direction, offset, 60, color);
		}
		/// <summary>\copydocref{ArrowheadArc(float2,float2,float,float)}</summary>
		public void ArrowheadArc (float2 origin, float2 direction, float offset, float width, Color color) {
			ArrowheadArc(xy ? new float3(origin, 0) : new float3(origin.x, 0, origin.y), xy ? new float3(direction, 0) : new float3(direction.x, 0, direction.y), offset, width, color);
		}
		/// <summary>\copydocref{ArrowheadArc(float2,float2,float,float)}</summary>
		public void ArrowheadArc (float2 origin, float2 direction, float offset, Color color) {
			ArrowheadArc(origin, direction, offset, 60, color);
		}


		/// <summary>\copydocref{WireRectangle(float3,quaternion,float2)}</summary>
		public void WireRectangle (float3 center, quaternion rotation, float2 size, Color color) {
			draw.WireRectangle(center, rotation, size, color);
		}
		/// <summary>\copydocref{WireRectangle(float2,quaternion,float2)}</summary>
		public void WireRectangle (float2 center, quaternion rotation, float2 size, Color color) {
			WireRectangle(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), rotation, size, color);
		}


































		/// <summary>\copydocref{Line(float3,float3)}</summary>
		public void Line (float3 a, float3 b, Color color) {
			draw.Line(a, b, color);
		}
		/// <summary>\copydocref{Circle(float2,float,float,float)}</summary>
		/// <param name="color">Color of the object</param>
		public void Circle (float2 center, float radius, float startAngle, float endAngle, Color color) {
			Circle(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), radius, startAngle, endAngle, color);
		}
		/// <summary>\copydocref{Circle(float2,float,float,float)}</summary>
		/// <param name="color">Color of the object</param>
		public void Circle (float2 center, float radius, Color color) {
			Circle(center, radius, 0f, 2 * math.PI, color);
		}
		/// <summary>\copydocref{Circle(float3,float,float,float)}</summary>
		/// <param name="color">Color of the object</param>
		public void Circle (float3 center, float radius, float startAngle, float endAngle, Color color) {
			draw.PushColor(color);
			if (xy) {
				draw.PushMatrix(XZ_TO_XY_MATRIX);
				draw.CircleXZInternal(new float3(center.x, center.z, center.y), radius, startAngle, endAngle);
				draw.PopMatrix();
			} else {
				draw.CircleXZInternal(center, radius, startAngle, endAngle);
			}
			draw.PopColor();
		}
		/// <summary>\copydocref{Circle(float3,float,float,float)}</summary>
		/// <param name="color">Color of the object</param>
		public void Circle (float3 center, float radius, Color color) {
			Circle(center, radius, 0f, 2 * math.PI, color);
		}




		/// <summary>\copydocref{WirePill(float2,float2,float)}</summary>
		/// <param name="color">Color of the object</param>
		public void WirePill (float2 a, float2 b, float radius, Color color) {
			WirePill(a, b - a, math.length(b - a), radius, color);
		}
		/// <summary>\copydocref{WirePill(float2,float2,float,float)}</summary>
		/// <param name="color">Color of the object</param>
		public void WirePill (float2 position, float2 direction, float length, float radius, Color color) {
			draw.PushColor(color);
			direction = math.normalizesafe(direction);

			if (radius <= 0) {
				Line(position, position + direction * length);
			} else if (length <= 0 || math.all(direction == 0)) {
				Circle(position, radius);
			} else {
				float4x4 m;
				if (xy) {
					m = new float4x4(
						new float4(direction, 0, 0),
						new float4(math.cross(new float3(direction, 0), XY_UP), 0),
						new float4(0, 0, 1, 0),
						new float4(position, 0, 1)
						);
				} else {
					m = new float4x4(
						new float4(direction.x, 0, direction.y, 0),
						new float4(0, 1, 0, 0),
						new float4(math.cross(new float3(direction.x, 0, direction.y), XZ_UP), 0),
						new float4(position.x, 0, position.y, 1)
						);
				}
				draw.PushMatrix(m);
				Circle(new float2(0, 0), radius, 0.5f * math.PI, 1.5f * math.PI);
				Line(new float2(0, -radius), new float2(length, -radius));
				Circle(new float2(length, 0), radius, -0.5f * math.PI, 0.5f * math.PI);
				Line(new float2(0, radius), new float2(length, radius));
				draw.PopMatrix();
			}
			draw.PopColor();
		}
		/// <summary>\copydocref{Polyline(List<Vector2>,bool)}</summary>
		[BurstDiscard]
		public void Polyline (List<Vector2> points, bool cycle, Color color) {
			draw.PushColor(color);
			for (int i = 0; i < points.Count - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Count > 1) Line(points[points.Count - 1], points[0]);
			draw.PopColor();
		}
		/// <summary>\copydocref{Polyline(List<Vector2>,bool)}</summary>
		[BurstDiscard]
		public void Polyline (List<Vector2> points, Color color) {
			Polyline(points, false, color);
		}
		/// <summary>\copydocref{Polyline(Vector2[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (Vector2[] points, bool cycle, Color color) {
			draw.PushColor(color);
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
			draw.PopColor();
		}
		/// <summary>\copydocref{Polyline(Vector2[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (Vector2[] points, Color color) {
			Polyline(points, false, color);
		}
		/// <summary>\copydocref{Polyline(float2[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (float2[] points, bool cycle, Color color) {
			draw.PushColor(color);
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
			draw.PopColor();
		}
		/// <summary>\copydocref{Polyline(float2[],bool)}</summary>
		[BurstDiscard]
		public void Polyline (float2[] points, Color color) {
			Polyline(points, false, color);
		}
		/// <summary>\copydocref{Polyline(NativeArray<float2>,bool)}</summary>
		public void Polyline (NativeArray<float2> points, bool cycle, Color color) {
			draw.PushColor(color);
			for (int i = 0; i < points.Length - 1; i++) {
				Line(points[i], points[i+1]);
			}
			if (cycle && points.Length > 1) Line(points[points.Length - 1], points[0]);
			draw.PopColor();
		}
		/// <summary>\copydocref{Polyline(NativeArray<float2>,bool)}</summary>
		public void Polyline (NativeArray<float2> points, Color color) {
			Polyline(points, false, color);
		}
		/// <summary>\copydocref{Cross(float2,float)}</summary>
		public void Cross (float2 position, float size, Color color) {
			draw.PushColor(color);
			size *= 0.5f;
			Line(position - new float2(size, 0), position + new float2(size, 0));
			Line(position - new float2(0, size), position + new float2(0, size));
			draw.PopColor();
		}
		/// <summary>\copydocref{Cross(float2,float)}</summary>
		public void Cross (float2 position, Color color) {
			Cross(position, 1, color);
		}
		/// <summary>\copydocref{WireRectangle(float3,float2)}</summary>
		public void WireRectangle (float3 center, float2 size, Color color) {
			draw.WirePlane(center, xy ? XY_TO_XZ_ROTATION : XZ_TO_XZ_ROTATION, size, color);
		}
		/// <summary>\copydocref{WireRectangle(Rect)}</summary>
		public void WireRectangle (Rect rect, Color color) {
			draw.PushColor(color);
			float2 min = rect.min;
			float2 max = rect.max;

			Line(new float2(min.x, min.y), new float2(max.x, min.y));
			Line(new float2(max.x, min.y), new float2(max.x, max.y));
			Line(new float2(max.x, max.y), new float2(min.x, max.y));
			Line(new float2(min.x, max.y), new float2(min.x, min.y));
			draw.PopColor();
		}

		/// <summary>\copydocref{WireGrid(float2,int2,float2)}</summary>
		/// <param name="color">Color of the object</param>
		public void WireGrid (float2 center, int2 cells, float2 totalSize, Color color) {
			draw.WireGrid(xy ? new float3(center, 0) : new float3(center.x, 0, center.y), xy ? XY_TO_XZ_ROTATION : XZ_TO_XZ_ROTATION, cells, totalSize, color);
		}
		/// <summary>\copydocref{WireGrid(float3,int2,float2)}</summary>
		/// <param name="color">Color of the object</param>
		public void WireGrid (float3 center, int2 cells, float2 totalSize, Color color) {
			draw.WireGrid(center, xy ? XY_TO_XZ_ROTATION : XZ_TO_XZ_ROTATION, cells, totalSize, color);
		}
	}
}
