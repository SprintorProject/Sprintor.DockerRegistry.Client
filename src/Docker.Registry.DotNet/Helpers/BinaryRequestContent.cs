using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Docker.Registry.DotNet.Helpers
{
    internal class BinaryRequestContent : IRequestContent
    {
        private readonly Stream _stream;
        private readonly string _mimeType;
        private readonly long _begin;
        private readonly long _end;
        private readonly long _contentLength;

        public BinaryRequestContent(Stream stream, string mimeType)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (string.IsNullOrEmpty(mimeType))
            {
                throw new ArgumentNullException(nameof(mimeType));
            }

            this._stream = stream;
            this._mimeType = mimeType;
        }

        public BinaryRequestContent(Stream stream, string mimeType,long begin,long end,long contentLength) :this(stream,mimeType)
        {
            _begin = begin;
            _end = end;
            _contentLength = contentLength;
          
        }

        public BinaryRequestContent(Stream stream, string mimeType,long contentLength) : this(stream, mimeType)
        {
            _contentLength = contentLength;
        }

        public HttpContent GetContent()
        {
            this._stream.Position = 0;
            var data = new StreamContent(this._stream);
            data.Headers.ContentType = new MediaTypeHeaderValue(this._mimeType);

            if(_begin>0||_end>0)
            {
                data.Headers.ContentRange = new ContentRangeHeaderValue(_begin, _end, _contentLength);
                data.Headers.ContentLength = _stream.Length;
            }
            else if (_contentLength > 0)
            {
                data.Headers.ContentRange = new ContentRangeHeaderValue(0, _contentLength - 1, _contentLength);
                data.Headers.ContentLength = 0;
            }

            return data;
        }
    }
}
