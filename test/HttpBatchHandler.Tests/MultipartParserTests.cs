﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HttpBatchHandler.Multipart;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Xunit;

namespace HttpBatchHandler.Tests
{
    // https://blogs.msdn.microsoft.com/webdev/2013/11/01/introducing-batch-support-in-web-api-and-web-api-odata/
    public class MultipartParserTests
    {
        [Theory]
        [InlineData(null, "MultipartRequest.txt", true)]
        [InlineData("/path/base", "MultipartRequestPathBase.txt", true)]
        [InlineData(null, "MultipartRequest.txt", false)]
        [InlineData("/path/base", "MultipartRequestPathBase.txt", false)]
        public async Task Parse(string path, string file, bool isHttps)
        {
            var reader = new MultipartReader("batch_357647d1-a6b5-4e6a-aa73-edfc88d8866e",
                TestUtilities.GetNormalizedContentStream(file));
            var sections = new List<HttpApplicationRequestSection>();

            HttpApplicationRequestSection section;
            while ((section = await reader.ReadNextHttpApplicationRequestSectionAsync(path, isHttps).ConfigureAwait(false)) !=
                   null)
            {
                sections.Add(section);
            }

            string scheme = isHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp;
            Assert.Equal(4, sections.Count);
            Assert.Collection(sections,
                x => InspectFirstRequest(x, path, scheme),
                x => InspectSecondRequest(x, path, scheme),
                x => InspectThirdRequest(x, path, scheme),
                x => InspectFourthRequest(x, path, scheme));
        }

        private void InspectFirstRequest(HttpApplicationRequestSection obj, string pathBase, string scheme)
        {
            Assert.Equal("GET", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers", obj.RequestFeature.Path);
            Assert.Equal(pathBase, obj.RequestFeature.PathBase);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal(scheme,  obj.RequestFeature.Scheme);
            Assert.Equal("?Query=Parts", obj.RequestFeature.QueryString);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
        }

        private void InspectFourthRequest(HttpApplicationRequestSection obj, string pathBase, string scheme)
        {
            Assert.Equal("DELETE", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers/2", obj.RequestFeature.Path);
            Assert.Equal(pathBase, obj.RequestFeature.PathBase);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal(scheme, obj.RequestFeature.Scheme);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
        }

        private void InspectSecondRequest(HttpApplicationRequestSection obj, string pathBase, string scheme)
        {
            Assert.Equal("POST", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers", obj.RequestFeature.Path);
            Assert.Equal(pathBase, obj.RequestFeature.PathBase);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal(scheme, obj.RequestFeature.Scheme);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
            var serializer = JsonSerializer.Create();
            dynamic deserialized =
                serializer.Deserialize(new JsonTextReader(new StreamReader(obj.RequestFeature.Body)));
            Assert.Equal("129", deserialized.Id.ToString());
            Assert.Equal("Name4752cbf0-e365-43c3-aa8d-1bbc8429dbf8", deserialized.Name.ToString());
        }


        private void InspectThirdRequest(HttpApplicationRequestSection obj, string pathBase, string scheme)
        {
            Assert.Equal("PUT", obj.RequestFeature.Method);
            Assert.Equal("/api/WebCustomers/1", obj.RequestFeature.Path);
            Assert.Equal(pathBase, obj.RequestFeature.PathBase);
            Assert.Equal("HTTP/1.1", obj.RequestFeature.Protocol);
            Assert.Equal(scheme, obj.RequestFeature.Scheme);
            Assert.Equal("localhost:12345", obj.RequestFeature.Headers[HeaderNames.Host]);
            var serializer = JsonSerializer.Create();
            dynamic deserialized =
                serializer.Deserialize(new JsonTextReader(new StreamReader(obj.RequestFeature.Body)));
            Assert.Equal("1", deserialized.Id.ToString());
            Assert.Equal("Peter", deserialized.Name.ToString());
        }
    }
}