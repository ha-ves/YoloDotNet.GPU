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
        public readonly partial struct Rgba32ToNormalizedTensor(ReadWriteTexture2D<Rgba32> src, ReadWriteBuffer<float> tensor) : IComputeShader
        {
            public void Execute()
            {
                throw new NotImplementedException();
            }
        }
    }
}
