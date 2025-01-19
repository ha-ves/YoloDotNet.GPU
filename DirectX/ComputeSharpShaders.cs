using ComputeSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoloDotNet.GPU.DirectX
{
    public static partial class ComputeSharpShaders
    {
        [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct NormalizeRgbaToSingleTensor( 
            ReadOnlyTexture2D<int> RgbaData,
            ReadWriteTexture3D<float> DenseTensor,
            float NormalizeBy = 255f)
            : IComputeShader
        {
            readonly float normMul = 1f / NormalizeBy;

            public void Execute()
            {
                // Read the 32-bit ARGB pixel value
                var px = RgbaData[ThreadIds.XY];
                var pixel = new Float3(
                    px & 0xFF,           // Red
                    (px >> 8) & 0xFF,    // Green
                    (px >> 16) & 0xFF    // Blue
                    //(px >> 24) & 0xFF   // Alpha
                );

                // Skip processing if RGB channels are zero (regardless of A-channel opacity)
                if (px << 8 == 0)
                {
                    return;
                }

                var N_pixel = Hlsl.Mul(pixel.RGB, normMul);

                DenseTensor[new(ThreadIds.XY, 0)] = N_pixel.R;
                DenseTensor[new(ThreadIds.XY, 1)] = N_pixel.G;
                DenseTensor[new(ThreadIds.XY, 2)] = N_pixel.B;
            }
        }
    }
}
