using UnityEngine;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Burst.Intrinsics.X86.Ssse3;
using static Unity.Burst.Intrinsics.X86.Sse4_1;
using static Unity.Burst.Intrinsics.Arm.Neon;




namespace MarchingCubes
{
    [BurstCompile(
        CompileSynchronously = true,
        DisableSafetyChecks = true,
        FloatMode = FloatMode.Fast,
        FloatPrecision = FloatPrecision.Low)]
    public struct MeshingJob : IJob, System.IDisposable
    {
        public Mesher.Mode mode;
        public int xStart;
        public int xStop;

        [ReadOnly] public NativeArray<byte> triangulationTable;
        [ReadOnly] public NativeArray<byte> cornerIndexA;
        [ReadOnly] public NativeArray<byte> cornerIndexB;

        [ReadOnly] public NativeArray<sbyte> volume;
        [NativeDisableParallelForRestriction] public NativeList<float3> vertices;
        public UnsafePointer<Bounds> bounds;


        [BurstDiscard]
        public void Allocate()
        {
            vertices = new NativeList<float3>(500, Allocator.Persistent);
            bounds = new UnsafePointer<Bounds>(default);
        }

        [BurstDiscard]
        public void Dispose()
        {
            vertices.Dispose();
            bounds.Dispose();
        }

        private static readonly v128 MOVEPM_MASK = new v128(1u, 2u, 4u, 8u);
        public static int movemask_neon(v128 a)
        {
            if (IsNeonSupported)
            {
                // 1. vshrq_n_u32: Shift each 32-bit element right by 31 bits.
                // e.g., [0x80000001, 0x00000001, 0xFFFFFFFF, 0x40000000]
                //  ->   [         1,          0,          1,          0]
                v128 isolated_msbs = Arm.Neon.vshrq_n_u32(a, 31);

                // 2. vmulq_u32: Multiply by the bitmask [1, 2, 4, 8]
                //  ->   [1*1, 0*2, 1*4, 0*8]
                //  ->   [  1,   0,   4,   0]
                v128 masked_bits = Arm.Neon.vmulq_u32(isolated_msbs, MOVEPM_MASK);

                // 3. vaddvq_u32: "Vector Add Across Vector"
                // Sums all elements in the vector into a single scalar.
                //  ->   1 + 0 + 4 + 0 = 5
                // (This must be cast to int, as vaddvq_u32 returns a uint)
                return (int)Arm.Neon.vaddvq_u32(masked_bits);
            }
            return 0;
        }
        private static int NeonMoveMask_epi8(v128 input)
        {
            if (IsNeonSupported) { 
            // The v128 'temp' variable will be reused. The intrinsic function we call
            // determines how the bits in the register are interpreted for that operation.

            // 1. Start by treating 'input' as 16x 8-bit lanes.
            // Shift each byte right by 7, leaving only the most significant bit.
            // vshrq_n_u8: [b7 b6...b0, ...] -> [00...0 b7, ...]
            v128 temp = vshrq_n_u8(input, 7);

            // 2. Now, treat 'temp' as 8x 16-bit lanes for the next operation.
            // vsraq_n_u16 performs a "shift right and accumulate". It effectively
            // packs the high bits from adjacent pairs together.
            temp = vsraq_n_u16(temp, temp, 7);

            // 3. Now, treat 'temp' as 4x 32-bit lanes.
            // Repeat the process, packing the bits from adjacent 16-bit results.
            temp = vsraq_n_u32(temp, temp, 14);

            // 4. Finally, treat 'temp' as 2x 64-bit lanes.
            // This is the last packing step.
            temp = vsraq_n_u64(temp, temp, 28);

            // 5. The final 8 bits for each half of the original vector are now in the
            // lowest byte of each 64-bit lane. We extract these two bytes and
            // combine them to form the final 16-bit mask.
            return vgetq_lane_u8(temp, 0) | ((int)vgetq_lane_u8(temp, 8) << 8);
            }
            return 0;
        }

        // --- Add the NEON version of this method inside the MeshingJob struct ---

