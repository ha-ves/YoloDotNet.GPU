using ComputeSharp;
using Microsoft.ML.OnnxRuntime;
using SkiaSharp;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using YoloDotNet.Core;
using YoloDotNet.Extensions;
using YoloDotNet.Models;
using static YoloDotNet.GPU.DirectX.ComputeSharpShaders;

namespace YoloDotNet.GPU.Core
{
    [SupportedOSPlatform("windows")]
    public class YoloCoreDirectX : YoloCoreGPUBase
    {
        public YoloCoreDirectX(string onnxModel, bool useCuda, bool allocateGpuMemory, int gpuId) : base(onnxModel, useCuda, allocateGpuMemory, gpuId)
        {
        }

        new public void InitializeYolo(YoloOptions opts)
        {
            base.InitializeYolo(opts);

            InitializeDirectCompute();
        }

        private GraphicsDevice? GPU = null;
        private bool _isGPULost = true;
        private string _reasonGPULost = string.Empty;

        //private UploadTexture2D<int>? _skBmpHost_Write;
        private nint _skBmpHost_Write;
        //private ReadOnlyTexture2D<int>? _rgbaDataGPU_In;
        private ReadOnlyTexture2D<Bgra32, Float4>? _rgbaDataGPU_In;

        private ReadWriteTexture3D<float>? _normTensorGPU_Out;
        //private ReadBackTexture3D<float>? _inputTensorHost_Read;
        private nint _ortTensorHost_Read;

        private Dictionary<string, OrtValue>? inputNames;

        private unsafe void InitializeDirectCompute()
        {
            GPU = GraphicsDevice.GetDefault();
            #warning GPU device lost is kinda troublesome with the current YoloCore code-architecture
            _isGPULost = false;
            GPU.DeviceLost += OnComputeDeviceLost;

            _skBmpHost_Write = Marshal.AllocHGlobal(_imageInfo.BytesSize);
            //_skBmpHost_Write = GPU!.AllocateUploadTexture2D<int>(OnnxModel.Input.Width, OnnxModel.Input.Height, AllocationMode.Clear);
            //_rgbaDataGPU_In = GPU!.AllocateReadOnlyTexture2D<int>(OnnxModel.Input.Width, OnnxModel.Input.Height, AllocationMode.Clear);
            _rgbaDataGPU_In = GPU!.AllocateReadOnlyTexture2D<Bgra32, Float4>(OnnxModel.Input.Width, OnnxModel.Input.Height, AllocationMode.Clear);

            _normTensorGPU_Out = GPU!.AllocateReadWriteTexture3D<float>(OnnxModel.Input.Width, OnnxModel.Input.Height, OnnxModel.Input.Channels, AllocationMode.Clear);
            //_inputTensorHost_Read = GPU!.AllocateReadBackTexture3D<float>(OnnxModel.Input.Width, OnnxModel.Input.Height, OnnxModel.Input.Channels, AllocationMode.Clear);
            _ortTensorHost_Read = Marshal.AllocHGlobal(_tensorBufferSize * sizeof(float));

            var inputOrtValue = OrtValue.CreateTensorValueWithData(
                    OrtMemoryInfo.DefaultInstance,
                    Microsoft.ML.OnnxRuntime.Tensors.TensorElementType.Float,
                    OnnxModel.InputShape,
                    _ortTensorHost_Read,
                    _tensorBufferSize * sizeof(float));
            //var inputOrtValue = OrtValue.CreateTensorValueFromMemory(
            //    OrtMemoryInfo.DefaultInstance, _inputTensorHost_Read.View.span, OnnxModel.InputShape);

            inputNames = new Dictionary<string, OrtValue>
            {
                { OnnxModel.InputName, inputOrtValue }
            };
        }

