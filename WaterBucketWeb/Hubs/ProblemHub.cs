using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using ZeroMQ;
using WaterBucket.Domain;
using Utils;
using System.Threading;
using WaterBucketWeb.Models;

namespace WaterBucketWeb.Hubs
{
    public class WorkerException : Exception
    {
        public WorkerException(string message) : base(message)
        {
        }
    }

    public class ProblemHub : Hub
    {
        // try not to have magic strings
        private const string WATER_BUCKET_WORKER = "WaterBucketWorker";
        private const string ZMQ_PUBLISHER = "ZmqPublisher";
        private const string CLOUD_Q_FOR_WORK = "problemstosolve";

        private List<string> _publisherAddresses = new List<string>();
        private IList<string> PublisherAddresses { get { return _publisherAddresses; } }
        private bool CanUseWorker { get; set; }
        private string NoWorkerReason { get; set; }
        private int BucketMax { get; set; }
        private int BucketMin { get; set; }
        private int ActionBindingThreshold { get; set; }
        private bool YieldOnAction { get; set; }
        private bool ObservableOnZmq { get; set; }
        private bool ObservableOnJavaScript { get; set; }
        private bool BroadcastAll { get; set; }

        public ProblemHub()
            : base()
        {
            // Get settings from the RoleEnvironment that can be configured on the fly
            // because SignalR constructs a new Hub for every request, this will get the latest values
            // TODO: Refactor into ConfigSettings static class with const definitions
            BucketMax = RoleEnvironmentExt.GetRoleConfigSetting("Bucket.Size.Max", 5000);
            BucketMin = RoleEnvironmentExt.GetRoleConfigSetting("Bucket.Size.Min", 0);
            ActionBindingThreshold = RoleEnvironmentExt.GetRoleConfigSetting("Action.Binding.Threshold", 500);
            YieldOnAction = RoleEnvironmentExt.GetRoleConfigSetting("Action.On.Yield", false);
            ObservableOnZmq = RoleEnvironmentExt.GetRoleConfigSetting("Zmq.UseObservable", false);
            ObservableOnJavaScript = RoleEnvironmentExt.GetRoleConfigSetting("JavaScript.UseObservable", false);
            BroadcastAll = RoleEnvironmentExt.GetRoleConfigSetting("Client.BroadcastAll", false);

            // THIS IS A FAIL - during testing/execution using an instance-based PublisherAddresses list during 
            //  a call did not result in child Task threads getting the proper addresses for subscriptions
            //  CURRENT implementation uses a GetPublisherAddresses call in each child Task thread
            // Okay to do here since SignalR calls constructor on every request - would rather it not
            //InitPublisherAddresses();
        }

        //private void InitPublisherAddresses()
        //{
        //    if (!RoleEnvironment.Roles.ContainsKey(WATER_BUCKET_WORKER) || !RoleEnvironment.Roles[WATER_BUCKET_WORKER].Instances.Any())
        //    {
        //        CanUseWorker = false;
        //        NoWorkerReason = "Could not get any '" + WATER_BUCKET_WORKER + "' WorkerRole instances";
        //    }
        //    else
        //    {
        //        foreach (var worker in RoleEnvironment.Roles[WATER_BUCKET_WORKER].Instances)
        //        {
        //            var zmqPubEP = worker.InstanceEndpoints.ContainsKey(ZMQ_PUBLISHER) ? worker.InstanceEndpoints[ZMQ_PUBLISHER] : null;
        //            if ((zmqPubEP != null) && (zmqPubEP.IPEndpoint != null))
        //            {
        //                string pubAddr = "tcp://" + zmqPubEP.IPEndpoint.Address.ToString() + ":" + zmqPubEP.IPEndpoint.Port;
        //                _publisherAddresses.Add(pubAddr);
        //                Clients.All.foundPublisherEP(pubAddr);
        //                CanUseWorker = true;
        //            }
        //        }
        //        if (!CanUseWorker)
        //            NoWorkerReason = "Could not find '" + ZMQ_PUBLISHER + "' Endpoint for Workers";
        //    }
        //}

