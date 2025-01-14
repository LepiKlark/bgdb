﻿using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Test.Common;
using VideoProcessing;

namespace VideoProcessingTests
{
    public class FFMpegVideoSplitTests
    {
        private static string GetExampleVideoPath()
        {
            FileInfo dataRoot = new FileInfo(typeof(FFmpegProbeTests).Assembly.Location);
            string assemblyFolderPath = dataRoot.Directory.FullName;
            return Path.Combine(assemblyFolderPath, "examples/sample_960x400_ocean_with_audio.mkv");
        }

        private static string GetTempFolderPath()
        {
            FileInfo dataRoot = new FileInfo(typeof(FFmpegProbeTests).Assembly.Location);
            string assemblyFolderPath = dataRoot.Directory.FullName;

            string path = Path.Combine(assemblyFolderPath, "temp");
            Directory.CreateDirectory(path);
            return path;
        }

        [Test]
        public async Task FFmpegVideoChunkerTest()
        {
            var videoChunker = new FfmpegVideoChunker(GetTempFolderPath(), new NoOpLogging());

            string[] chunkPaths = await videoChunker.Execute(GetExampleVideoPath(), TimeSpan.FromSeconds(10), new DummyTran(), CancellationToken.None);

            Assert.AreEqual(5, chunkPaths.Length);
        }

        [Test]
        public async Task TaskFFmpegChunkPlusProbeTest()
        {

            var videoChunker = new FfmpegVideoChunker(GetTempFolderPath(), new NoOpLogging());
            var probe = new FfmpegProbeWrapper(new NoOpLogging());

            string[] chunkPaths = await videoChunker.Execute(GetExampleVideoPath(), TimeSpan.FromSeconds(10), new DummyTran(), CancellationToken.None);

            Assert.AreEqual(5, chunkPaths.Length);
            foreach (string chunkPath in chunkPaths)
            {
                var probeOutput = await probe.Execute(chunkPath, CancellationToken.None);
            }
        }
    }
}
