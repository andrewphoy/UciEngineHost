using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UciEngineHost.Helpers {
    public partial class ExtensionMethods {

        public static Action<T> Debounce<T>(this Action<T> action, int milliseconds = 500) {
            CancellationTokenSource? cts = null;
            return arg => {
                cts?.Cancel();
                cts = new CancellationTokenSource();

                Task.Delay(milliseconds, cts.Token)
                    .ContinueWith(t => {
                        if (t.IsCompletedSuccessfully) {
                            action(arg);
                        }
                    }, TaskScheduler.Default);
            };
        }

        public static Action<T> Throttle<T>(this Action<T> action, int milliseconds = 500) {
            using (var semaphore = new SemaphoreSlim(0, 1)) {

                return arg => {
                    action(arg);


                };
                
            }
        }

    }
}