        /// <summary>
        /// Get the PublisherAddresses used by the WorkerRoles in this Environment
        /// </summary>
        /// <returns>Enumerated list of PublisherAddresses in TCP for All current WorkerRole Publish Endpoints</returns>
        private IEnumerable<string> GetPublisherAddresses()
        {
            if (!RoleEnvironment.Roles.ContainsKey(WATER_BUCKET_WORKER) || !RoleEnvironment.Roles[WATER_BUCKET_WORKER].Instances.Any())
            {
                throw new WorkerException("No instances of worker role '" + WATER_BUCKET_WORKER + "' available");
            }
            foreach (var worker in RoleEnvironment.Roles[WATER_BUCKET_WORKER].Instances)
            {
                var zmqPubEP = worker.InstanceEndpoints.ContainsKey(ZMQ_PUBLISHER) ? worker.InstanceEndpoints[ZMQ_PUBLISHER] : null;
                if ((zmqPubEP != null) && (zmqPubEP.IPEndpoint != null))
                {
                    yield return string.Format("tcp://{0}:{1}", zmqPubEP.IPEndpoint.Address.ToString(), zmqPubEP.IPEndpoint.Port);
                }
            }
        }

        /// <summary>
        /// Get a message from the Client to Solve a given problem using the given work order
        /// </summary>
        /// <param name="problem"><see cref="WaterBucket.Domain.Problem"/> defining the Bucket and Goal Parameters to use in the calculation</param>
        /// <param name="work">WorkOrder parameters used during execution of the solution</param>
        public void Solve(ProblemVM problem, WorkOrder work)//, bool useWorker = false, int delayStart = -1, int workDelay = -1)
        {
            if (BroadcastAll)
            {
                Clients.All.problemSubmitted(ObservableOnJavaScript);
                Clients.Others.submission(problem, work.UseWorker);
            }
            else
            {
                Clients.Caller.problemSubmitted(ObservableOnJavaScript);
            }
            // Test Bucket Ranges here instead of at the browser so that updates made on the back end for thresholds are checked without requiring browser refresh
            bool end = false;
            int bigBucket = Math.Max(problem.FirstBucketCapacity, problem.SecondBucketCapacity);
            if (bigBucket > BucketMax)
            {
                if (BroadcastAll)
                {
                    Clients.All.outOfRange(bigBucket, true);
                }
                else
                {
                    Clients.Caller.outOfRange(bigBucket, true);
                }
                end = true;
            }
            int smallBucket = Math.Min(problem.FirstBucketCapacity, problem.SecondBucketCapacity);
            if (smallBucket < BucketMin)
            {
                if (BroadcastAll)
                {
                    Clients.All.outOfRange(smallBucket, false);
                }
                else
                {
                    Clients.Caller.outOfRange(smallBucket, false);
                }
                end = true;
            }
            if (end)
                return;
            // Check to see if the Problem exists in the Cache
            Problem problemo = HttpContext.Current.Cache[problem.CacheKey] as Problem;
            // If not, create a new Problem based on the parameters to solve and add it to the Cache
            if (problemo == null)
            {
                problemo = new Problem(problem.FirstBucketCapacity, problem.SecondBucketCapacity, problem.GoalWaterVolume);
                HttpContext.Current.Cache.Insert(problem.CacheKey, problemo);
            }
            // Notify Client(s) that the Problem is being started
            // ActionBindingThreshold tells the client browser whether to use knockout.js observableArray binding for displaying action step updates or
            // plain old DOM manipulation - ko.observableArray creates a big performance hit
            bool bindActions = bigBucket <= ActionBindingThreshold;
            if (BroadcastAll)
            {
                Clients.All.startedProblem(problem, bindActions && !work.UseWorker);
            }
            else
            {
                Clients.Caller.startedProblem(problem, bindActions && !work.UseWorker);
            }

            // Check to see if it is a solvable Problem
            if (!problemo.IsSolvable)
            {
                // If not a solvable problem then notify the clients and do not try to solve it
                if (BroadcastAll)
                {
                    Clients.All.notSolvable(problem);
                }
                else
                {
                    Clients.Caller.notSolvable(problem);
                }
            }
            else
            {
                // Try-Catch didn't work here to solve the problem I was having
                //try
                //{
                    var problemTasks = work.UseWorker ? SolveProblemWithWorkerAsync(problemo, work) : SolveAProblemAsync(problemo, work);
                //    problemTasks.ContinueWith(t =>
                //        {
                //            if (t.IsFaulted)
                //                Clients.All.errorInSolve(t.Exception);
                //        });
                //}
                //catch (Exception ex)
                //{
                //    Clients.All.errorInSolve(ex);
                //}
            }
        }