        private static unsafe (int, int) NeonExtractBitsAndSamples(sbyte* samples23, void* volumePtr, int x, int y = -1)
        {
            var ptr2 = (sbyte*)volumePtr + (x + 0) * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + (y + 1) * Chunk.ChunkSizeZ;
            var ptr3 = (sbyte*)volumePtr + (x + 1) * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + (y + 1) * Chunk.ChunkSizeZ;

            // Load 128-bit vectors
            v128 lo2 = vld1q_s8(ptr2 + 0);
            v128 hi2 = vld1q_s8(ptr2 + 16);
            v128 lo3 = vld1q_s8(ptr3 + 0);
            v128 hi3 = vld1q_s8(ptr3 + 16);

            // Interleave data for the next iteration's reuse
            vst1q_s8(samples23 + 0, vzip1q_s8(lo2, lo3));
            vst1q_s8(samples23 + 16, vzip2q_s8(lo2, lo3));
            vst1q_s8(samples23 + 32, vzip1q_s8(hi2, hi3));
            vst1q_s8(samples23 + 48, vzip2q_s8(hi2, hi3));

            // 1. Extract masks in natural order (z=0 is at LSB)
            int rawMask2 = NeonMoveMask_epi8(lo2) | (NeonMoveMask_epi8(hi2) << 16);
            int rawMask3 = NeonMoveMask_epi8(lo3) | (NeonMoveMask_epi8(hi3) << 16);

            // 2. CRITICAL FIX: Reverse the bits.
            // The main loop expects z=0 to be at the MSB (Bit 31).
            // Currently it is at Bit 0. math.reversebits fixes this.
            var signBits2 = math.reversebits(rawMask2);
            var signBits3 = math.reversebits(rawMask3);

            return (signBits2, signBits3);
        }

        public void Execute()
        {
                // Runtime check for the best available SIMD instruction set
            if (IsNeonSupported)
            {
                switch (mode)
                {
                    case Mesher.Mode.Simd32:
                        NEON_SIMDChunkSizeZ32(0, Chunk.ChunkSizeX - 1);
                        break;
                    case Mesher.Mode.Simd32Multithreaded:
                        NEON_SIMDChunkSizeZ32(xStart, xStop);
                        break;
                    default:
                        DefaultImplementation();
                        break;
                }
                return; // Exit after running NEON path
            }

                // Fallback to SSE if NEON is not supported (e.g., running in the Editor on a PC)
            if (IsSse41Supported)
            {
                switch (mode)
                {
                    case Mesher.Mode.Simd32:
                        SIMDChunkSizeZ32(0, Chunk.ChunkSizeX - 1);
                        break;
                    case Mesher.Mode.Simd32Multithreaded:
                        SIMDChunkSizeZ32(xStart, xStop);
                        break;
                    default:
                        DefaultImplementation();
                        break;
                }
                return; // Exit after running SSE path
            }

            // Final fallback to the naive implementation if no SIMD is available
            DefaultImplementation();
        }
    


