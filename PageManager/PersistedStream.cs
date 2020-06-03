﻿using System;
using System.IO;

namespace PageManager
{
    public interface IPersistedStream
    {
        public string GetFileName();
        public ulong CurrentFileSize();
        public void Grow(ulong newSize);
        public void Shrink(ulong newSize);
        public void SeekAndAccess(ulong position, Action<BinaryWriter> writer);
    }

    public class PersistedStream : IPersistedStream
    {
        private string fileName;

        private FileStream fileStream;
        private BinaryWriter binaryWriter;

        public PersistedStream(ulong startFileSize, string fileName, bool createNew)
        {
            if (File.Exists(fileName) || !createNew)
            {
                this.fileStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            }
            else
            {
                this.fileStream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
                this.fileStream.SetLength((long)startFileSize);
            }

            this.binaryWriter = new BinaryWriter(this.fileStream);

            this.fileName = fileName;
        }

        public ulong CurrentFileSize() => (ulong)this.fileStream.Length;

        public string GetFileName() => this.fileName;

        public void Grow(ulong newSize)
        {
            if ((ulong)this.fileStream.Length > newSize)
            {
                throw new ArgumentException();
            }

            this.fileStream.SetLength((long)newSize);
        }

        public void Shrink(ulong newSize)
        {
            if ((ulong)this.fileStream.Length < newSize)
            {
                throw new ArgumentException();
            }

            this.fileStream.SetLength((long)newSize);
        }

        public void SeekAndAccess(ulong position, Action<BinaryWriter> writer)
        {
            this.fileStream.Seek((long)position, SeekOrigin.Begin);
            writer(this.binaryWriter);
            this.fileStream.Flush();
        }
    }
}