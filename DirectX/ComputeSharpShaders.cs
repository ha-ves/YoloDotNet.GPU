using ComputeSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YoloDotNet.Models;

namespace YoloDotNet.GPU.DirectX
{
    public static partial class ComputeSharpShaders
    {
        /// <summary>
        /// Resizing algorithm is Linear.
        /// </summary>
        [ThreadGroupSize(DefaultThreadGroupSizes.XYZ)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct NormalizePixelsToTensorSingle_3D : IComputeShader
        {
            /// <summary>
            /// The constructor is run on Host (managed).
            /// </summary>
            /// <param name="image">source image.</param>
            /// <param name="tensor">input tensor.</param>
            /// <param name="keepAspect"></param>
            /// <param name="emptyResizePixelFill"></param>
            public NormalizePixelsToTensorSingle_3D(
                ReadOnlyTexture2D<Bgra32, Float4> image,
                ReadWriteTexture3D<float> tensor,
                bool keepAspect = true,
                Float3 emptyResizePixelFill = default
            )
            {
                InputPixels = image;
                InputTensor = tensor;

                if (!keepAspect)
                {
                    KeepAspect = 0;
                    return;
                }
                else
                {
                    //KeepAspect = Random.Shared.Next();
                    KeepAspect = 1;
                }

                int modelWidth = tensor.Width;
                int modelHeight = tensor.Height;
                int width = image.Width;
                int height = image.Height;

                // Calculate the new image size based on the aspect ratio
                float scaleFactor = Math.Min((float)modelWidth / width, (float)modelHeight / height);
                int newWidth = (int)Math.Floor(width * scaleFactor);
                int newHeight = (int)Math.Floor(height * scaleFactor);

                // Calculate the destination rectangle within the model dimensions
                int x = (modelWidth - newWidth) / 2;
                int y = (modelHeight - newHeight) / 2;

                resizeRect = new ResizeRect(
                    new(1f / newWidth, 1f / newHeight),
                    new(x, y),
                    new(x + newWidth, y + newHeight),
                    emptyResizePixelFill);
            }

            public struct ResizeRect(Float2 wh, Int2 leftTop, Int2 botRight, Float3 empFiller)
            {
                public readonly Float2 NormalizedWidthHeight = wh;
                public readonly Int2 LeftTop = leftTop;
                public readonly Int2 BottomRight = botRight;
                public readonly Float3 EmptyPixelFiller = empFiller;
            }

            readonly ReadOnlyTexture2D<Bgra32, Float4> InputPixels;
            readonly ReadWriteTexture3D<float> InputTensor;
            readonly int KeepAspect = 0;

            readonly ResizeRect resizeRect;

            public void Execute()
            {
                //if(Hlsl.IntToBool(KeepAspect))
                if (KeepAspect == 0)
                {
                    // TODO: another texture2D to save sampled texture?
                    var px = InputPixels.Sample(ThreadIds.Normalized.XY);
                    InputTensor[ThreadIds.XYZ] = px[ThreadIds.Z];
                }
                else
                {
                    if (Hlsl.Any(
                        new Bool2x2(ThreadIds.XY <= resizeRect.LeftTop,
                                        ThreadIds.XY >= resizeRect.BottomRight)
                    ))
                    {
                        InputTensor[ThreadIds.XYZ] = resizeRect.EmptyPixelFiller[ThreadIds.Z];
                    }
                    else
                    {
                        // TODO: another texture2D to save sampled texture?
                        var start = (ThreadIds.XY - resizeRect.LeftTop) * resizeRect.NormalizedWidthHeight;
                        var px = InputPixels.Sample(start);
                        InputTensor[ThreadIds.XYZ] = px[ThreadIds.Z];
                    }
                }
            }
        }

        /// <summary>
        /// Resizing algorithm is Linear.
        /// </summary>
        [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct NormalizePixelsToTensorSingle_2D : IComputeShader
        {
            /// <summary>
            /// The constructor is run on Host (managed).
            /// </summary>
            /// <param name="image">source image.</param>
            /// <param name="tensor">input tensor.</param>
            /// <param name="keepAspect"></param>
            /// <param name="emptyResizePixelFill"></param>
            public NormalizePixelsToTensorSingle_2D(
                ReadOnlyTexture2D<Bgra32, Float4> image,
                ReadWriteTexture3D<float> tensor,
                bool keepAspect = true,
                Float3 emptyResizePixelFill = default
            )
            {
                InputPixels = image;
                InputTensor = tensor;

                if (!keepAspect)
                {
                    KeepAspect = 0;
                    return;
                }
                else
                {
                    //KeepAspect = Random.Shared.Next();
                    KeepAspect = 1;
                }

                int modelWidth = tensor.Width;
                int modelHeight = tensor.Height;
                int width = image.Width;
                int height = image.Height;

                // Calculate the new image size based on the aspect ratio
                float scaleFactor = Math.Min((float)modelWidth / width, (float)modelHeight / height);
                int newWidth = (int)Math.Floor(width * scaleFactor);
                int newHeight = (int)Math.Floor(height * scaleFactor);

                // Calculate the destination rectangle within the model dimensions
                int x = (modelWidth - newWidth) / 2;
                int y = (modelHeight - newHeight) / 2;

                resizeRect = new ResizeRect(
                    new(1f / newWidth, 1f / newHeight),
                    new(x, y),
                    new(x + newWidth, y + newHeight),
                    emptyResizePixelFill);
            }

            public struct ResizeRect(Float2 wh, Int2 leftTop, Int2 botRight, Float3 empFiller)
            {
                public readonly Float2 NormalizedWidthHeight = wh;
                public readonly Int2 LeftTop = leftTop;
                public readonly Int2 BottomRight = botRight;
                public readonly Float3 EmptyPixelFiller = empFiller;
            }

            readonly ReadOnlyTexture2D<Bgra32, Float4> InputPixels;
            readonly ReadWriteTexture3D<float> InputTensor;
            readonly int KeepAspect = 0;

            readonly ResizeRect resizeRect;

            public void Execute()
            {
                //if(Hlsl.IntToBool(KeepAspect))
                if (KeepAspect == 0)
                {
                    var px = InputPixels.Sample(ThreadIds.Normalized.XY);
                    InputTensor[new(ThreadIds.XY, 0)] = px.R;
                    InputTensor[new(ThreadIds.XY, 1)] = px.G;
                    InputTensor[new(ThreadIds.XY, 2)] = px.B;
                }
                else
                {
                    if (Hlsl.Any(
                        new Bool2x2(ThreadIds.XY <= resizeRect.LeftTop,
                                        ThreadIds.XY >= resizeRect.BottomRight)
                    ))
                    {
                        InputTensor[new(ThreadIds.XY, 0)] = resizeRect.EmptyPixelFiller.R;
                        InputTensor[new(ThreadIds.XY, 1)] = resizeRect.EmptyPixelFiller.G;
                        InputTensor[new(ThreadIds.XY, 2)] = resizeRect.EmptyPixelFiller.B;
                    }
                    else
                    {
                        var start = (ThreadIds.XY - resizeRect.LeftTop) * resizeRect.NormalizedWidthHeight;
                        var px = InputPixels.Sample(start);
                        InputTensor[new(ThreadIds.XY, 0)] = px.R;
                        InputTensor[new(ThreadIds.XY, 1)] = px.G;
                        InputTensor[new(ThreadIds.XY, 2)] = px.B;
                    }
                }
            }
        }
    }
}