        /// <summary>
        /// Solve the Problem locally in the WebRole/Hub asynchronously using Rx Observable sequences to capture and signal updates to the Client(s)
        /// </summary>
        /// <param name="problemToSolve"><see cref="WaterBucket.Domain.Problem"/> to be solved and calculate the results</param>
        /// <param name="work">The <see cref="WaterBucketWeb.Models.WorkOrder"/> passed from the Client determining how to perform the work of solving the <see cref="WaterBucket.Domain.Problem"/></param>
        /// <returns>Task resulting in <see cref="WaterBucket.Domain.SolutionResult"/>s for the problem for each <see cref="WaterBucket.Domain.ISolutionStrategy"/> employed</returns>
        protected async Task<SolutionResult[]> SolveAProblemAsync(Problem problemToSolve, WorkOrder work)//, int delayStart, int workDelay)
        {
            if (work.StartDelay > 0)
                Thread.Sleep(work.StartDelay);

            ISolutionStrategy smallToBig, bigToSmall;
            smallToBig = new SmallToBigSingleBucketSolutionStrategy(problemToSolve);
            bigToSmall = new BigToSmallSingleBucketSolutionStrategy(problemToSolve);
            var startSmall = from step in problemToSolve.Solve(smallToBig)
                             select step;
            //startSmall.ToObservable().Subscribe(ProblemActionStep, ProblemActionError, ProblemSolutionCompleted);
            // Example of doing all of the code on a single line
            //problemToSolve.Solve(new SmallToBigSingleBucketSolutionStrategy(problemToSolve)).ToObservable().Subscribe(ProblemActionStep, ProblemActionError, () => ProblemSolutionCompleted("Small to Big"));
            // Capture as primitive value type rather than rely on reference type for use in Tasks - was burned on this with the PublisherAddresses list
            bool yieldOnAction = work.YieldWeb;
            // Set up a Task to run the Observable in but don't wait on it yet
            var smallToBigTask = Task.Run(() => (work.WorkDelay > 0 ? startSmall.ToObservable().Do(_ => Thread.Sleep(work.WorkDelay)) : startSmall.ToObservable())
                .Subscribe(step => ProblemActionStep(step, yieldOnAction), ProblemActionError, () => ProblemSolutionCompleted("Small to Big")))
                .ContinueWith(t => 
                    {
                        if (t.IsCanceled)
                        {
                        }
                        else if (t.IsFaulted)
                        {
                        }
                        else if (t.IsCompleted)
                        {
                            t.Result.Dispose();
                            SignalSolution(smallToBig.StrategyName, smallToBig.Result);
                            return smallToBig.Result;
                        }
                        return null;
                    });
            var startBig = from step in problemToSolve.Solve(bigToSmall)
                            select step;
            //var startBig = (from step in problemToSolve.Solve(bigToSmall)
            //               select step).ToObservable();
            //if (workDelay > 0)
            //{
            //    var timer = Observable.Interval(TimeSpan.FromMilliseconds(workDelay));
            //    startBig = startBig.Zip(timer, (bs, _) => bs);
            //}
            // Set up a Task to run the Observable in but don't wait on it yet
            var bigToSmallTask = Task.Run(() => (work.WorkDelay > 0 ? startBig.ToObservable().Do(_ => Thread.Sleep(work.WorkDelay)) : startBig.ToObservable())
                .Subscribe(step => ProblemActionStep(step, yieldOnAction), ProblemActionError, () => ProblemSolutionCompleted("Big to Small")))
                .ContinueWith(t =>
                    {
                        if (t.IsCanceled)
                        {
                        }
                        else if (t.IsFaulted)
                        {
                        }
                        else if (t.IsCompleted)
                        {
                            t.Result.Dispose();
                            SignalSolution(bigToSmall.StrategyName, bigToSmall.Result);
                            return smallToBig.Result;
                        }
                        return null;
                    });
            // Wait for the Observable execution tasks to complete before finishing
            return await Task.WhenAll(smallToBigTask, bigToSmallTask);
        }

