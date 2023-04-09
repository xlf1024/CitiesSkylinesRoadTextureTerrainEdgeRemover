using ColossalFramework;
using HarmonyLib;
using static HarmonyLib.CodeInstructionExtensions;
using static RoadTextureTerrainEdgeRemover.CodeInstructionExtensions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using System.Reflection;

namespace RoadTextureTerrainEdgeRemover
{
    /*
     * Summary of what this does:
     * the blue and alpha m_surfaceMapA contain normal map information. Each pixel corresponds to a quad face of the terrain mesh.
     * In vanilla, the normals are computed as the average of the slopes of the edges (#) of that quad (X) by looking at the corners.
     * For each of those edges, this patch looks at the edges before and after it (*) and limits the calculated slope
     * to lie in between the slopes of the extensions or closer to horizontal.
     * 
     * Sketch:
     * +->x
     * |           ( 0|-1)     ( 1|-1)
     * V              |           |
     * y              *           *
     *                |           |
     * (-1| 0)--*--( 0| 0)--#--( 1| 0)--*--( 2| 0)
     *                |           |
     *                #     X     #
     *                |           |
     * (-1| 1)--*--( 0| 1)--#--( 1| 1)--*--( 2| 1)
     *                |           |
     *                *           *
     *                |           |
     *             ( 0| 2)     ( 1| 2)
     * 
     * TODO: same patch, but for the undetailed version
     */
    [EdgeFilterPatch]
    [HarmonyPatch]
    [HarmonyPatch(typeof(TerrainPatch), "Refresh")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051", Justification = "Called by harmony")]
    class TerrainPatchRefreshPatchEdgeFilter
    {
#if DEBUG
        [HarmonyDebug]
#endif
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> DetailTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
#if DEBUG
            Debug.Log("ROTTERdam: Applying TerrainPatch::Refresh transpiler for edge detection");
#endif
            var codes = new List<CodeInstruction>(instructions);
            var xCandidates = new HashSet<int>();
            var zCandidates = new HashSet<int>();
            var xLI = new Dictionary<int, int>();
            var zLI = new Dictionary<int, int>();
            var fhLI = new Dictionary<Tuple<int, int>, int>();
            xLI.Add(-1, generator.DeclareLocal(typeof(int)).LocalIndex);
            xLI.Add(2, generator.DeclareLocal(typeof(int)).LocalIndex);
            zLI.Add(-1, generator.DeclareLocal(typeof(int)).LocalIndex);
            zLI.Add(2, generator.DeclareLocal(typeof(int)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(0, -1), generator.DeclareLocal(typeof(float)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(1, -1), generator.DeclareLocal(typeof(float)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(-1, 0), generator.DeclareLocal(typeof(float)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(2, 0), generator.DeclareLocal(typeof(float)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(-1, 1), generator.DeclareLocal(typeof(float)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(2, 1), generator.DeclareLocal(typeof(float)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(0, 2), generator.DeclareLocal(typeof(float)).LocalIndex);
            fhLI.Add(new Tuple<int, int>(1, 2), generator.DeclareLocal(typeof(float)).LocalIndex);
            MethodInfo getDetailHeight = typeof(TerrainManager).GetMethod("GetDetailHeight");
            MethodInfo mathfMinInt = typeof(Mathf).GetMethod("Min", new Type[] { typeof(int), typeof(int) });
            MethodInfo mathfMaxInt = typeof(Mathf).GetMethod("Max", new Type[] { typeof(int), typeof(int) });
            ConstructorInfo vectorConstructor = typeof(Vector3).GetConstructor(new Type[] { typeof(float), typeof(float), typeof(float) });
            // find local fields
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: find local fields");
#endif
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(getDetailHeight))
                {
                    if (codes[i - 2].IsLdloc())
                    {
                        xCandidates.Add(codes[i - 2].LocalIndex());
                    }
                    if (codes[i - 1].IsLdloc())
                    {
                        zCandidates.Add(codes[i - 1].LocalIndex());
                    }
                }
            }
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: local variable index status:");
            Debug.Log(xCandidates.Join(null, ","));
            Debug.Log(zCandidates.Join(null, ","));
#endif
            // distinguish min and max
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: distinguish min and max");
#endif
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].IsStloc())
                {
                    int localIndex = codes[i].LocalIndex();
                    if (xCandidates.Contains(localIndex))
                    {
                        if (codes[i - 1].Calls(mathfMaxInt))
                        {
                            xLI.Add(0, localIndex);
                            xCandidates.Remove(localIndex);
                        }
                        else if (codes[i - 1].Calls(mathfMinInt))
                        {
                            xLI.Add(1, localIndex);
                            xCandidates.Remove(localIndex);
                        }
                    }
                    if (zCandidates.Contains(localIndex))
                    {
                        if (codes[i - 1].Calls(mathfMaxInt))
                        {
                            zLI.Add(0, localIndex);
                            zCandidates.Remove(localIndex);
                        }
                        else if (codes[i - 1].Calls(mathfMinInt))
                        {
                            zLI.Add(1, localIndex);
                            zCandidates.Remove(localIndex);
                        }
                    }
                }
            }
            // find local fields for heights
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: find local fields for heights");
#endif
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(getDetailHeight) && codes[i - 2].IsLdloc() && codes[i - 1].IsLdloc() && codes[i + 1].IsStloc())
                {
                    int foundXLI = codes[i - 2].LocalIndex();
                    int foundZLI = codes[i - 1].LocalIndex();
                    if (xLI.ContainsValue(foundXLI) && zLI.ContainsValue(foundZLI))
                    {
                        int x = 42;
                        int z = 42;
                        foreach (var coord in xLI.Keys)
                        {
                            if (xLI[coord] == foundXLI) x = coord;
                        }
                        foreach (var coord in zLI.Keys)
                        {
                            if (zLI[coord] == foundZLI) z = coord;
                        }
                        fhLI.Add(new Tuple<int, int>(x, z), codes[i + 1].LocalIndex());
                    }
                }
            }
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: local variable index status:");
            Debug.Log(xLI.Join());
            Debug.Log(zLI.Join());
            Debug.Log(fhLI.Join());
#endif
            // set the new coordinate variables
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: set the new coordinate variables");
#endif
            var coordDependencyMap = new Dictionary<int, IEnumerable<DependentClampedLocalValueDescription>>{
                {xLI[0], new[]{new DependentClampedLocalValueDescription{ targetLI = xLI[-1], offset= -1, limit= 0 } ,
                    new DependentClampedLocalValueDescription{ targetLI = xLI[1], offset= 1, limit= 4320 } } },
                {xLI[1], new[]{new DependentClampedLocalValueDescription{ targetLI = xLI[2], offset= 1, limit= 4320 } } },
                {zLI[0], new[]{new DependentClampedLocalValueDescription{ targetLI = zLI[-1], offset= -1, limit= 0 } } },
                {zLI[1], new[]{new DependentClampedLocalValueDescription{ targetLI = zLI[2], offset= 1, limit= 4320 } } }
            };
            DependentClampedLocalValue(codes, coordDependencyMap);

            // modify the height reads
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: modify the height reads");
#endif
            var heightReadCoordMap = new Dictionary<Tuple<int, int>, IEnumerable<Tuple<int, int>>> {
                {new Tuple<int,int>(0,0),new Tuple<int,int>[]{ //outer loop
                    new Tuple<int,int>(0,-1),
                    new Tuple<int,int>(-1,0),
                    new Tuple<int,int>(0,0),
                    new Tuple<int,int>(1,0)
                } },
                {new Tuple<int,int>(0,1),new Tuple<int,int>[]{ //outer loop
                    new Tuple<int,int>(-1,1),
                    new Tuple<int,int>(0,1),
                    new Tuple<int,int>(1,1),
                    new Tuple<int,int>(0,2)
                } },
                {new Tuple<int,int>(1,0),new Tuple<int,int>[]{ //inner loop
                    new Tuple<int,int>(1,-1),
                    new Tuple<int,int>(2,0)
                } },
                {new Tuple<int,int>(1,1),new Tuple<int,int>[]{ //inner loop
                    new Tuple<int,int>(2,1),
                    new Tuple<int,int>(1,2)
                } }
            };
            ReplaceInstanceCallCoords(codes, heightReadCoordMap, getDetailHeight, xLI, zLI, fhLI);


            // modify the loop copies
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: modify the loop copies");
#endif
            var copyLocalFieldMap = new Dictionary<Tuple<int, int>, IEnumerable<Tuple<int, int>>>
            {
                {new Tuple<int,int>(fhLI[new Tuple<int,int>(1,0)], fhLI[new Tuple<int,int>(0,0)]), new Tuple<int, int>[]{
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(1,-1)], fhLI[new Tuple<int,int>(0,-1)]),
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(0,0)], fhLI[new Tuple<int,int>(-1,0)]),
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(1,0)], fhLI[new Tuple<int,int>(0,0)]),
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(2,0)], fhLI[new Tuple<int,int>(1,0)])
                }},
                {new Tuple<int,int>(fhLI[new Tuple<int,int>(1,1)], fhLI[new Tuple<int,int>(0,1)]), new Tuple<int, int>[]{
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(0,1)], fhLI[new Tuple<int,int>(-1,1)]),
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(1,1)], fhLI[new Tuple<int,int>(0,1)]),
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(2,1)], fhLI[new Tuple<int,int>(1,1)]),
                    new Tuple<int,int>(fhLI[new Tuple<int,int>(1,2)], fhLI[new Tuple<int,int>(0,2)])
                }}
            };
            ReplaceCopies(codes, copyLocalFieldMap);


            // replace the normal vector calculation
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: replace the normal vector calculation");
#endif
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && Equals(codes[i].operand, vectorConstructor))
                {
                    if (codes[i - 1].opcode == OpCodes.Mul && codes[i - 2].IsLdloc())
                    {
                        var normalScaleLI = codes[i - 2].LocalIndex();
                        if (
                            (codes[i - 3].opcode == OpCodes.Add || codes[i - 3].opcode == OpCodes.Sub)
                            && codes[i - 4].IsLdloc() && fhLI.ContainsValue(codes[i - 4].LocalIndex())
                            && (codes[i - 5].opcode == OpCodes.Add || codes[i - 5].opcode == OpCodes.Sub)
                            && codes[i - 6].IsLdloc() && fhLI.ContainsValue(codes[i - 6].LocalIndex())
                            && (codes[i - 7].opcode == OpCodes.Add || codes[i - 7].opcode == OpCodes.Sub)
                            && codes[i - 8].IsLdloc() && fhLI.ContainsValue(codes[i - 8].LocalIndex())
                            && codes[i - 9].IsLdloc() && fhLI.ContainsValue(codes[i - 9].LocalIndex())
                            && codes[i - 10].LoadsConstant()
                            )
                        {
                            var yConstantLoad = codes[i - 10].Clone();
                            if (
                            codes[i - 11].opcode == OpCodes.Mul
                            && codes[i - 12].IsLdloc() && normalScaleLI == codes[i - 12].LocalIndex()
                            && (codes[i - 13].opcode == OpCodes.Add || codes[i - 13].opcode == OpCodes.Sub)
                            && codes[i - 14].IsLdloc() && fhLI.ContainsValue(codes[i - 14].LocalIndex())
                            && (codes[i - 15].opcode == OpCodes.Add || codes[i - 15].opcode == OpCodes.Sub)
                            && codes[i - 16].IsLdloc() && fhLI.ContainsValue(codes[i - 16].LocalIndex())
                            && (codes[i - 17].opcode == OpCodes.Add || codes[i - 17].opcode == OpCodes.Sub)
                            && codes[i - 18].IsLdloc() && fhLI.ContainsValue(codes[i - 18].LocalIndex())
                            && codes[i - 19].IsLdloc() && fhLI.ContainsValue(codes[i - 19].LocalIndex())
                            )
                            {
                                i -= 19;
                                var labels = RemoveRangeExtractLabels(codes, i, 19);
                                var insertInstructions = new CodeInstruction[]
                                {
                                    // x-direction; z=0
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(-1,0)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(0,0)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(1,0)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(2,0)]),
                                    CodeInstruction.Call(typeof(TerrainPatchRefreshPatchEdgeFilter), "ClampSlope"),
                                    // x-direction; z=1
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(-1,1)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(0,1)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(1,1)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(2,1)]),
                                    CodeInstruction.Call(typeof(TerrainPatchRefreshPatchEdgeFilter), "ClampSlope"),
                                    // x-direction; combine and scale
                                    new CodeInstruction(OpCodes.Add),
                                    CodeInstructionExtensions.LoadLocal(normalScaleLI),
                                    new CodeInstruction(OpCodes.Mul),
                                    // y component
                                    yConstantLoad.Clone(),
                                    // z-direction; x=0
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(0,-1)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(0,0)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(0,1)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(0,2)]),
                                    CodeInstruction.Call(typeof(TerrainPatchRefreshPatchEdgeFilter), "ClampSlope"),
                                    // z-direction; x=1
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(1,-1)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(1,0)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(1,1)]),
                                    CodeInstructionExtensions.LoadLocal(fhLI[new Tuple<int, int>(1,2)]),
                                    CodeInstruction.Call(typeof(TerrainPatchRefreshPatchEdgeFilter), "ClampSlope"),
                                    // z-direction; combine and scale
                                    new CodeInstruction(OpCodes.Add),
                                    CodeInstructionExtensions.LoadLocal(normalScaleLI),
                                    new CodeInstruction(OpCodes.Mul)
                                };
                                codes.InsertRange(i, insertInstructions);
                                codes[i] = codes[i].WithLabels(labels);
                                labels.Clear();
                                i += insertInstructions.Length;
                            }
                        }
                    }
                }
            }

            return codes;
        }
        static List<Label> RemoveRangeExtractLabels(List<CodeInstruction> codes, int start, int count)
        {
            var labels = new List<Label>();
            for (int i = start; i < start + count; i++)
            {
                labels.AddRange(codes[i].ExtractLabels());
            }
            codes.RemoveRange(start, count);
            return labels;
        }
        struct DependentClampedLocalValueDescription
        {
            public int targetLI;
            public int offset;
            public int limit;
        }
        static void DependentClampedLocalValue(List<CodeInstruction> codes, IDictionary<int, IEnumerable<DependentClampedLocalValueDescription>> dependencies)
        {
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].IsStloc())
                {
                    var sourceLI = codes[i].LocalIndex();
                    if (dependencies.ContainsKey(sourceLI))
                    {
                        foreach (var dependent in dependencies[sourceLI])
                        {
                            codes.InsertRange(i + 1, new CodeInstruction[] {
                                CodeInstructionExtensions.LoadLocal(sourceLI),
                                CodeInstructionExtensions.LoadConstant(dependent.offset),
                                new CodeInstruction(OpCodes.Add),
                                CodeInstructionExtensions.LoadConstant(dependent.limit),
                                CodeInstruction.Call(typeof(Mathf), dependent.offset > 0 ? "Min" : "Max", new Type[]{typeof(int), typeof(int) }),
                                CodeInstructionExtensions.StoreLocal(dependent.targetLI)
                            });
                            i += 6;
                        }
                    }
                }
            }
        }
        static void ReplaceInstanceCallCoords(List<CodeInstruction> codes, IDictionary<Tuple<int, int>, IEnumerable<Tuple<int, int>>> replacements, MethodInfo calledMethod, Dictionary<int, int> xLI, Dictionary<int, int> zLI, Dictionary<Tuple<int, int>, int> targetsLI)
        {
            for (int i = 0; i < codes.Count - 4; i++)
            {
                if (codes[i + 3].Calls(calledMethod))
                {
                    var instanceLoadInstruction = codes[i].Clone();
                    var callInstruction = codes[i + 3].Clone();
                    var foundTargetLI = codes[i + 4].LocalIndex();
                    foreach (var foundTargetCoords in replacements.Keys)
                    {
                        if (foundTargetLI == targetsLI[foundTargetCoords])
                        {
                            if (codes[i + 1].IsLdloc() && codes[i + 1].LocalIndex() == xLI[foundTargetCoords.First]
                                && codes[i + 2].IsLdloc() && codes[i + 2].LocalIndex() == zLI[foundTargetCoords.Second])
                            {
#if DEBUG
                                Debug.Log("found call with coords " + foundTargetCoords);
#endif
                                var labels = RemoveRangeExtractLabels(codes, i, 5);
                                foreach (var replacementCoords in replacements[foundTargetCoords])
                                {
                                    codes.InsertRange(i, new CodeInstruction[] {
                                        instanceLoadInstruction.Clone(),
                                        CodeInstructionExtensions.LoadLocal(xLI[replacementCoords.First]),
                                        CodeInstructionExtensions.LoadLocal(zLI[replacementCoords.Second]),
                                        callInstruction.Clone(),
                                        CodeInstructionExtensions.StoreLocal(targetsLI[replacementCoords])
                                    });
                                    codes[i] = codes[i].WithLabels(labels);
                                    labels.Clear();
                                    i += 5; // move to after the insertion
                                }
                                i--; // compensate for loop increment
                                break;
                            }
                        }
                    }
                }
            }
        }
        static void ReplaceCopies(List<CodeInstruction> codes, IDictionary<Tuple<int, int>, IEnumerable<Tuple<int, int>>> replacements)
        {
#if DEBUG
            Debug.Log("ROTTERdam: EdgeFilter patch: ReplaceCopies");
            Debug.Log(replacements.Join());
#endif
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].IsLdloc() && codes[i + 1].IsStloc())
                {
                    var foundSource = codes[i].LocalIndex();
                    var foundTarget = codes[i + 1].LocalIndex();
                    var foundPair = new Tuple<int, int>(foundSource, foundTarget);
                    if (replacements.ContainsKey(foundPair))
                    {
                        var labels = RemoveRangeExtractLabels(codes, i, 2);
                        foreach (var replacementPair in replacements[foundPair])
                        {
                            codes.InsertRange(i, new CodeInstruction[]
                            {
                                CodeInstructionExtensions.LoadLocal(replacementPair.First),
                                CodeInstructionExtensions.StoreLocal(replacementPair.Second)
                            });
                            codes[i] = codes[i].WithLabels(labels);
                            labels.Clear();
                            i += 2; // move to after the insertion
                        }
                        i--; // compensate for loop increment
                    }
                }
            }
        }
        public static float ClampSlope(float hm1, float h0, float h1, float h2)
        {
            float sm10 = hm1 - h0;
            float s01 = h0 - h1;
            float s12 = h1 - h2;
            float[] boundlist = new float[] { sm10, s12, 0.0f };
            float upperBound = Mathf.Max(boundlist) + Settings.EdgeFilterRange.value;
            float lowerBound = Mathf.Min(boundlist) - Settings.EdgeFilterRange.value;
            return Mathf.Clamp(s01, lowerBound, upperBound);
        }
    }
}