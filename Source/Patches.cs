using HarmonyLib;
using Verse;
using System;
using System.Diagnostics;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
using static Reunity.Setup;
using System.Collections.Generic;
 
namespace Reunity
{
	[StaticConstructorOnStartup]
	public static class Setup
	{
		/* 
		matrix.m00 = (1.0f - 2.0f * (q.y * q.y + q.z * q.z)) * s.x;
		matrix.m10 = (q.x * q.y + q.z * q.w) * s.x * 2.0f;
		matrix.m20 = (q.x * q.z - q.y * q.w) * s.x * 2.0f;
		matrix.m30 = 0.0f;
		
		matrix.m01 = (q.x * q.y - q.z * q.w) * s.y * 2.0f;
		matrix.m11 = (1.0f - 2.0f * (q.x * q.x + q.z * q.z)) * s.y;
		matrix.m21 = (q.y * q.z + q.x * q.w) * s.y * 2.0f;
		matrix.m31 = 0.0f;
		
		matrix.m02 = (q.x * q.z + q.y * q.w) * s.z * 2.0f;
		matrix.m12 = (q.y * q.z - q.x * q.w) * s.z * 2.0f;
		matrix.m22 = (1.0f - 2.0f * (q.x * q.x + q.y * q.y)) * s.z;
		matrix.m32 = 0.0f;
		
		matrix.m03 = pos.x;
		matrix.m13 = pos.y;
		matrix.m23 = pos.z;
		matrix.m33 = 1.0f;
		*/

		public static Matrix4x4 matrix = new Matrix4x4() { m10 = 0f, m30 = 0f, m01 = 0f, m11 = 1f, m21 = 0f, m31 = 0f, m12 = 0f, m32 = 0f, m33 = 1f } ;
		public static Vector3 vectorOne = Vector3.one;
        static Setup()
        {
            new Harmony("owlchemist.reunity").PatchAll();
        }
	}

	//Patch a few odd stray methods in rimworld that actually use Z rotation to use the z-supporting TRS method
	[HarmonyPatch]
    static class Replace_TRSWithZ
    {
		static IEnumerable<System.Reflection.MethodBase> TargetMethods()
		{
			//Often used by the bracket selector
			yield return AccessTools.Method(typeof(GUIUtility), nameof(GUIUtility.RotateAroundPivot));
			//Such as the reseach tree
			yield return AccessTools.Method(typeof(Widgets), nameof(Widgets.DrawLine));
		}
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			return instructions.MethodReplacer(AccessTools.Method(typeof(Matrix4x4), nameof(Matrix4x4.TRS)),
				AccessTools.Method(typeof(Replace_TRSWithZ), nameof(Replace_TRSWithZ.TRS)));
		}
		