        private unsafe void DefaultImplementation()
        {
            float4* samples = stackalloc float4[8];

            for (int x = 0; x < Chunk.ChunkSizeX - 1; x++)
            {
                for (int y = 0; y < Chunk.ChunkSizeY - 1; y++)
                {
                    for (int z = 0; z < Chunk.ChunkSizeZ - 1; z++)
                    {
                        const int flatOffsetX = Chunk.ChunkSizeY * Chunk.ChunkSizeZ;
                        const int flatOffsetY = Chunk.ChunkSizeZ;
                        const int flatOffsetZ = 1;
                        var flatIndex = FlattenIndex(x, y, z);

                        samples[0] = new float4(x + 0, y + 0, z + 0, volume[flatIndex]);                                            // volume[FlattenIndex(x + 0, y + 0, z + 0)]);
                        samples[1] = new float4(x + 1, y + 0, z + 0, volume[flatIndex + flatOffsetX]);                              // volume[FlattenIndex(x + 1, y + 0, z + 0)]);
                        samples[2] = new float4(x + 1, y + 0, z + 1, volume[flatIndex + flatOffsetX + flatOffsetZ]);                // volume[FlattenIndex(x + 1, y + 0, z + 1)]);
                        samples[3] = new float4(x + 0, y + 0, z + 1, volume[flatIndex + flatOffsetZ]);                              // volume[FlattenIndex(x + 0, y + 0, z + 1)]);
                        samples[4] = new float4(x + 0, y + 1, z + 0, volume[flatIndex + flatOffsetY]);                              // volume[FlattenIndex(x + 0, y + 1, z + 0)]);
                        samples[5] = new float4(x + 1, y + 1, z + 0, volume[flatIndex + flatOffsetX + flatOffsetY]);                // volume[FlattenIndex(x + 1, y + 1, z + 0)]);
                        samples[6] = new float4(x + 1, y + 1, z + 1, volume[flatIndex + flatOffsetX + flatOffsetY + flatOffsetZ]);  // volume[FlattenIndex(x + 1, y + 1, z + 1)]);
                        samples[7] = new float4(x + 0, y + 1, z + 1, volume[flatIndex + flatOffsetY + flatOffsetZ]);                // volume[FlattenIndex(x + 0, y + 1, z + 1)]);

                        int cornerMask = 0;

                        if (samples[0].w < 0.0f) cornerMask |= 1 << 0;  // order of bits is different than in original marching cubes
                        if (samples[1].w < 0.0f) cornerMask |= 1 << 1;  // only to make simd stuff easier
                        if (samples[2].w < 0.0f) cornerMask |= 1 << 5;  // read Mesher.Arrays.cs for more info
                        if (samples[3].w < 0.0f) cornerMask |= 1 << 4;
                        if (samples[4].w < 0.0f) cornerMask |= 1 << 2;  // default order: 76 54 32 10
                        if (samples[5].w < 0.0f) cornerMask |= 1 << 3;  // new order	: 67 23 54 10
                        if (samples[6].w < 0.0f) cornerMask |= 1 << 7;
                        if (samples[7].w < 0.0f) cornerMask |= 1 << 6;

                        if (cornerMask == 0 || cornerMask == 255)
                            continue;

                        cornerMask *= Mesher.TriangulationSubTableLenght;

                        bounds.item.Encapsulate(new Vector3(x, y, z));
                        bounds.item.Encapsulate(new Vector3(x + 1, y + 1, z + 1));

                        for (; triangulationTable[cornerMask] != 99; cornerMask += 3)
                        {
                            vertices.Length = vertices.Length + 3; // make room for 3 vertices
                            int a0 = cornerIndexA[triangulationTable[cornerMask]];
                            int b0 = cornerIndexB[triangulationTable[cornerMask]];
                            int a1 = cornerIndexA[triangulationTable[cornerMask + 1]];
                            int b1 = cornerIndexB[triangulationTable[cornerMask + 1]];
                            int a2 = cornerIndexA[triangulationTable[cornerMask + 2]];
                            int b2 = cornerIndexB[triangulationTable[cornerMask + 2]];
                            vertices[vertices.Length - 1] = InterpolateVerts(samples[a0], samples[b0]);
                            vertices[vertices.Length - 2] = InterpolateVerts(samples[a1], samples[b1]);
                            vertices[vertices.Length - 3] = InterpolateVerts(samples[a2], samples[b2]);
                        }
                    }
                }
            }
        }

