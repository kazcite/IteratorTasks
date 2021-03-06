﻿using System;
using System.Linq;

namespace IteratorTasks
{
    public static partial class TaskEx
    {
        public static Task<T> First<T>(params Task<T>[] tasks) { return First(null, tasks); }

        public static Task<T> First<T>(params AsyncFunc<T>[] tasks)
        {
            var cts = new CancellationTokenSource();
            var created = tasks.Select(x => x(cts.Token)).ToArray();
            return First(cts, created);
        }

        /// <summary>
        /// 複数のタスクのうち、最初に終わったものの値を返す。
        /// 残りのタスクは内部でキャンセルする。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cts"></param>
        /// <param name="tasks">最初の1つを待ちたいタスク一覧。</param>
        /// <returns>最初の1つだけ終わったら完了になるタスク。</returns>
        public static Task<T> First<T>(CancellationTokenSource cts, params Task<T>[] tasks)
        {
            return First(null, cts, tasks);
        }

        public static Task<T> First<T>(CancellationToken ct, params Task<T>[] tasks)
        {
            var cts = ct.ToCancellationTokenSourceOneWay();
            return First(cts, tasks);
        }

        public static Task<T> First<T>(TaskScheduler scheduler, CancellationTokenSource cts, params Task<T>[] tasks)
        {
            if (tasks.Length == 0)
                throw new ArgumentException("tasks must contain at least one task", "tasks");

            var tcs = new TaskCompletionSource<T>(scheduler);
            // Task.Firstに終わったタスクを渡すとtcs = null が先に呼ばれてnullぽ出るのでここで受けておく
            var task0 = tcs.Task;

            if (cts != null)
                cts.Token.Register(() =>
                {
                    if (tcs != null)
                    {
                        tcs.SetCanceled();
                        tcs = null;
                    }
                });

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("task must not null", "tasks");

                task.ContinueWith(t =>
                {
                    if (tcs != null)
                    {
                        tcs.Propagate(t);
                        tcs = null;
                        if (cts != null)
                            cts.Cancel();
                    }
                });
            }

            return task0;
        }

        public static Task First(params Task[] tasks) { return First(null, tasks); }

        public static Task First(params AsyncAction[] tasks)
        {
            var cts = new CancellationTokenSource();
            var created = tasks.Select(x => x(cts.Token)).ToArray();
            return First(cts, created);
        }

        public static Task First(TaskScheduler scheduler, params AsyncAction[] tasks)
        {
            var cts = new CancellationTokenSource();
            var created = tasks.Select(x => x(cts.Token)).ToArray();
            return First(scheduler, cts, created);
        }

        public static Task First(CancellationTokenSource cts, params Task[] tasks)
        {
            return First(null, cts, tasks);
        }

        public static Task First(CancellationToken ct, params Task[] tasks)
        {
            var cts = ct.ToCancellationTokenSourceOneWay();
            return First(cts, tasks);
        }

        /// <summary>
        /// 複数のタスクのうち、最初に終わったものの値を返す。
        /// 残りのタスクは内部でキャンセルする。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tasks">最初の1つを待ちたいタスク一覧。</param>
        /// <param name="cts"></param>
        /// <returns>最初の1つだけ終わったら完了になるタスク。</returns>
        // ※保留 <param name="onComplete">最初の1つのタスクが終了時に呼ばれる。Task.First().ContinueWith(onComplete) すると呼ばれるフレームが1フレーム遅れるけども、これならたぶん即呼ばれる。</param>
        public static Task First(/* Action onComplete, */ TaskScheduler scheduler, CancellationTokenSource cts, params Task[] tasks)
        {
            if (tasks.Length == 0)
                throw new ArgumentException("tasks must contain at least one task", "tasks");

            var tcs = new TaskCompletionSource<object>(scheduler);
            // Task.Firstに終わったタスクを渡すとtcs = null が先に呼ばれてnullぽ出るのでここで受けておく
            var task0 = tcs.Task;

            if (cts != null)
                cts.Token.Register(() =>
                {
                    //if (onComplete != null)
                    //    onComplete();

                    if (tcs != null)
                    {
                        tcs.SetCanceled();
                        tcs = null;
                    }
                });

            foreach (var task in tasks)
            {
                if (task == null)
                    throw new ArgumentException("task must not null", "tasks");

                task.ContinueWith(t =>
                {
                    if (tcs != null)
                    {
                        tcs.Propagate(t);
                        tcs = null;
                        if (cts != null)
                            cts.Cancel();
                    }
                });
            }

            return task0;
        }

        internal static void Propagate<T>(this TaskCompletionSource<T> tcs, Task task)
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                var tt = task as Task<T>;
                tcs.SetResult(tt == null ? default(T) : tt.Result);
            }
            else if (task.IsFaulted)
            {
                tcs.SetException(task.Exception);
            }
            else
            {
                tcs.SetCanceled();
            }
        }
    }
}