		static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s)
		{			
			return new Matrix4x4{
				m00 = (1.0f - 2.0f * (q.y * q.y + q.z * q.z)) * s.x,
				m10 = (q.z * q.w) *s.x * 2.0f,
				m20 = (0f - q.y * q.w) * s.x * 2.0f,
				
				m01 = (0f - q.z * q.w) *s.y * 2.0f,
				m11 = (1.0f - 2.0f * (q.z * q.z)) * s.y,
				m21 = (q.y * q.z) * s.y * 2.0f,
				
				m02 = (q.y * q.w) * s.z * 2.0f,
				m12 = (q.y * q.z) * s.z * 2.0f,
				m22 = (1.0f - 2.0f * (q.y * q.y)) * s.z,
				
				m03 = pos.x,
				m13 = pos.y,
				m23 = pos.z,
				m33 = 1.0f
			};
		}
	}
	
	[HarmonyPatch(typeof(Matrix4x4), nameof(Matrix4x4.TRS))]
    static class Replace_TRS
    {	
		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var original = AccessTools.Method(typeof(Replace_TRS), nameof(Replace_TRS.TRS));
			var generator = PatchProcessor.CreateILGenerator(original);
			var codes = MethodCopier.GetInstructions(generator, original, 0);
			return codes;
		}
		
		static Matrix4x4 TRS(Vector3 pos, Quaternion q, Vector3 s)
		{
			//X and Z rotation are always 0
			matrix.m00 = (1.0f -2.0f * (q.y *q.y)) *s.x;
			matrix.m20 = (0f - q.y * q.w) *s.x * 2.0f;

			matrix.m02 = q.y * q.w * s.z * 2f;
			matrix.m22 = (1.0f - 2.0f * (q.y * q.y)) *s.z;
			
			matrix.m03 = pos.x;
			matrix.m13 = pos.y;
			matrix.m23 = pos.z;
			
			return matrix;
		}
		
		/*
		static bool Prefix(ref Matrix4x4 __result, Vector3 pos, Quaternion q, Vector3 s)
		{
			if (q.x != 0f) Log.Message("O_____________O");
			if (q.z != 0f) Log.Message("X_____________X");
			matrix.m00 = (1.0f -2.0f * (q.y *q.y)) *s.x;
			matrix.m20 = (0f - q.y * q.w) *s.x * 2.0f;

			matrix.m02 = q.y * q.w * s.z * 2f;
			matrix.m22 = (1.0f - 2.0f * (q.y * q.y)) *s.z;
			
			matrix.m03 = pos.x;
			matrix.m13 = pos.y;
			matrix.m23 = pos.z;
			
			__result = matrix;
			return false;
		}
		/*
		
		static void Postfix(Matrix4x4 __result, Quaternion q, Vector3 s, Matrix4x4 __state)
		{
			if (!__result.Equals(__state))
			{
				Log.Message(
					"=========BEFORE=======\n" +
					__state.m00.ToString() + " " +
					__state.m10.ToString() + " " +
					__state.m20.ToString() + " " +
					__state.m30.ToString() + " " +

					__state.m01.ToString() + " " +
					__state.m11.ToString() + " " +
					__state.m21.ToString() + " " +
					__state.m31.ToString() + " " +

					__state.m02.ToString() + " " +
					__state.m12.ToString() + " " +
					__state.m22.ToString() + " " +
					__state.m32.ToString() + " " +

					__state.m03.ToString() + " " +
					__state.m13.ToString() + " " +
					__state.m23.ToString() + " " +
					__state.m33.ToString() +

					"\n=========AFTER=======\n" +
					__result.m00.ToString() + " " +
					__result.m10.ToString() + " " +
					__result.m20.ToString() + " " +
					__result.m30.ToString() + " " +

					__result.m01.ToString() + " " +
					__result.m11.ToString() + " " +
					__result.m21.ToString() + " " +
					__result.m31.ToString() + " " +

					__result.m02.ToString() + " " +
					__result.m12.ToString() + " " +
					__result.m22.ToString() + " " +
					__result.m32.ToString() + " " +

					__result.m03.ToString() + " " +
					__result.m13.ToString() + " " +
					__result.m23.ToString() + " " +
					__result.m33.ToString() +
					"\n========VALUES-Q=======\n" +
					q.w.ToString() + " " + q.x.ToString() + " " + q.y.ToString() + " " + q.z.ToString() +
					"\n========VALUES-S=======\n" +
					s.x.ToString() + " " + s.y.ToString() + " " + s.z.ToString() +
					"\n====RECOMPUTING=====\n" +
					((1.0f - 2.0f * (q.y * q.y + q.z * q.z)) *s.x).ToString() +
					"\n((1.0f - 2.0f * ( " + q.y.ToString() + " * " + q.y.ToString() + " + " + q.z.ToString() + " * " + q.z.ToString() + " )) * " + s.x.ToString() + " )"
				);
			}
		}
		*/
	}

	[HarmonyPatch(typeof(Graphics), nameof(Graphics.DrawMesh), new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int) } )]
	[HarmonyPriority(Priority.Last)]
    static class Replace_DrawMesh
    {	
		/*
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var original = AccessTools.Method(typeof(Replace_DrawMesh), nameof(Replace_DrawMesh.DrawMesh));
			var generator = PatchProcessor.CreateILGenerator(original);
			var codes = MethodCopier.GetInstructions(generator, original, 0);
		
			var label = new Label();
			foreach (var code in codes)
			{
				if (code.opcode == OpCodes.Brtrue)
				{
					yield return new CodeInstruction(OpCodes.Brtrue, label);
					continue;
				}
				else if (code.opcode == OpCodes.Ldarga_S && code.OperandIs(1))  
				{
					code.WithLabels(label);
				}
				yield return code;
			}
		}
		*/

		static bool Prefix(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer)
		{
			if (layer == 0)
			{
				//X and Z rotation are always 0
				matrix.m00 = (1f - 2f * rotation.y * rotation.y);
				matrix.m20 = (0f - rotation.y * rotation.w) * 2.0f;
				
				matrix.m02 = rotation.y * rotation.w * 2f;
				matrix.m22 = (1f - 2f * rotation.y * rotation.y);
				
				matrix.m03 = position.x;
				matrix.m13 = position.y;
				matrix.m23 = position.z;

				Graphics.Internal_DrawMesh_Injected(
					mesh, //Mesh
					0, //Submesh
					ref matrix, //Matrix
					material, //Material
					layer, //Layer
					null, //Camera
					null, //MaterialPropertyBlock
					ShadowCastingMode.On, //ShadowCastingMode
					true, //ReceiveShadows
					null, //ProbeAnchor
					LightProbeUsage.BlendProbes, //LightProbeUsage
					null //LightProbeProxyVolume
				);
				return false;
			}
			
			Matrix4x4 worldMatrix;
			Matrix4x4.TRS_Injected(ref position, ref rotation, ref vectorOne, out worldMatrix);

			Graphics.Internal_DrawMesh_Injected(
				mesh, //Mesh
				0, //Submesh
				ref worldMatrix, //Matrix
				material, //Material
				layer, //Layer
				null, //Camera
				null, //MaterialPropertyBlock
				ShadowCastingMode.On, //ShadowCastingMode
				true, //ReceiveShadows
				null, //ProbeAnchor
				LightProbeUsage.BlendProbes, //LightProbeUsage
				null //LightProbeProxyVolume
			);

			return false;
		}
    }

	//Draws where the matrix is computed within Rimworld assembly
	[HarmonyPatch(typeof(Graphics), nameof(Graphics.DrawMesh), new System.Type[] {typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int)})]
    static class Replace_DrawMeshWithMatrix
    {
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var original = AccessTools.Method(typeof(Replace_DrawMeshWithMatrix), nameof(Replace_DrawMeshWithMatrix.DrawMesh));
			var generator = PatchProcessor.CreateILGenerator(original);
			var codes = MethodCopier.GetInstructions(generator, original, 0);
			return codes;
		}
		
		static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer)
		{
			Graphics.Internal_DrawMesh_Injected(
				mesh, //Mesh
				0, //Submesh
				ref matrix, //Matrix
				material, //Material
				layer, //Layer
				null, //Camera
				null, //MaterialPropertyBlock
				ShadowCastingMode.On, //ShadowCastingMode
				true, //ReceiveShadows
				null, //ProbeAnchor
				LightProbeUsage.BlendProbes, //LightProbeUsage
				null //LightProbeProxyVolume
			);
		}
	}

	//Mainly just the planet renderer here
	[HarmonyPatch(typeof(Graphics), nameof(Graphics.DrawMesh), new System.Type[] 
	{typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(int), typeof(Camera), typeof(int), typeof(MaterialPropertyBlock)})]
    static class Replace_DrawPlanetMesh
    {		
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{

			var original = AccessTools.Method(typeof(Replace_DrawPlanetMesh), nameof(Replace_DrawPlanetMesh.DrawMesh));
			var generator = PatchProcessor.CreateILGenerator(original);
			var codes = MethodCopier.GetInstructions(generator, original, 0);
			return codes;
		}
		static Matrix4x4 worldMatrix;
		static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties)
		{
			Matrix4x4.TRS_Injected(ref position, ref rotation, ref vectorOne, out worldMatrix);
			Graphics.Internal_DrawMesh_Injected(
				mesh, //Mesh
				submeshIndex, //Submesh
				ref worldMatrix, //Matrix
				material, //Material
				layer, //Layer
				camera, //Camera
				properties, //MaterialPropertyBlock
				ShadowCastingMode.On, //ShadowCastingMode
				true, //ReceiveShadows
				null, //ProbeAnchor
				LightProbeUsage.BlendProbes, //LightProbeUsage
				null //LightProbeProxyVolume
			);
		}
	}
}