using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Docker.Registry.DotNet.Helpers
{
    internal class JsonRequestContent<T> : IRequestContent
    {
        private readonly string _jsonMimeType;

        private readonly T _value;
        private readonly JsonSerializer _serializer;

        public JsonRequestContent(T val, JsonSerializer serializer, string jsonMimeType= "application/json")
        {
            if (EqualityComparer<T>.Default.Equals(val))
            {
                throw new ArgumentNullException(nameof(val));
            }

            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            if(string.IsNullOrEmpty(jsonMimeType))
            {
                throw new ArgumentNullException(nameof(jsonMimeType));
            }

            this._value = val;
            this._serializer = serializer;
            _jsonMimeType = jsonMimeType;
        }

        public HttpContent GetContent()
        {
            var serializedObject = this._serializer.SerializeObject(this._value);
            return new StringContent(serializedObject, Encoding.UTF8, _jsonMimeType);
        }
    }
}
