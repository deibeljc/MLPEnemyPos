// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompressedContent.cs" company="ANN">
//     Copyright (c) ANN. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ANN.Helpers
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    public enum CompressionMethods
    {
        Deflate,

        GZip
    }

    public class CompressedContent : HttpContent
    {
        private readonly CompressionMethods compressionMethod;

        private readonly HttpContent originalContent;

        public CompressedContent(HttpContent content, CompressionMethods compressionMethod)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (!Enum.IsDefined(typeof(CompressionMethods), compressionMethod))
            {
                throw new InvalidEnumArgumentException(
                    nameof(compressionMethod),
                    (int)compressionMethod,
                    typeof(CompressionMethods));
            }

            this.originalContent = content;
            this.compressionMethod = compressionMethod;

            foreach (var header in this.originalContent.Headers)
            {
                this.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            this.Headers.ContentEncoding.Add(this.compressionMethod.ToString().ToLowerInvariant());
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Stream compressedStream = null;

            switch (this.compressionMethod)
            {
                case CompressionMethods.Deflate:
                    compressedStream = new DeflateStream(stream, CompressionMode.Compress, true);
                    break;
                case CompressionMethods.GZip:
                    compressedStream = new GZipStream(stream, CompressionMode.Compress, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return this.originalContent
                       .CopyToAsync(compressedStream)
                       .ContinueWith(
                           tsk => { compressedStream?.Dispose(); });
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}