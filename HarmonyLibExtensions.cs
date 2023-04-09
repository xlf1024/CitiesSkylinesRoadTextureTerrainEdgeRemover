using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using UnityEngine;

// stuff from newer versions of harmony that I felt like using
// sometimes adapted to make it work with current harmony

namespace RoadTextureTerrainEdgeRemover
{

    /// <summary>Extensions for <see cref="CodeInstruction"/></summary>
    ///
    public static class CodeInstructionExtensions
    {
        /// <summary>Returns the index targeted by this <c>ldloc</c>, <c>ldloca</c>, or <c>stloc</c></summary>
        /// <param name="code">The <see cref="CodeInstruction"/></param>
        /// <returns>The index it targets</returns>
        /// <seealso cref="CodeInstruction.LoadLocal(int, bool)"/>
        /// <seealso cref="CodeInstruction.StoreLocal(int)"/>
        public static int LocalIndex(this CodeInstruction code)
        {
            //#if DEBUG
            //            Debug.Log("ROTTERdam: Harmony extension: CodeInstruction.LocalIndex");
            //            Debug.Log(code.opcode);
            //            Debug.Log(code.operand);
            //            Debug.Log(code.operand?.GetType());
            //#endif
            if (code.operand?.GetType() == typeof(LocalBuilder)) return (code.operand as LocalBuilder).LocalIndex;
            else if (code.opcode == OpCodes.Ldloc_0 || code.opcode == OpCodes.Stloc_0) return 0;
            else if (code.opcode == OpCodes.Ldloc_1 || code.opcode == OpCodes.Stloc_1) return 1;
            else if (code.opcode == OpCodes.Ldloc_2 || code.opcode == OpCodes.Stloc_2) return 2;
            else if (code.opcode == OpCodes.Ldloc_3 || code.opcode == OpCodes.Stloc_3) return 3;
            else if (code.opcode == OpCodes.Ldloc_S || code.opcode == OpCodes.Ldloc) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Stloc_S || code.opcode == OpCodes.Stloc) return Convert.ToInt32(code.operand);
            else if (code.opcode == OpCodes.Ldloca_S || code.opcode == OpCodes.Ldloca) return Convert.ToInt32(code.operand);
            else throw new ArgumentException("Instruction is not a load or store", "code");
        }

        /// <summary>Creates a CodeInstruction loading a local with the given index, using the shorter forms when possible</summary>
        /// <param name="index">The index where the local is stored</param>
        /// <param name="useAddress">Use address of local</param>
        /// <returns></returns>
        /// <seealso cref="CodeInstructionExtensions.LocalIndex(CodeInstruction)"/>
        public static CodeInstruction LoadLocal(int index, bool useAddress = false)
        {
            if (useAddress)
            {
                if (index < 256) return new CodeInstruction(OpCodes.Ldloca_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldloca, index);
            }
            else
            {
                if (index == 0) return new CodeInstruction(OpCodes.Ldloc_0);
                else if (index == 1) return new CodeInstruction(OpCodes.Ldloc_1);
                else if (index == 2) return new CodeInstruction(OpCodes.Ldloc_2);
                else if (index == 3) return new CodeInstruction(OpCodes.Ldloc_3);
                else if (index < 256) return new CodeInstruction(OpCodes.Ldloc_S, Convert.ToByte(index));
                else return new CodeInstruction(OpCodes.Ldloc, index);
            }
        }

        /// <summary>Creates a CodeInstruction storing to a local with the given index, using the shorter forms when possible</summary>
        /// <param name="index">The index where the local is stored</param>
        /// <returns></returns>
        /// <seealso cref="CodeInstructionExtensions.LocalIndex(CodeInstruction)"/>
        public static CodeInstruction StoreLocal(int index)
        {
            if (index == 0) return new CodeInstruction(OpCodes.Stloc_0);
            else if (index == 1) return new CodeInstruction(OpCodes.Stloc_1);
            else if (index == 2) return new CodeInstruction(OpCodes.Stloc_2);
            else if (index == 3) return new CodeInstruction(OpCodes.Stloc_3);
            else if (index < 256) return new CodeInstruction(OpCodes.Stloc_S, Convert.ToByte(index));
            else return new CodeInstruction(OpCodes.Stloc, index);
        }

        /// <summary>Creates a CodeInstruction loading a constant with the given value, using the shorter forms when possible</summary>
        /// <param name="value">The value of the constant
        /// <returns></returns>
        /// <seealso cref="CodeInstructionExtensions.LocalIndex(CodeInstruction)"/>
        public static CodeInstruction LoadConstant(int value)
        {
            if (value == -1) return new CodeInstruction(OpCodes.Ldc_I4_M1);
            else if (value == 0) return new CodeInstruction(OpCodes.Ldc_I4_0);
            else if (value == 1) return new CodeInstruction(OpCodes.Ldc_I4_1);
            else if (value == 2) return new CodeInstruction(OpCodes.Ldc_I4_2);
            else if (value == 3) return new CodeInstruction(OpCodes.Ldc_I4_3);
            else if (value == 4) return new CodeInstruction(OpCodes.Ldc_I4_4);
            else if (value == 5) return new CodeInstruction(OpCodes.Ldc_I4_5);
            else if (value == 6) return new CodeInstruction(OpCodes.Ldc_I4_6);
            else if (value == 7) return new CodeInstruction(OpCodes.Ldc_I4_7);
            else if (value == 8) return new CodeInstruction(OpCodes.Ldc_I4_8);
            else if (-128 <= value && value < 128) return new CodeInstruction(OpCodes.Ldc_I4_S, Convert.ToSByte(value));
            else return new CodeInstruction(OpCodes.Ldc_I4, value);
        }
    }

    public static class HarmonyExtensions
    {
        /// <summary>Searches an assembly for Harmony annotations with a specific category and uses them to create patches</summary>
		/// <param name="assembly">The assembly</param>
		/// <param name="category">Name of patch category</param>
		/// 
		public static void PatchWithAnnotation(this Harmony self, Assembly assembly, Type annotationType)
        {
            PatchClassProcessor[] patchClasses = AccessTools.GetTypesFromAssembly(assembly).Where(type => type.GetCustomAttributes(true).Any(annotation => annotation.GetType() == annotationType)).Select(self.CreateClassProcessor).ToArray();
            patchClasses.Do(patchClass => patchClass.Patch());
        }
    }
}
