using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FineReaderClient;
using Newtonsoft.Json;
using System.Linq;

namespace B24 {
    public class ProcessRequest {
        public string Uuid { set; get; }
        public string Path { set; get; }
        public string Name { set; get; }
        public string CreateBy { set; get; }
        public string Languages { set; get; }
        public string OutputTypes { set; get; }
        public string ProfileKey { set; get; } = "soc";
        public bool ConvertOnly { set; get; }
    }

    class Program {
        private static async Task<(bool, String)> OcrFile(ProcessRequest request, FileInfo file) {
            try {
                var bytes = File.ReadAllBytes(file.FullName);
                var url = $"http://10.211.55.4:5050/api/fineReaderApi/ocrFile";
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromHours(3);

                using (var formDataContent = new MultipartFormDataContent()) {
                    var json = JsonConvert.SerializeObject(request);
                    formDataContent.Add(new ByteArrayContent(bytes), "file", request.Name);
                    formDataContent.Add(new StringContent(json), "json");
                    var responseMessage = await client.PostAsync(url, formDataContent);
                    var response = await responseMessage.Content.ReadAsStringAsync();
                    var obj = JsonConvert.DeserializeObject<ProcessResult>(response);
                    if (obj.Success) {
                        return (true, obj.Uuid);
                    } else {
                        return (false, obj.Message);
                    }
                }
            } catch (HttpRequestException ex) {
                return (false, ex.Message);
            }
        }

        static async Task Main(string[] args) {

            var path = @"resource";
            var index = 1;
            while (true) {
                var files = new DirectoryInfo(path).GetFiles("*.png").OrderBy(x => x.Length).ToList();
                foreach (var item in files) {

                    var id = Guid.NewGuid().ToString("N");

                    Console.WriteLine($"[{index.ToString("D5")}] process {item.FullName}");

                    var uuid = Guid.NewGuid().ToString("N");
                    var request = new ProcessRequest {
                        Uuid = id,
                        Path = "/a/b/c",
                        Name = $"{id}-{item.Name}",
                        CreateBy = "example",
                        Languages = "English,Thai",
                        ProfileKey = "soc",
                        ConvertOnly = true,
                        OutputTypes = "Pdf",
                    };
                    var result = await OcrFile(request, item);

                    if (!result.Item1) {
                        Console.WriteLine(" > fail - {0} {1}", item.FullName, result.Item2);
                    } else {
                        Console.WriteLine(" > success - {0} {1}", item.FullName, result.Item2);
                    }

                    index++;
                }
            }
        }
    }
}