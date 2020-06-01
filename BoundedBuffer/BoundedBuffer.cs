// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// With thanks to Tom Cargill and
// http://wiki.c2.com/?ExtremeProgrammingChallengeFourteen

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tasks;

namespace BoundedBufferExample
{
    /// <summary>
    /// This is a C# version of Tom's extreme programming challenge.
    /// </summary>
    public class BoundedBuffer
    {
        public static bool PulseAll = false;
        private readonly ICoyoteRuntime Runtime;
        private readonly object SyncObject = new object();
        private readonly object[] Buffer;
        private int PutAt;
        private int TakeAt;
        private int Occupied;

        public BoundedBuffer(int bufferSize, ICoyoteRuntime runtime)
        {
            this.Runtime = runtime;
            this.Buffer = new object[bufferSize];
        }

        public void Put(object x)
        {
            using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
            {
                while (this.Occupied == this.Buffer.Length)
                {
                    monitor.Wait();
                }

                ++this.Occupied;
                this.PutAt %= this.Buffer.Length;
                this.Buffer[this.PutAt++] = x;
                if (PulseAll)
                {
                    monitor.PulseAll();
                }
                else
                {
                    monitor.Pulse();
                }
            }
        }

        public object Take()
        {
            object result = null;
            using (var monitor = SynchronizedBlock.Lock(this.SyncObject))
            {
                while (this.Occupied == 0)
                {
                    monitor.Wait();
                }

                --this.Occupied;
                this.TakeAt %= this.Buffer.Length;
                result = this.Buffer[this.TakeAt++];
                if (PulseAll)
                {
                    monitor.PulseAll();
                }
                else
                {
                    monitor.Pulse();
                }
            }

            return result;
        }
    }
}
