using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using ComputeSharp;
using Microsoft.ML.OnnxRuntime;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YoloDotNet.Extensions;
using static System.Net.Mime.MediaTypeNames;
using static YoloDotNet.GPU.DirectX.ComputeSharpShaders;

namespace YoloDotNet.GPU.Benchmark
{
    public unsafe partial class DirectX_Pre
    {

        [GlobalSetup]
        public unsafe void Setup()
        {
            src = SKImage.FromEncodedData(@"WIN_20240405_13_55_52_Pro.jpg");
            img = src.ToRasterImage(true);
            bmp = img.ResizeImage(_imageInfo);

            GPU = GraphicsDevice.GetDefault();

            _tensorBufferSize = 1 * 3 * 640 * 640;
            pixelsPerChannel = _tensorBufferSize / 3;

            customSizeFloatPool = ArrayPool<float>.Create(maxArrayLength: _tensorBufferSize + 1, maxArraysPerBucket: 10);

            _rgbaDataGPU_In = GPU!.AllocateReadOnlyTexture2D<Bgra32, Float4>(640, 640, AllocationMode.Clear);
            _normTensorGPU_Out = GPU!.AllocateReadWriteTexture3D<float>(640, 640, 3, AllocationMode.Clear);
            _ortTensorHost_Read = Marshal.AllocHGlobal(_tensorBufferSize * sizeof(float));

            var inputOrtValue = OrtValue.CreateTensorValueWithData(
                    OrtMemoryInfo.DefaultInstance,
                    Microsoft.ML.OnnxRuntime.Tensors.TensorElementType.Float,
                    _inShape,
                    _ortTensorHost_Read,
                    _tensorBufferSize * sizeof(float));

            inputNames = new Dictionary<string, OrtValue>
            {
                { "input", inputOrtValue }
            };
        }

        private GraphicsDevice? GPU = null;
        private ReadOnlyTexture2D<Bgra32, Float4>? _rgbaDataGPU_In;
        private ReadWriteTexture3D<float>? _normTensorGPU_Out;
        private nint _ortTensorHost_Read;

        int pixelsPerChannel, _tensorBufferSize;
        SKImage? src, img;
        SKBitmap? bmp;
        public ArrayPool<float> customSizeFloatPool = default!;
        SKImageInfo _imageInfo = new(640, 640, SKColorType.Rgb888x, SKAlphaType.Opaque);
        private Dictionary<string, OrtValue>? inputNames;


        long[] _inShape = [1, 3, 640, 640];


        [Benchmark]
        public unsafe void ComputeSharp_2D_KeepAspect()
        {
            PreComputeCopy();

            GPU!.For(640, 640, new NormalizePixelsToTensorSingle_2D(_rgbaDataGPU_In!, _normTensorGPU_Out!));

            PostComputeCopy();
        }

        [Benchmark]
        public unsafe void ComputeSharp_2D_Stretch()
        {
            PreComputeCopy();

            GPU!.For(640, 640, new NormalizePixelsToTensorSingle_2D(_rgbaDataGPU_In!, _normTensorGPU_Out!, false));

            PostComputeCopy();
        }

        [Benchmark]
        public unsafe void ComputeSharp_3D_KeepAspect()
        {
            PreComputeCopy();

            GPU!.For(640, 640, 3, new NormalizePixelsToTensorSingle_3D(_rgbaDataGPU_In!, _normTensorGPU_Out!));

            PostComputeCopy();
        }

        [Benchmark]
        public unsafe void ComputeSharp_3D_Stretch()
        {
            PreComputeCopy();

            GPU!.For(640, 640, 3, new NormalizePixelsToTensorSingle_3D(_rgbaDataGPU_In!, _normTensorGPU_Out!, false));

            PostComputeCopy();
        }

        [Benchmark]
        public unsafe void ComputeSharp_2D_KeepAspect_NoPostCopy()
        {
            PreComputeCopy();

            GPU!.For(640, 640, new NormalizePixelsToTensorSingle_2D(_rgbaDataGPU_In!, _normTensorGPU_Out!));
        }

        [Benchmark]
        public unsafe void ComputeSharp_2D_Stretch_NoPostCopy()
        {
            PreComputeCopy();

            GPU!.For(640, 640, new NormalizePixelsToTensorSingle_2D(_rgbaDataGPU_In!, _normTensorGPU_Out!, false));
        }

        [Benchmark]
        public unsafe void ComputeSharp_3D_KeepAspect_NoPostCopy()
        {
            PreComputeCopy();

            GPU!.For(640, 640, 3, new NormalizePixelsToTensorSingle_3D(_rgbaDataGPU_In!, _normTensorGPU_Out!));
        }

        [Benchmark]
        public unsafe void ComputeSharp_3D_Stretch_NoPostCopy()
        {
            PreComputeCopy();

            GPU!.For(640, 640, 3, new NormalizePixelsToTensorSingle_3D(_rgbaDataGPU_In!, _normTensorGPU_Out!, false));
        }