        private unsafe void SIMDChunkSizeZ32(int xStart, int xStop)
        {
            // This check tells the Burst compiler to only compile this code block
            // if the target CPU supports the required SSE4.1 instruction set.
            // For ARM CPUs, this condition will be false, and the code inside will be stripped out.
            if (IsSse41Supported)
            {
                if (Chunk.ChunkSizeZ != 32)
                    throw new System.Exception("ChunkSize Z must be equal to 32 to use this function");

                xStart = math.clamp(xStart, 0, Chunk.ChunkSizeX - 1);
                xStop = math.clamp(xStop, 0, Chunk.ChunkSizeX - 1);
                if (xStart >= xStop)
                    return;

                sbyte* samplesBase = stackalloc sbyte[Chunk.ChunkSizeZ * 4];
                sbyte* samples01 = samplesBase + Chunk.ChunkSizeZ * 0;
                sbyte* samples23 = samplesBase + Chunk.ChunkSizeZ * 2;

                int signBits0, signBits1, signBits2, signBits3;

                float4* samples = stackalloc float4[8];

                for (int x = xStart; x < xStop; x++)
                {
                    (signBits2, signBits3) = SimdExtractBitsAndSamples(samples23, volume.GetUnsafeReadOnlyPtr(), x);

                    for (int y = 0; y < Chunk.ChunkSizeY - 1; y++)
                    {
                        // reuse previous step
                        var temp = samples01;
                        samples01 = samples23;
                        samples23 = temp;
                        signBits0 = signBits2;
                        signBits1 = signBits3;


                        (signBits2, signBits3) = SimdExtractBitsAndSamples(samples23, volume.GetUnsafeReadOnlyPtr(), x, y);


                        v128 signBits = new v128(signBits0, signBits1, signBits2, signBits3);


                        if (SameSigns(signBits))
                            continue;


                        // make sure there is enought capacity for vertices,
                        // otherwise, allocate bit more
                        if (vertices.Capacity < vertices.Length + 32 * 5)
                            vertices.Capacity = vertices.Length + 128 * 5;
                        var verticesPtr = (float3*)vertices.GetUnsafePtr();


                        int cornerMask = X86.Sse.movemask_ps(signBits) << 4;

                        for (int z = 0; z < Chunk.ChunkSizeZ - 1; z++)
                        {
                            cornerMask = cornerMask >> 4;
                            signBits = X86.Sse2.slli_epi32(signBits, 1);
                            cornerMask |= X86.Sse.movemask_ps(signBits) << 4;

                            if (cornerMask == 0 || cornerMask == 255)
                                continue;


                            var zz = z + z;
                            samples[0] = new float4(x + 0, y + 0, z + 0, samples01[zz + 0]);
                            samples[1] = new float4(x + 1, y + 0, z + 0, samples01[zz + 1]);
                            samples[2] = new float4(x + 1, y + 0, z + 1, samples01[zz + 3]);
                            samples[3] = new float4(x + 0, y + 0, z + 1, samples01[zz + 2]);
                            samples[4] = new float4(x + 0, y + 1, z + 0, samples23[zz + 0]);
                            samples[5] = new float4(x + 1, y + 1, z + 0, samples23[zz + 1]);
                            samples[6] = new float4(x + 1, y + 1, z + 1, samples23[zz + 3]);
                            samples[7] = new float4(x + 0, y + 1, z + 1, samples23[zz + 2]);

                            var triangulationTableIndex = cornerMask * Mesher.TriangulationSubTableLenght;

                            bounds.item.Encapsulate(new Vector3(x, y, z));
                            bounds.item.Encapsulate(new Vector3(x + 1, y + 1, z + 1));

                            for (; triangulationTable[triangulationTableIndex] != 99; triangulationTableIndex += 3)
                            {
                                vertices.Length = vertices.Length + 3; // make room for 3 vertices
                                int a0 = cornerIndexA[triangulationTable[triangulationTableIndex]];
                                int b0 = cornerIndexB[triangulationTable[triangulationTableIndex]];
                                int a1 = cornerIndexA[triangulationTable[triangulationTableIndex + 1]];
                                int b1 = cornerIndexB[triangulationTable[triangulationTableIndex + 1]];
                                int a2 = cornerIndexA[triangulationTable[triangulationTableIndex + 2]];
                                int b2 = cornerIndexB[triangulationTable[triangulationTableIndex + 2]];
                                verticesPtr[vertices.Length - 1] = InterpolateVerts(samples[a0], samples[b0]);
                                verticesPtr[vertices.Length - 2] = InterpolateVerts(samples[a1], samples[b1]);
                                verticesPtr[vertices.Length - 3] = InterpolateVerts(samples[a2], samples[b2]);
                            }
                        }
                    }
                }
            }
        }