        /// <summary>
        /// Get Updates on the work being completed from the WorkerRole that is working on this problem using this strategy as they are happening
        /// </summary>
        /// <param name="ctx"><see cref="ZeroMQ.ZmqContext"/> on which to open ZmqSocket.SUB connection to the Publisher WorkerRole doing the work</param>
        /// <param name="strategy">The <see cref="WaterBucket.Domain.ISolutionStrategy"/> being employed to solve the problem on which you want to receive update messages</param>
        /// <param name="onCompletion">What to execute when the Worker has sent a completion of the problem message</param>
        /// <returns>A sequence of <see cref="WaterBucket.Domain.BucketActionStep"/>s every time one is received on the <see cref="ZeroMQ.ZmqSocket"/> from the WorkerRole</returns>
        /// <seealso cref="ZeroMQ.ZmqSocket"/>
        protected IEnumerable<BucketActionStep> GetWorkerUpdates(ZmqContext ctx, ISolutionStrategy strategy, Action<SolutionResult> onCompletion = null)
        {
            if (ctx == null)
                throw new ArgumentNullException("ctx");
            if (strategy == null)
                throw new ArgumentNullException("strategy");
            var publisherAddresses = GetPublisherAddresses();
            if (!publisherAddresses.Any())
            {
                throw new WorkerException("Could not get any publisher endpoints to receive messages from the Worker");
            }

            // Sockets are NOT Thread safe so they must be created in the Thread in which they will be used
            ZmqSocket subscriber = ctx.CreateSocket(SocketType.SUB);
            subscriber.Subscribe(Encoding.UTF8.GetBytes(strategy.Signature));
            foreach (var pubAddr in publisherAddresses)
            {
                subscriber.Connect(pubAddr);
            }
            ProblemUpdate update;
            do
            {
                update = null;
                // This is the blocking version of Receiving from a ZMQ Socket - this is okay since we will run this call from a child Task or wrap in Rx
                ZmqMessage msg = subscriber.ReceiveMessage();
                // Skip the first Frame since it is the Subscription Prefix
                var msgSig = msg.Unwrap();
                // Get the UpdateType from the Message
                byte[] updateTypeData = msg.Unwrap().Buffer;
                ProblemUpdateType updateType = (ProblemUpdateType)updateTypeData[0];
                //update = new ProblemUpdate(updateType, msg.Last.Buffer);
                // Find the data demarcation frame
                var updateFrames = msg.SkipWhile(f => (f.BufferSize != 1) && (f.Buffer[0] != 0x7f));//.ToList();
                // Skip the data demarcation frame when creating the ProblemUpdate
                update = new ProblemUpdate(updateType, updateFrames.Skip(1).ToList());
                if (update.IsAction || update.IsInitial)
                    // Return BucketActionSteps to the awaiting caller
                    yield return update.IntoFrameableType<BucketActionStep>().UpdateState;
            } while ((update != null) && !(update.IsCompletion || update.IsError));

            if (update != null)
            {
                // Signal Completion
                if ((update.IsCompletion) && (onCompletion != null))
                    onCompletion(update.IntoFrameableType<SolutionResult>().UpdateState);

                // Throw an Exception to the caller if the WorkerRole experienced an Exception
                if (update.IsError)
                {
                    throw update.GetException<Exception>();
                }
            }
        }

