// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// With thanks to Tom Cargill and
// http://wiki.c2.com/?ExtremeProgrammingChallengeFourteen
using System.Threading;

namespace BoundedBufferExample
{
    public class OriginalBoundedBuffer
    {
        public OriginalBoundedBuffer(int bufferSize)
        {
            this.Buffer = new object[bufferSize];
        }

        public void Put(object x)
        {
            lock (this.SyncObject)
            {
                while (this.Occupied == this.Buffer.Length)
                {
                    Monitor.Wait(this.SyncObject);
                }

                ++this.Occupied;
                this.PutAt %= this.Buffer.Length;
                this.Buffer[this.PutAt++] = x;
                Monitor.Pulse(this.SyncObject);
            }
        }

        public object Take()
        {
            object result = null;
            lock (this.SyncObject)
            {
                while (this.Occupied == 0)
                {
                    Monitor.Wait(this.SyncObject);
                }

                --this.Occupied;
                this.TakeAt %= this.Buffer.Length;
                result = this.Buffer[this.TakeAt++];
                Monitor.Pulse(this.SyncObject);
            }

            return result;
        }

        private readonly object SyncObject = new object();
        private readonly object[] Buffer;
        private int PutAt;
        private int TakeAt;
        private int Occupied;
    }
}