        [Benchmark(Baseline = true)]
        public unsafe void Basic()
        {
            var resizedImage = src!.ResizeImage(_imageInfo);
            
            var tensorArrayBuffer = customSizeFloatPool.Rent(minimumLength: _tensorBufferSize);

            try
            {
                var tensorPixels = resizedImage.NormalizePixelsToTensor(_inShape, _tensorBufferSize, tensorArrayBuffer);

                var inputOrtValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, tensorPixels.Buffer, _inShape);

                inputNames = new Dictionary<string, OrtValue>
                {
                    { "input", inputOrtValue }
                };
            }
            finally
            {
                customSizeFloatPool.Return(tensorArrayBuffer, true);
            }

        }

        private void PreComputeCopy()
        {
            var sp = new ReadOnlySpan<Bgra32>((void*)img!.PeekPixels().GetPixels(), _imageInfo.Width * _imageInfo.Height);

            unsafe
            {
                _rgbaDataGPU_In!.CopyFrom(sp);
            }
        }

        private void PostComputeCopy()
        {
            var vsp = new Span<float>((void*)_ortTensorHost_Read!, _tensorBufferSize);
            //TODO: Directly use the D3D12Resource with DirectML
            unsafe
            {
                _normTensorGPU_Out!.CopyTo(vsp);
            }
        }

        [ThreadGroupSize(DefaultThreadGroupSizes.XY)]
        [GeneratedComputeShaderDescriptor]
        public readonly partial struct LinearSampling : IComputeShader
        {
            public LinearSampling(
                ReadOnlyTexture2D<Bgra32, Float4> input,
                ReadWriteTexture2D<Bgra32, Float4> resized,
                bool keepAspect = true
                )
            {
                RgbaData_Input = input;
                RgbaData_Resize = resized;

                int modelWidth = resized.Width;
                int modelHeight = resized.Height;
                int width = input.Width;
                int height = input.Height;

                // Calculate the new image size based on the aspect ratio
                float scaleFactor = Math.Min((float)modelWidth / width, (float)modelHeight / height);
                int newWidth = (int)Math.Floor(width * scaleFactor);
                int newHeight = (int)Math.Floor(height * scaleFactor);

                // Calculate the destination rectangle within the model dimensions
                int x = (modelWidth - newWidth) / 2;
                int y = (modelHeight - newHeight) / 2;

                resizeRect = new ResizeRect(new(1f / newWidth, 1f / newHeight), new(x, y), new(x + newWidth, y + newHeight));
            }

            public struct ResizeRect(Float2 wh, Int2 leftTop, Int2 botRight)
            {
                public readonly Float2 NormalizedWidthHeight = wh;
                public readonly Int2 LeftTop = leftTop;
                public readonly Int2 BottomRight = botRight;
            }

            readonly ReadOnlyTexture2D<Bgra32, Float4> RgbaData_Input;
            readonly ReadWriteTexture2D<Bgra32, Float4> RgbaData_Resize;
            readonly ResizeRect resizeRect;

            public void Execute()
            {
                if (Hlsl.Any(new Bool4(ThreadIds.XY <= resizeRect.LeftTop, ThreadIds.XY >= resizeRect.BottomRight)))
                {
                    RgbaData_Resize[ThreadIds.XY] = new Float4(0f, 0f, 0f, 1f);
                }
                else
                {
                    float stInt_X = (ThreadIds.XY - resizeRect.LeftTop).X;
                    float stInt_Y = (ThreadIds.XY - resizeRect.LeftTop).Y;

                    var start = (ThreadIds.XY - resizeRect.LeftTop) * resizeRect.NormalizedWidthHeight;
                    RgbaData_Resize[ThreadIds.XY].RGBA = RgbaData_Input.Sample(start);
                }
            }
        }

        public class FloatComparer(int digits) : IEqualityComparer<float>
        {
            public bool Equals(float x, float y)
            {
                return Math.Round(x, digits, MidpointRounding.ToZero) == Math.Round(y, digits, MidpointRounding.ToZero);
            }

            public int GetHashCode([DisallowNull] float obj)
            {
                return obj.GetHashCode();
            }
        }

        public static void DirectX_Main(string[] args)
        {
            var pr = new DirectX_Pre();
            pr.Setup();

            pr.ComputeSharp_2D_KeepAspect();
            var cmpSharp = pr.inputNames!["input"].GetTensorDataAsSpan<float>();
            var cok = Unsafe.AsPointer(ref MemoryMarshal.GetReference(cmpSharp));

            pr.Basic();
            var @base = pr.inputNames!["input"].GetTensorDataAsSpan<float>();
            var bok = Unsafe.AsPointer(ref MemoryMarshal.GetReference(@base));

            var bbs = @base.ToArray();

            var notmatchindex = cmpSharp.ToImmutableArray().Select((f, i) =>
            {
                var cmpF = (float)Math.Round(f, 6, MidpointRounding.ToZero);
                var baseF = (float)Math.Round(bbs[i], 6, MidpointRounding.ToZero);

                if (cmpF == baseF)
                    return (0, 0, 0, 0);

                return (i, (float)cmpF, (float)baseF, (float)Math.Abs(cmpF - baseF));
            }).Where(f => f.i != 0);

            var ok6 = @base.SequenceEqual(cmpSharp, new FloatComparer(6));
            Debugger.Break();


            var cfg = DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddDiagnoser(MemoryDiagnoser.Default);

            BenchmarkRunner.Run<DirectX_Pre>(cfg);
        }
    }
}