        // Deprecated message for SignedUpdates with the theory of having a single ZmqSocket subscribe to all
        //  messages for a problem and then separate/parse them by which strategy the message is an update for 
        //  and pass that appropriately to the client(s)
        //protected IEnumerable<SignedProblemUpdate> GetProblemUpdates(ZmqSocket subscriber, string signature)
        //{
        //    if (subscriber == null)
        //        throw new ArgumentNullException("subscriber");

        //    SignedProblemUpdate update;
        //    do
        //    {
        //        update = null;
        //        // This is the blocking version of Receiving from a ZMQ Socket - this is okay since we will wrap this call in Rx
        //        ZmqMessage msg = subscriber.ReceiveMessage();
        //        // To decouple ProblemUpdate from the Transport Medium, only binary data is passed using byte[] instead of a ZeroMQ.Frame
        //        update = new SignedProblemUpdate(msg.Select(f => f.Buffer).ToArray());
        //        if (!signature.Equals(update.Signature))
        //            continue;

        //        if (update.IsAction || update.IsInitial)
        //            yield return update;
        //    } while ((update != null) && !(update.IsCompletion || update.IsError));

        //    if (update != null)
        //    {
        //        if (update.IsCompletion)
        //        {
        //            yield return update;
        //        }
        //        else if (update.IsError)
        //        {
        //            throw update.GetException<Exception>();
        //        }
        //    }

        //    yield break;
        //}

        /// <summary>
        /// Solve the Problem using a WorkerRole hosted in Azure PaaS Cloud asynchronously by queuing the problem in an Azure Storage CloudQueue and 
        /// listening on ZmqSockets for update messages from the Worker doing the work as the work is being done.
        /// <para>Based on a setting by the Client in the WorkOrder optionally use an Rx Observable sequences over the message stream to signal 
        /// updates to the Client(s)</para>
        /// </summary>
        /// <param name="problemToSolve"><see cref="WaterBucket.Domain.Problem"/> to be solved and calculate the results</param>
        /// <param name="work">The <see cref="WaterBucketWeb.Models.WorkOrder"/> passed from the Client determining how to perform the work of solving the <see cref="WaterBucket.Domain.Problem"/></param>
        /// <returns>Task that can be awaited on for work being completed by the Worker</returns>
        /// <seealso cref="Microsoft.WindowsAzure.Storage.Queue.CloudQueue"/>
        /// <seealso cref="ZeroMQ"/>
        protected async Task SolveProblemWithWorkerAsync(Problem problemToSolve, WorkOrder work)//, int delayStart, int workDelay)
        {
            // Calling this here will ensure that it is trying to find workers anytime it needs them
            // rather than prefetching PublisherAddresses, finding out they're not initialized and 
            // never being able to use workers after that
            // - fortunately or unfortunately, it doesn't matter in SignalR since Hubs are reconstructed 
            //   for every call to the Hub so the result if called in the ctor wouldn't be cached accross calls
            //InitPublisherAddresses();
            //if (!CanUseWorker)
            //{
            //    Clients.All.addMsg("Cannot use Worker for calculations, reason: " + NoWorkerReason);
            //    return;
            //}

            // Check whether the CloudQueue is for the staging or production environment, which use separate Queues in order to provide fully isolated environments
            bool isStaging = RoleEnvironmentExt.GetRoleConfigSetting("UseStaging", false);
            // Get the CloudQueue on which we want to put messages for the WorkerRoles to get their work
            string storageAccountConStr = !isStaging ? RoleEnvironment.GetConfigurationSettingValue("StorageAccount") : RoleEnvironment.GetConfigurationSettingValue("StagingStorageAccount");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConStr);
            CloudQueueClient qClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = qClient.GetQueueReference(CLOUD_Q_FOR_WORK);
            queue.CreateIfNotExists();

