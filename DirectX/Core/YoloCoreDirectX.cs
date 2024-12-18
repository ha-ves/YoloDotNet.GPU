using ComputeSharp;
using Microsoft.ML.OnnxRuntime;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using YoloDotNet.Core;
using YoloDotNet.Extensions;

namespace YoloDotNet.GPU.Core
{
    [SupportedOSPlatform("windows")]
    public class YoloCoreDirectX : YoloCoreGPUBase
    {
        public YoloCoreDirectX(string onnxModel, bool useCuda, bool allocateGpuMemory, int gpuId) : base(onnxModel, useCuda, allocateGpuMemory, gpuId)
        {

        }

        private readonly object _inferenceLock = new();
/*
        public override IDisposableReadOnlyCollection<OrtValue> Run(SKImage image)
        { 
            // resize image
            

            var tensorArrayBuffer = customSizeFloatPool.Rent(minimumLength: _tensorBufferSize);

            try
            {
                lock (_inferenceLock)
                {
                    // get densetensor

                    var tensorPixels = resizedImage.NormalizePixelsToTensor(OnnxModel.InputShape, _tensorBufferSize, tensorArrayBuffer);

                    using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, tensorPixels.Buffer, OnnxModel.InputShape);

                    var inputNames = new Dictionary<string, OrtValue>
                    {
                        { OnnxModel.InputName, inputOrtValue }
                    };

                    return _session.Run(_runOptions, inputNames, OnnxModel.OutputNames);
                }
            }
            finally
            {
                customSizeFloatPool.Return(tensorArrayBuffer, true);
            }
        }*/
    }
}