        private unsafe void NEON_SIMDChunkSizeZ32(int xStart, int xStop)
        {
            // This check tells the Burst compiler to only compile this code block
            // if the target CPU supports the required SSE4.1 instruction set.
            // For ARM CPUs, this condition will be false, and the code inside will be stripped out.
            //#if UNITY_BURST_EXPERIMENTAL_NEON_INTRINSICS

            if (IsNeonSupported)
            {
                if (Chunk.ChunkSizeZ != 32)
                    throw new System.Exception("ChunkSize Z must be equal to 32 to use this function");

                xStart = math.clamp(xStart, 0, Chunk.ChunkSizeX - 1);
                xStop = math.clamp(xStop, 0, Chunk.ChunkSizeX - 1);
                if (xStart >= xStop)
                    return;

                sbyte* samplesBase = stackalloc sbyte[Chunk.ChunkSizeZ * 4];
                sbyte* samples01 = samplesBase + Chunk.ChunkSizeZ * 0;
                sbyte* samples23 = samplesBase + Chunk.ChunkSizeZ * 2;

                int signBits0, signBits1, signBits2, signBits3;

                float4* samples = stackalloc float4[8];

                for (int x = xStart; x < xStop; x++)
                {
                    (signBits2, signBits3) = NeonExtractBitsAndSamples(samples23, volume.GetUnsafeReadOnlyPtr(), x);

                    for (int y = 0; y < Chunk.ChunkSizeY - 1; y++)
                    {
                        // reuse previous step
                        var temp = samples01;
                        samples01 = samples23;
                        samples23 = temp;
                        signBits0 = signBits2;
                        signBits1 = signBits3;


                        (signBits2, signBits3) = NeonExtractBitsAndSamples(samples23, volume.GetUnsafeReadOnlyPtr(), x, y);


                        v128 signBits = new v128(signBits0, signBits1, signBits2, signBits3);


                        if (SameSigns(signBits))
                            continue;


                        // make sure there is enought capacity for vertices,
                        // otherwise, allocate bit more
                        if (vertices.Capacity < vertices.Length + 32 * 5)
                            vertices.Capacity = vertices.Length + 128 * 5;
                        var verticesPtr = (float3*)vertices.GetUnsafePtr();


                        int cornerMask = movemask_neon(signBits) << 4;

                        for (int z = 0; z < Chunk.ChunkSizeZ - 1; z++)
                        {
                            cornerMask = cornerMask >> 4;
                            signBits = vshlq_n_s32(signBits, 1);
                            cornerMask |= movemask_neon(signBits) << 4;

                            if (cornerMask == 0 || cornerMask == 255)
                                continue;


                            var zz = z + z;
                            samples[0] = new float4(x + 0, y + 0, z + 0, samples01[zz + 0]);
                            samples[1] = new float4(x + 1, y + 0, z + 0, samples01[zz + 1]);
                            samples[2] = new float4(x + 1, y + 0, z + 1, samples01[zz + 3]);
                            samples[3] = new float4(x + 0, y + 0, z + 1, samples01[zz + 2]);
                            samples[4] = new float4(x + 0, y + 1, z + 0, samples23[zz + 0]);
                            samples[5] = new float4(x + 1, y + 1, z + 0, samples23[zz + 1]);
                            samples[6] = new float4(x + 1, y + 1, z + 1, samples23[zz + 3]);
                            samples[7] = new float4(x + 0, y + 1, z + 1, samples23[zz + 2]);

                            var triangulationTableIndex = cornerMask * Mesher.TriangulationSubTableLenght;

                            bounds.item.Encapsulate(new Vector3(x, y, z));
                            bounds.item.Encapsulate(new Vector3(x + 1, y + 1, z + 1));

                            for (; triangulationTable[triangulationTableIndex] != 99; triangulationTableIndex += 3)
                            {
                                vertices.Length = vertices.Length + 3; // make room for 3 vertices
                                int a0 = cornerIndexA[triangulationTable[triangulationTableIndex]];
                                int b0 = cornerIndexB[triangulationTable[triangulationTableIndex]];
                                int a1 = cornerIndexA[triangulationTable[triangulationTableIndex + 1]];
                                int b1 = cornerIndexB[triangulationTable[triangulationTableIndex + 1]];
                                int a2 = cornerIndexA[triangulationTable[triangulationTableIndex + 2]];
                                int b2 = cornerIndexB[triangulationTable[triangulationTableIndex + 2]];
                                verticesPtr[vertices.Length - 1] = InterpolateVerts(samples[a0], samples[b0]);
                                verticesPtr[vertices.Length - 2] = InterpolateVerts(samples[a1], samples[b1]);
                                verticesPtr[vertices.Length - 3] = InterpolateVerts(samples[a2], samples[b2]);
                            }
                        }
                    }
                }
            }
            //#endif

        }
        private static unsafe (int, int) SimdExtractBitsAndSamples(sbyte* samples23, void* volumePtr, int x, int y = -1 /* first step */)
        {
            // This method uses instructions up to SSSE3.
            if (IsSsse3Supported)
            {
                v128 shuffleReverseByteOrder = new v128(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

                var ptr2 = (sbyte*)volumePtr + (x + 0) * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + (y + 1) * Chunk.ChunkSizeZ;
                var ptr3 = (sbyte*)volumePtr + (x + 1) * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + (y + 1) * Chunk.ChunkSizeZ;
                v128 lo2 = X86.Sse2.load_si128(ptr2 + 0);
                v128 hi2 = X86.Sse2.load_si128(ptr2 + 16);
                v128 lo3 = X86.Sse2.load_si128(ptr3 + 0);
                v128 hi3 = X86.Sse2.load_si128(ptr3 + 16);
                X86.Sse2.store_si128(samples23 + 00, X86.Sse2.unpacklo_epi8(lo2, lo3));
                X86.Sse2.store_si128(samples23 + 16, X86.Sse2.unpackhi_epi8(lo2, lo3));
                X86.Sse2.store_si128(samples23 + 32, X86.Sse2.unpacklo_epi8(hi2, hi3));
                X86.Sse2.store_si128(samples23 + 48, X86.Sse2.unpackhi_epi8(hi2, hi3));
                lo2 = X86.Ssse3.shuffle_epi8(lo2, shuffleReverseByteOrder);
                lo3 = X86.Ssse3.shuffle_epi8(lo3, shuffleReverseByteOrder);
                hi2 = X86.Ssse3.shuffle_epi8(hi2, shuffleReverseByteOrder);
                hi3 = X86.Ssse3.shuffle_epi8(hi3, shuffleReverseByteOrder);
                var signBits2 = (X86.Sse2.movemask_epi8(lo2) << 16 | (X86.Sse2.movemask_epi8(hi2)));
                var signBits3 = (X86.Sse2.movemask_epi8(lo3) << 16 | (X86.Sse2.movemask_epi8(hi3)));
                return (signBits2, signBits3);
            }

            // Provide a default return path for unsupported CPUs.
            return (0, 0);
        }

        private static bool SameSigns(v128 signBits)
        {
            // This method uses an SSE4.1 instruction.
            if (IsSse41Supported)
            {
                var maskAllOnes = new v128(uint.MaxValue);
                return X86.Sse4_1.test_mix_ones_zeroes(signBits, maskAllOnes) == 0;
            }
            else if (IsNeonSupported)
            {
                // 1. Check if all bits are zero.
                // We treat the 128-bit vector as 4 x 32-bit unsigned integers.
                // vmaxvq_u32 finds the maximum u32 value across the entire vector.
                // If the maximum value is 0, the whole vector must be 0.
                bool isAllZeros = vmaxvq_u32(signBits) == 0;

                // 2. Check if all bits are one.
                // vmvnq_u32 is "Vector Bitwise NOT (unsigned 32-bit)".
                // This inverts all bits in the vector.
                v128 invertedBits = vmvnq_u32(signBits);

                // Now, check if this new inverted vector is all zeros,
                // using the same logic as before.
                bool isAllOnes = vmaxvq_u32(invertedBits) == 0;

                return isAllZeros || isAllOnes;
            }
            // Provide a default return path for unsupported CPUs.
            return false;
        }



        private static int FlattenIndex(int x, int y, int z) => x * Chunk.ChunkSizeY * Chunk.ChunkSizeZ + y * Chunk.ChunkSizeZ + z;

        private static float3 InterpolateVerts(float4 v1, float4 v2)
        {
            // both version, simd and default assumes that the isolevel is equal to 0
            // its easy to change this in default version (those 8 if's)
            // but simd version extract sign bits, so it might be harder to implement different isolevels.
            const float isoLevel = 0.0f;
            float t = (isoLevel - v1.w) / (v2.w - v1.w);
            return (v1 + t * (v2 - v1)).xyz;
        }



        // bad idea :(
        private static float3 GetInterpolatedVertex(int edge, float sample0, float sample1)
        {
            var t = (-sample0) / (sample1 - sample0);
            switch (edge)
            {
                case 0: return new float3(t, 0, 0);
                case 1: return new float3(1, 0, t);
                case 2: return new float3(1 - t, 0, 1);
                case 3: return new float3(0, 0, 1 - t);

                case 4: return new float3(t, 1, 0);
                case 5: return new float3(1, 1, t);
                case 6: return new float3(1 - t, 1, 1);
                case 7: return new float3(0, 1, 1 - t);

                case 8: return new float3(0, t, 0);
                case 9: return new float3(1, t, 0);
                case 10: return new float3(1, t, 1);
                case 11: return new float3(0, t, 1);

            }
            return default;
        }
    }
}