            try
            {
                ZmqContext ctx = ZmqContext.Create();
                //ZmqSocket socket = ctx.CreateSocket(SocketType.SUB);
                //socket.Connect(publishAddress);
                //List<ISolutionStrategy> strategies = new List<ISolutionStrategy>();
                //strategies.Add(new BigToSmallSingleBucketSolutionStrategy(problemToSolve));
                //strategies.Add(new SmallToBigSingleBucketSolutionStrategy(problemToSolve));
                //IObservable<SignedProblemUpdate> combinedUpdates = null;
                //foreach (var s in strategies)
                //{
                //    socket.Subscribe(Encoding.UTF8.GetBytes(s.Signature));
                //    if (combinedUpdates == null)
                //    {
                //        combinedUpdates = GetProblemUpdates(socket, s.Signature).ToObservable();
                //    }
                //    else
                //    {
                //        combinedUpdates = combinedUpdates.Union(GetProblemUpdates(socket, s.Signature).ToObservable());
                //    }
                //}
                ISolutionStrategy bigToSmall = new BigToSmallSingleBucketSolutionStrategy(problemToSolve);
                ISolutionStrategy smallToBig = new SmallToBigSingleBucketSolutionStrategy(problemToSolve);
                //socket.Subscribe(Encoding.UTF8.GetBytes(bigToSmall.Signature));
                //socket.Subscribe(Encoding.UTF8.GetBytes(smallToBig.Signature));

                //var combinedUpdates = (from u in GetProblemUpdates(socket, bigToSmall.Signature)
                //                       select u)
                //                       .Union(
                //                       from u in GetProblemUpdates(socket, smallToBig.Signature)
                //                       select u);
                //bool done = false;

                //using (combinedUpdates.ToObservable().Subscribe(
                //    update =>
                //    {
                //        if (update.IsAction || update.IsInitial)
                //        {
                //            ProblemActionStep(update.IntoType<BucketActionStep>().UpdateState);
                //        }
                //        else if (update.IsCompletion)
                //        {
                //            ISolutionStrategy signedStrategy = strategies.FirstOrDefault(s => update.Signature.Equals(s.Signature));
                //            if (signedStrategy != null)
                //            {
                //                var solnResult = update.IntoType<SolutionResult>().UpdateState;
                //                ProblemSolutionCompleted(signedStrategy.StrategyName);
                //                // Don't need this with the bool done
                //                signedStrategy.RemoteResult(solnResult);
                //                SignalSolution(signedStrategy.StrategyName, solnResult);
                //            }
                //            //if (bigToSmall.Signature.Equals(update.Signature))
                //            //{
                //            //    SignalSolution(bigToSmall.StrategyName, update.IntoType<SolutionResult>().UpdateState);
                //            //}
                //            //else if (smallToBig.Signature.Equals(update.Signature))
                //            //{
                //            //    SignalSolution(smallToBig.StrategyName, update.IntoType<SolutionResult>().UpdateState);
                //            //}
                //        }
                //        else if (update.IsError)
                //        {
                //            ProblemActionError(update.GetException<Exception>());
                //        }
                //    },
                //    ProblemActionError,
                //    () => { done = true; }
                //    ))
                //{
                //    byte[] qMsgBytes = new byte[12];
                //    Buffer.BlockCopy(BitConverter.GetBytes(problemToSolve.FirstBucket.Capacity), 0, qMsgBytes, 0, 4);
                //    Buffer.BlockCopy(BitConverter.GetBytes(problemToSolve.SecondBucket.Capacity), 0, qMsgBytes, 4, 4);
                //    Buffer.BlockCopy(BitConverter.GetBytes(problemToSolve.GoalWaterVolume), 0, qMsgBytes, 8, 4);
                //    CloudQueueMessage qMsg = new CloudQueueMessage(qMsgBytes);
                //    queue.AddMessage(qMsg);
                //    while (!done)
                //    {
                //        Thread.Sleep(500);
                //    }
                //}
                Task b2sWork, s2bWork;
                // Put the work.YieldWeb value in a local value type which is easier to pass to child Tasks
                bool yieldOnAction = work.YieldWeb;
                // The Client does NOT want to use a Rx Observable to wrap the message stream from the ZmqSocket
                if (!work.ObserverOnZmq)
                {
                    // Start tasks to listen for Worker updates on their own ZmqSockets per each SolutionStrategy that will be employed to solve the problem and signal Client(s) as they come in
                    b2sWork = Task.Run(() =>
                        {
                            try
                            {
                                foreach (var step in GetWorkerUpdates(ctx, bigToSmall, result => SignalSolution(bigToSmall.StrategyName, result)))
                                {
                                    ProblemActionStep(step, yieldOnAction);
                                }
                                ProblemSolutionCompleted("Big to Small");
                            }
                            catch (Exception ex)
                            {
                                ProblemActionError(ex);
                            }
                        });
                    s2bWork = Task.Run(() =>
                        {
                            try
                            {
                                foreach (var step in GetWorkerUpdates(ctx, smallToBig, result => SignalSolution(smallToBig.StrategyName, result)))
                                {
                                    ProblemActionStep(step, yieldOnAction);
                                }
                                ProblemSolutionCompleted("Small to Big");
                            }
                            catch (Exception ex)
                            {
                                ProblemActionError(ex);
                            }
                        });
                }
                else // The Client wants to use a Rx Observable to wrap the message stream from the ZmqSocket
                {
                    // First create a lazy evaluated Linq Sequence around getting ZmqSocket subscription messages for a specific SolutionStrategy
                    var b2sUpdates = from u in GetWorkerUpdates(ctx, bigToSmall, result => SignalSolution(bigToSmall.StrategyName, result))
                                     select u;
                    // Start a task in which a Rx Observable will wrap the message stream and signal the Client(s) when messages are received
                    b2sWork = Task.Run(() => b2sUpdates.ToObservable()
                        .Subscribe(step => ProblemActionStep(step, yieldOnAction), ProblemActionError, () => ProblemSolutionCompleted("Big to Small")))
                        .ContinueWith(t =>
                            {
                                if (t.IsCanceled)
                                {
                                    // TODO: Log this
                                }
                                else if (t.IsFaulted)
                                {
                                    // TODO: Log the Exception
                                }
                                else if (t.IsCompleted)
                                {
                                    t.Result.Dispose();
                                }
                            });
                    // Repeat the above steps for the other SolutionStrategy
                    var s2bUpdates = from u in GetWorkerUpdates(ctx, smallToBig, result => SignalSolution(smallToBig.StrategyName, result))
                                     select u;
                    s2bWork = Task.Run(() => s2bUpdates.ToObservable()
                        .Subscribe(step => ProblemActionStep(step, yieldOnAction), ProblemActionError, () => ProblemSolutionCompleted("Small to Big")))
                        .ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                {
                                    // TODO: Log the Exception
                                }
                                else if (t.IsCanceled)
                                {
                                    // TODO: Log this
                                }
                                else if (t.IsCompleted)
                                {
                                    t.Result.Dispose();
                                }
                            });
                }
                // To ensure the Subscriptions occur before any messages are published by the Worker, we could call Thread.Yield here
                // and we will get back control once those Tasks block on attempting to receive a message
                //Thread.Yield();
                // BUT - we want to see the effects of delays on publishing and the Zmq PUB-SUB messaging so we're not doing that now
                // Create the message to be put into the Azure Storage CloudQueue that is picked up by the Workers
                byte[] qMsgBytes = new byte[20];
                Buffer.BlockCopy(BitConverter.GetBytes(problemToSolve.FirstBucket.Capacity), 0, qMsgBytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(problemToSolve.SecondBucket.Capacity), 0, qMsgBytes, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(problemToSolve.GoalWaterVolume), 0, qMsgBytes, 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(work.StartDelay), 0, qMsgBytes, 12, 4);
                // be able to signal to the worker the WorkDelay or whether it should Yield on each Action (workDelay == 0) or Not to Yield (workDelay < 0)
                int workDelay = work.WorkDelay > 0 ? work.WorkDelay : work.YieldWorker ? 0 : -1;
                Buffer.BlockCopy(BitConverter.GetBytes(workDelay), 0, qMsgBytes, 16, 4);
                CloudQueueMessage qMsg = new CloudQueueMessage(qMsgBytes);
                queue.AddMessage(qMsg);
                // Now wait for the Subscription tasks to complete
                await Task.WhenAll(b2sWork, s2bWork);
            }
            catch (Exception ex)
            {
                // TODO: Log any uncaught exception
                throw new Exception("Encountered Exception during WorkerAsync", ex);
            }
        }

        /// <summary>
        /// Signal Client(s) of an action step that was performed during the work
        /// </summary>
        /// <param name="step"><see cref="WaterBucket.Domain.BucketActionStep"/> executed during work solving a problem</param>
        /// <param name="yieldOnAction">Yield the current Thread after sending the action step message to the Client(s)</param>
        /// <remarks>The yieldOnAction parameter is designed to artifically add concurrency to messages being received and sent to the Client(s)</remarks>
        /// <seealso cref="System.Threading.Thread.Yield"/>
        protected void ProblemActionStep(BucketActionStep step, bool yieldOnAction = false)
        {
            if (BroadcastAll)
            {
                Clients.All.actionStep(step);
            }
            else
            {
                Clients.Caller.actionStep(step);
            }
            if (yieldOnAction)
                Thread.Yield();
        }

        /// <summary>
        /// Signal Client(s) of an Error encountered during the work to solve the problem
        /// </summary>
        /// <param name="ex">The <see cref="System.Exception"/> encountered during work</param>
        protected void ProblemActionError(Exception ex)
        {
            if (BroadcastAll)
            {
                Clients.All.errorInSolve(ex);
            }
            else
            {
                Clients.Caller.errorInSolve(ex);
            }
        }

        /// <summary>
        /// Signal Client(s) that the work for an <see cref="WaterBucket.Domain.ISolutionStrategy"/> has been completed
        /// </summary>
        /// <param name="strategy">Identifiable name for the <see cref="WaterBucket.Domain.ISolutionStrategy"/> that has completed its work on the <see cref="WaterBucket.Domain.Problem"/></param>
        protected void ProblemSolutionCompleted(string strategy)
        {
            if (BroadcastAll)
            {
                Clients.All.problemCompleted(strategy);
            }
            else
            {
                Clients.Caller.problemCompleted(strategy);
            }
        }

        /// <summary>
        /// Signal Client(s) of the solution result obtained from solving the problem using a given strategy
        /// </summary>
        /// <param name="strategy">Identifiable name for the <see cref="WaterBucket.Domain.ISolutionStrategy"/> used to solve the problem and reaching this <see cref="WaterBucket.Domain.SolutionResult"/></param>
        /// <param name="result">The <see cref="WaterBucket.Domain.SolutionResult"/> from using a specific <see cref="WaterBucket.Domain.ISolutionStrategy"/> to solve the <see cref="WaterBucket.Domain.Problem"/></param>
        /// <seealso cref="ProblemSolutionCompleted"/>
        protected void SignalSolution(string strategy, SolutionResult result)
        {
            if (BroadcastAll)
            {
                Clients.All.problemSolution(strategy, result);
            }
            else
            {
                Clients.Caller.problemSolution(strategy, result);
            }
        }
        
    }
}