        private void OnComputeDeviceLost(object? sender, DeviceLostEventArgs e)
        {
            _isGPULost = true;

            _reasonGPULost = e.Reason.ToString();

            GPU?.Dispose();

            Marshal.FreeHGlobal(_skBmpHost_Write);
            //_skBmpHost_Write?.Dispose();
            _rgbaDataGPU_In?.Dispose();
            _normTensorGPU_Out?.Dispose();
            Marshal.FreeHGlobal(_ortTensorHost_Read);
            //_inputTensorHost_Read?.Dispose();

            InitializeDirectCompute();
        }

        private readonly object _inferenceLock = new();

        public override IDisposableReadOnlyCollection<OrtValue> Run(SKImage image)
        {
            lock (_inferenceLock)
            {
                #warning this is BAD. Even the base method has no guard for exceptions.
                if (_isGPULost)
                    throw new InvalidOperationException($"Something bad happened. {_reasonGPULost}.");

                var (batchSize, colorChannels, width, height) = ((int)OnnxModel.InputShape[0], (int)OnnxModel.InputShape[1], (int)OnnxModel.InputShape[2], (int)OnnxModel.InputShape[3]);
                var pixelsPerChannel = _tensorBufferSize / colorChannels;

                //PrepareImageData(image);

                //GPU!.For(width, height, new NormalizeRgbaToSingleTensor(_rgbaDataGPU_In!, _normTensorGPU_Out!));
                //GPU!.For(width, height, new NormalizePixelsToTensorSingle_2D(_rgbaDataGPU_In!, _normTensorGPU_Out!));
                GPU!.For(width, height, colorChannels, new NormalizePixelsToTensorSingle_3D(_rgbaDataGPU_In!, _normTensorGPU_Out!));

                #warning its bad to copy back and forth, but afaik, can only directly interop with DirectML.
                //TODO: Directly use the D3D12Resource with DirectML
                unsafe
                {
                    _normTensorGPU_Out!.CopyTo(new Span<float>((void*)_ortTensorHost_Read!, _tensorBufferSize));
                }

                return _session.Run(_runOptions, inputNames!, OnnxModel.OutputNames);
            }
        }

        private SKSizeI _lastInImgSize;
        private SKRectI _resizeInfo;

        /// <summary>
        /// Resizes the SKImage into the <see cref="_skBmpHost_Write"/> buffer.
        /// </summary>
        /// <remarks>
        /// Since SkiaSharp 3.x the <see cref="SKImage.ScalePixels(SKPixmap, SKSamplingOptions)"/>
        /// <br/> now seems to have been optimized and feasible to be used.
        /// </remarks>
        /// <param name="img"></param>
        private void PrepareImageData(SKImage img)
        {
            if (!img.Info.Size.Equals(_lastInImgSize))
            {
                _lastInImgSize = img.Info.Size;

                int modelWidth = _imageInfo.Width;
                int modelHeight = _imageInfo.Height;
                int width = img.Width;
                int height = img.Height;

                // Calculate the new image size based on the aspect ratio
                float scaleFactor = Math.Min((float)modelWidth / width, (float)modelHeight / height);
                int newWidth = (int)Math.Floor(width * scaleFactor);
                int newHeight = (int)Math.Floor(height * scaleFactor);

                // Calculate the destination rectangle within the model dimensions
                int x = (modelWidth - newWidth) / 2;
                int y = (modelHeight - newHeight) / 2;

                _resizeInfo = new SKRectI()
                {
                    Left = x,
                    Top = y,
                    Size = new(newWidth, newHeight)
                };
            }

            var pxm = new SKPixmap(_imageInfo.WithSize(_resizeInfo.Size), _skBmpHost_Write + (_resizeInfo.Left + _resizeInfo.Top * _imageInfo.RowBytes));
            img.ScalePixels(pxm, SKSamplingOptions.Default);

            unsafe
            {
                _rgbaDataGPU_In!.CopyFrom(MemoryMarshal.Cast<int, Bgra32>(new ReadOnlySpan<int>((void*)_skBmpHost_Write, _imageInfo.Width * _imageInfo.Height)));
            }
        }
    }
}
