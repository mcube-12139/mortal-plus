using System.Net;
using System.Text;
using System.Text.Json;

class BaseResult {
    public bool win { get; set; } = true;
}

class MortalPlus {
    const string PATH_NOT_EXIST = "{\"win\":false,\"error\":\"Path not exist\"}";
    const string WRONG_FORMAT = "{\"win\":false,\"error\":\"Wrong format\"}";
    const string EMPTY_WIN = "{\"win\":true}";

    public static HttpListener listener;
    const string url = "http://localhost:12139/";
    public static bool runServer = true;

    public static Dictionary<string, Func<string, string>> pathDict = new() {
        { "/api/recordList", getResponseFunNoParam<RecordListResult>(Record.getList) }
    };

    public static async Task HandleIncomingConnections() {
        // 循环处理请求
        while (runServer) {
            // 阻塞，直到收到请求
            HttpListenerContext ctx = await listener.GetContextAsync();

            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;

            if (req.HttpMethod == "POST") {
                // 处理 POST
                string? path = req.Url?.AbsolutePath;
                Console.WriteLine($"post {path}");

                string responseStr;
                if (path != null && pathDict.ContainsKey(path)) {
                    // 请求路径存在，调用处理函数
                    Func<string, string> handler = pathDict[path];

                    Stream body = req.InputStream;
                    Encoding encoding = req.ContentEncoding;
                    StreamReader reader = new(body, encoding);
                    string bodyStr = reader.ReadToEnd();

                    responseStr = handler(bodyStr);
                } else {
                    // 请求路径不存在，返回错误
                    responseStr = PATH_NOT_EXIST;
                }

                byte[] responseData = Encoding.UTF8.GetBytes(responseStr);
                resp.ContentType = "application/json";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = responseData.LongLength;
                resp.AppendHeader("Access-Control-Allow-Origin", "*");

                await resp.OutputStream.WriteAsync(responseData);
            }
            resp.Close();
        }
    }

    public static Func<string, string> getResponseFun<Param, Result>(Func<Param, Result> handler) {
        return (string requestStr) => {
            string responseStr;

            try {
                Param? param = JsonSerializer.Deserialize<Param>(requestStr);
                if (param != null) {
                    Result result = handler(param);
                    responseStr = JsonSerializer.Serialize(result);
                    goto response;
                } else {
                    goto wrongFormat;
                }
            } catch (Exception _) {
                goto wrongFormat;
            }

        response:
            return responseStr;
        wrongFormat:
            return WRONG_FORMAT;
        };
    }

    public static Func<string, string> getResponseFunNoParam<Result>(Func<Result> handler) {
        return (string _) => {
            string responseStr;

            Result response = handler();
            responseStr = JsonSerializer.Serialize(response);

            return responseStr;
        };
    }

    public static Func<string, string> getResponseFunNoResult<Param>(Action<Param> handler) {
        return (string requestStr) => {
            try {
                Param? bodyJson = JsonSerializer.Deserialize<Param>(requestStr);
                if (bodyJson != null) {
                    handler(bodyJson);
                }
            } catch (Exception _) {
                return WRONG_FORMAT;
            }

            return EMPTY_WIN;
        };
    }

    public static Func<string, string> getResponseFunNoParamNoResult(Action handler) {
        return (string _) => {
            handler();
            return EMPTY_WIN;
        };
    }

    public static void Main(string[] _) {
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine("Listening for connections on {0}", url);

        Task listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();

        listener.Close();
    }
}