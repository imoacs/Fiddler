namespace Fiddler
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;

    internal class PipePool
    {
        private static int MSEC_PIPE_POOLED_LIFETIME = 0x1d4c0;
        private readonly Dictionary<string, Queue<ServerPipe>> thePool;

        internal PipePool()
        {
            MSEC_PIPE_POOLED_LIFETIME = FiddlerApplication.Prefs.GetInt32Pref("fiddler.network.timeouts.serverpipe.reuse", 0x1d4c0);
            this.thePool = new Dictionary<string, Queue<ServerPipe>>();
            FiddlerApplication.Janitor.assignWork(new SimpleEventHandler(this.ScavengeCache), 0xea60);
        }

        internal void Clear()
        {
            lock (this.thePool)
            {
                this.thePool.Clear();
            }
        }

        internal int Count()
        {
            return this.thePool.Count;
        }

        internal ServerPipe DequeuePipe(string sPoolKey, int iPID, int HackiForSession)
        {
            Queue<ServerPipe> queue;
            ServerPipe pipe;
            Queue<ServerPipe> queue2;
            if (!CONFIG.bReuseServerSockets)
            {
                return null;
            }
            lock (this.thePool)
            {
                if ((((iPID == 0) || !this.thePool.TryGetValue(string.Format("PID{0}*{1}", iPID, sPoolKey), out queue)) || (queue.Count < 1)) && (!this.thePool.TryGetValue(sPoolKey, out queue) || (queue.Count < 1)))
                {
                    return null;
                }
            }
            Monitor.Enter(queue2 = queue);
            try
            {
                if (queue.Count == 0)
                {
                    return null;
                }
                pipe = queue.Dequeue();
            }
            catch (Exception exception)
            {
                FiddlerApplication.ReportException(exception);
                return null;
            }
            finally
            {
                Monitor.Exit(queue2);
            }
            return pipe;
        }

        internal void EnqueuePipe(ServerPipe oPipe)
        {
            if ((CONFIG.bReuseServerSockets && ((oPipe.sPoolKey != null) && (oPipe.sPoolKey.Length >= 2))) && ((oPipe.ReusePolicy != PipeReusePolicy.MarriedToClientPipe) && (oPipe.ReusePolicy != PipeReusePolicy.NoReuse)))
            {
                Queue<ServerPipe> queue;
                lock (this.thePool)
                {
                    if (!this.thePool.TryGetValue(oPipe.sPoolKey, out queue))
                    {
                        queue = new Queue<ServerPipe>();
                        this.thePool.Add(oPipe.sPoolKey, queue);
                    }
                }
                oPipe.iLastPooled = Environment.TickCount;
                lock (queue)
                {
                    queue.Enqueue(oPipe);
                }
            }
        }

        internal string InspectPool()
        {
            StringBuilder builder = new StringBuilder(0x2000);
            builder.AppendFormat("ServerPipePool\nfiddler.network.timeouts.serverpipe.reuse: {0}ms\nContents\n--------\n", MSEC_PIPE_POOLED_LIFETIME);
            lock (this.thePool)
            {
                foreach (string str in this.thePool.Keys)
                {
                    Queue<ServerPipe> queue = this.thePool[str];
                    builder.AppendFormat("\t[{0}] entries for '{1}'.\n", queue.Count, str);
                    lock (queue)
                    {
                        foreach (ServerPipe pipe in queue)
                        {
                            builder.AppendFormat("\t\t{0}\n", pipe.ToString());
                        }
                        continue;
                    }
                }
            }
            builder.Append("\n--------\n");
            return builder.ToString();
        }

        internal void ScavengeCache()
        {
            if (this.thePool.Count >= 1)
            {
                lock (this.thePool)
                {
                    List<string> list = new List<string>();
                    foreach (KeyValuePair<string, Queue<ServerPipe>> pair in this.thePool)
                    {
                        lock (pair.Value)
                        {
                            while (pair.Value.Count > 0)
                            {
                                if (pair.Value.Peek().iLastPooled >= (Environment.TickCount - MSEC_PIPE_POOLED_LIFETIME))
                                {
                                    break;
                                }
                                pair.Value.Dequeue();
                            }
                            if (pair.Value.Count < 1)
                            {
                                list.Add(pair.Key);
                            }
                            continue;
                        }
                    }
                    foreach (string str in list)
                    {
                        this.thePool.Remove(str);
                    }
                }
            }
        }
    }
}

