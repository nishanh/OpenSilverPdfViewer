using System;
using System.Linq;
using System.Threading.Tasks;

namespace OpenSilverPdfViewer.JSInterop
{
    public static class JSAsyncTaskRunner
    {
        // HACK: Convert JS promise to C# Task - kinda...
        public static async Task RunJavaScriptAsync(string functionName, params object[] args)
        {
            (string jsFunc, object[] arguments) = FormatJSFunctionCall(functionName, args);

            var taskCompletionSource = new TaskCompletionSource<object>();
            arguments[arguments.Length - 1] = (Action)(() => taskCompletionSource.SetResult(null));

            OpenSilver.Interop.ExecuteJavaScript(jsFunc, arguments);
            await taskCompletionSource.Task;
        }
        public static async Task<T> RunJavaScriptAsync<T>(string functionName, params object[] args)
        {
            (string jsFunc, object[] arguments) = FormatJSFunctionCall(functionName, args);

            var taskCompletionSource = new TaskCompletionSource<T>();
            arguments[arguments.Length - 1] = (Action<T>)((result) => taskCompletionSource.SetResult((T)Convert.ChangeType(result, typeof(T))));

            OpenSilver.Interop.ExecuteJavaScript(jsFunc, arguments);
            await taskCompletionSource.Task;
            return taskCompletionSource.Task.Result;
        }
        private static (string, object[]) FormatJSFunctionCall(string functionName, params object[] args)
        {
            var arglist = args
                .Select((arg, i) => $"${i},")
                .Aggregate("", (current, arg) => current + arg);
            arglist += $"${args.Length}";

            var jsFunc = $"{functionName}({arglist})";

            var arguments = new object[args.Length + 1];
            for (int i = 0; i < args.Length; i++)
                arguments[i] = args[i];

            return (jsFunc, arguments);
        }
    }
}
