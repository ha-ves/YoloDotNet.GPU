using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoloDotNet.Core;
using YoloDotNet.GPU.Core;
using YoloDotNet.Models;
using YoloDotNet.Modules.Interfaces;
using YoloDotNet.Modules.V8;

namespace YoloDotNet.GPU.Modules.V8
{
    public class ObjectDetectionDirectXModuleV8(YoloCoreDirectX yoloCore) : IObjectDetectionModule
    {
        public OnnxModel OnnxModel => yoloCore.OnnxModel;

        public event EventHandler VideoStatusEvent;
        public event EventHandler VideoProgressEvent;
        public event EventHandler VideoCompleteEvent;

        public void Dispose()
        {
            yoloCore?.Dispose();
        }

        public List<ObjectDetection> ProcessImage(SKImage image, double confidence, double pixelConfidence, double iou)
        {
            yoloCore
        }

        public Dictionary<int, List<ObjectDetection>> ProcessVideo(VideoOptions options, double confidence, double pixelConfidence, double iou)
        {
            throw new NotImplementedException();
        }
    }
}
