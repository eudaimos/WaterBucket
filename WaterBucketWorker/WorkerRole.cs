using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using ZeroMQ;
using ZeroMQ.Devices;
using WaterBucket.Domain;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Core;
using System.Threading.Tasks;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using Utils;

namespace WaterBucketWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        // try not to have magic strings
        private const string ZMQ_PUBLISHER = "ZmqPublisher";
        private const string INTERNAL_PUB_ADDRESS = "inproc://publishing";
        private const string CLOUD_Q_FOR_WORK = "problemstosolve";

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("WaterBucketWorker entry point called", "Information");

            // Check whether the CloudQueue is for the staging or production environment, which use separate Queues in order to provide fully isolated environments
            // IF THIS IS CHANGED - the WorkerRole MUST be RESTARTED in order for it to take affect
            bool isStaging = RoleEnvironmentExt.GetRoleConfigSetting("UseStaging", false);
            // Get the CloudQueue on which we want to get messages from the WebRoles for the work to be done
            // NOTE: don't use RoleEnvironmentExt.GetRoleConfigSetting() as the WorkerRole should crash if the setting isn't configured properly
            string storageAccountConStr = !isStaging ? RoleEnvironment.GetConfigurationSettingValue("StorageAccount") : RoleEnvironment.GetConfigurationSettingValue("StagingStorageAccount");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConStr);
            CloudQueueClient qClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = qClient.GetQueueReference(CLOUD_Q_FOR_WORK);
            queue.CreateIfNotExists();

            // Get the TCP address for publishing update messages for work being done using ZeroMQ
            RoleInstanceEndpoint zeromqPubEP = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.ContainsKey(ZMQ_PUBLISHER) ? RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[ZMQ_PUBLISHER] : null;
            // Gracefully just not work to prevent restarts and errors from a known configuration limitation - allows testing WebRoles properly detecting inability to connect by changing the EndPoint name
            while (zeromqPubEP == null)
            {
                Thread.Sleep(1000);
                zeromqPubEP = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.ContainsKey(ZMQ_PUBLISHER) ? RoleEnvironment.CurrentRoleInstance.InstanceEndpoints[ZMQ_PUBLISHER] : null;
            }
            //if (zeromqPubEP == null)
            //{
            //    throw new Exception("Could not get 'ZmqPublisher' Endpoint");
            //}

            string publishAddress = string.Format("tcp://{0}:{1}", zeromqPubEP.IPEndpoint.Address.ToString(), zeromqPubEP.IPEndpoint.Port); //this.GetRoleConfigSetting("PublisherAddress", "tcp://127.0.0.1:9898");

            //string baseIPAddress;
            //try
            //{
            //    baseIPAddress = RoleEnvironment.GetConfigurationSettingValue("BaseIPAddress");
            //}
            //catch (RoleEnvironmentException rex)
            //{
            //    //ErrorSignal.
            //    baseIPAddress = "127.0.0.1";
            //}

            string signalAddress = null;
            bool useSignal = false;

            try
            {
                // Not the right way to get addresses for ZmqSockets - need to use Azure EndPoints and configure them in the WebRole/WorkerRoles before publishing
                signalAddress = RoleEnvironment.GetConfigurationSettingValue("SignallerAddress");
                useSignal = !string.IsNullOrWhiteSpace(signalAddress);
            }
            catch (RoleEnvironmentException rex)
            {
                useSignal = false;
            }

            using (var ctx = ZmqContext.Create())
            {
                try
                {
                    // Use XPUB-XSUB forwarding from Internal Publish to External TCP Publish or use the clrzmq provided ForwardDevice which uses regular PUB-SUB sockets?
                    // XPUB-XSUB was designed by the ZeroMQ team for this use case
                    bool useXForwarder = RoleEnvironmentExt.GetRoleConfigSetting("Forwarder.UseX", false);
                    using (Device forwarder = !useXForwarder ? new ForwarderDevice(ctx, INTERNAL_PUB_ADDRESS, publishAddress, DeviceMode.Threaded) as Device : new XForwarderDevice(ctx, INTERNAL_PUB_ADDRESS, publishAddress, DeviceMode.Threaded))
                    {
                        forwarder.FrontendSetup.SubscribeAll();
                        forwarder.Start();
                        //using (ZmqSocket pairSubSocket = ctx.CreateSocket(SocketType.XSUB))
                        //{
                        //    pairSubSocket.Bind(INTERNAL_PUB_ADDRESS);
                        //    pairSubSocket.SubscribeAll();
                        //    using (ZmqSocket pairPubSocket = ctx.CreateSocket(SocketType.XPUB))
                        //    {
                        //        pairPubSocket.Bind(publishAddress);
                        //        pairSubSocket.Forward(pairPubSocket);
                        //using (ZmqSocket socket = ctx.CreateSocket(SocketType.PUB))
                        //{
                        //    socket.Connect("inproc://publishing");
                        //socket.Bind(publishAddress);

                        // Wait a beat for work to arrive
                        Thread.Sleep(3000);
                        while (true)
                        {
                            CloudQueueMessage msg = null;
                            // Read problem to solve off of the queue
                            msg = queue.GetMessage(TimeSpan.FromMinutes(3));
                            // If no problems on the queue
                            if (msg == null)
                            {
                                // Using Signal Sockets to receive work would be more efficient that using Thread.Sleep
                                if (useSignal)
                                {
                                    #region Using Signaller Socket
                                    // Instead of waiting in a Sleep cycle, let the application Signal you to wake up when there is a problem to work on
                                    using (var signaller = ctx.CreateSocket(SocketType.PULL))
                                    {
                                        signaller.Bind(signalAddress);
                                        while (true)
                                        {
                                            var signal = signaller.ReceiveMessage();
                                            // Empty messages are false signals
                                            if (signal.IsEmpty)
                                                continue;
                                            // Having now been signalled to wake up, get a problem from the queue
                                            msg = queue.GetMessage(TimeSpan.FromMinutes(3));
                                            // If this is the first worker to get the problem, then attempt to solve it
                                            if (msg != null)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                else // If not using a Signal, then sleep and repeat the while loop to get a message
                                {
                                    Thread.Sleep(500);
                                    continue;
                                }
                            }
                            // Detect if the Work to be done is a poison pill and delete it without working on it
                            if (msg.DequeueCount > 5)
                            {
                                queue.DeleteMessage(msg);
                                continue;
                            }

                            try
                            {
                                //bool yieldOnAction = RoleEnvironmentExt.GetRoleConfigSetting("Action.On.Yield", false);
                                //int workTimeout = RoleEnvironmentExt.GetRoleConfigSetting("Work.Timeout", 0);
                                
                                // Turn the Queue Message into a Problem and some WorkOrder settings
                                byte[] problemBytes = msg.AsBytes;
                                int firstCapacity = BitConverter.ToInt32(problemBytes, 0);
                                int secondCapacity = BitConverter.ToInt32(problemBytes, 4);
                                int waterGoal = BitConverter.ToInt32(problemBytes, 8);
                                int startDelay = BitConverter.ToInt32(problemBytes, 12);
                                int workDelay = BitConverter.ToInt32(problemBytes, 16);
                                Problem problemToSolve = new Problem(firstCapacity, secondCapacity, waterGoal);

                                // Get the SolutionStrategies to be employed in solving the problem
                                ISolutionStrategy smallToBig, bigToSmall;
                                smallToBig = new SmallToBigSingleBucketSolutionStrategy(problemToSolve);
                                bigToSmall = new BigToSmallSingleBucketSolutionStrategy(problemToSolve);
                                // Get a binary formatter for messages that will be local to the Task thread
                                BinaryFormatter startSmallFormatter = new BinaryFormatter();
                                //var scheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
                                //var startSmallUpdates = from step in problemToSolve.Solve(smallToBig).ToObservable()
                                //                        select GetUpdateMessage(smallToBig, step.ActionTaken == BucketActions.Init ? ProblemUpdateType.Initial : ProblemUpdateType.Action, step);
                                //bool startSmallCompleted = false;
                                //using (startSmallUpdates.ObserveOn(System.Reactive.Concurrency.CurrentThreadScheduler.Instance).Subscribe(
                                //    actMsg =>
                                //    {
                                //        socket.SendMessage(actMsg);
                                //    },
                                //    ex => SolutionExceptionOccurred(smallToBig, socket, ex, startSmallFormatter),
                                //    () => SolutionCompletion(smallToBig, socket, ref startSmallCompleted, startSmallFormatter)))
                                //{
                                //    while (!startSmallCompleted)
                                //    {
                                //        Thread.Sleep(300);
                                //    }
                                //    queue.DeleteMessage(msg);
                                //}

                                //var scheduler = System.Reactive.Concurrency.Scheduler.CurrentThread;
                                //var thread = System.Threading.Thread.CurrentThread;
                                //var s = new System.Reactive.Concurrency.EventLoopScheduler(ts => thread);

                                if (startDelay > 0)
                                    Thread.Sleep(startDelay);

                                // Create a task for performing work using a SolutionStrategy and create a ZmqSocket to publish updates internally which 
                                // will be forwarded by the ForwarderDevice to any subscribers listening over our TCP Publish Endpoint
                                var startSmallTask = Task.Run(() =>
                                    {
                                        using (ZmqSocket smallPubSocket = ctx.CreateSocket(SocketType.PUB))
                                        {
                                            smallPubSocket.Connect(INTERNAL_PUB_ADDRESS);
                                            try
                                            {
                                                // Iterate Each action step for the Small to Big Solution Strategy
                                                foreach (var step in problemToSolve.Solve(smallToBig))
                                                {
                                                    SolutionAction(smallToBig, smallPubSocket, step, startSmallFormatter);
                                                    // if work order asked for artificial work delay
                                                    if (workDelay > 0)
                                                        Thread.Sleep(workDelay);
                                                    // If work order asked for artificial concurrency
                                                    else if (workDelay == 0)
                                                        Task.Yield();
                                                }
                                                SolutionCompletion(smallToBig, smallPubSocket, startSmallFormatter);
                                            }
                                            catch (Exception ex)
                                            {
                                                SolutionExceptionOccurred(smallToBig, smallPubSocket, ex, startSmallFormatter);
                                            }
                                            //using (problemToSolve.Solve(smallToBig).ToObservable().Subscribe(
                                            //    step => SolutionAction(smallToBig, smallPubSocket, step, startSmallFormatter),
                                            //    ex => SolutionExceptionOccurred(smallToBig, smallPubSocket, ex, startSmallFormatter),
                                            //    () => SolutionCompletion(smallToBig, smallPubSocket, startSmallFormatter)))
                                            //{
                                            //    // All activity is done by the subscribe handlers, we use the using clause to efficiently clean up 
                                            //    // the IDisposable from the Subscribe method
                                            //}
                                        }
                                    });
                                // Get a binary formatter for messages that will be local to the Task thread
                                BinaryFormatter startBigFormatter = new BinaryFormatter();
                                // Create a task for performing work using a SolutionStrategy and create a ZmqSocket to publish updates internally which 
                                // will be forwarded by the ForwarderDevice to any subscribers listening over our TCP Publish Endpoint
                                var startBigTask = Task.Run(() =>
                                    {
                                        //bool bigIsComplete = false;
                                        using (ZmqSocket bigPubSocket = ctx.CreateSocket(SocketType.PUB))
                                        {
                                            bigPubSocket.Connect(INTERNAL_PUB_ADDRESS);
                                            try
                                            {
                                                foreach (var step in problemToSolve.Solve(bigToSmall))
                                                {
                                                    SolutionAction(bigToSmall, bigPubSocket, step, startBigFormatter);
                                                    if (workDelay > 0)
                                                        Thread.Sleep(workDelay);
                                                    else if (workDelay == 0)
                                                        Task.Yield();
                                                }
                                                SolutionCompletion(bigToSmall, bigPubSocket, startBigFormatter);
                                            }
                                            catch (Exception ex)
                                            {
                                                SolutionExceptionOccurred(bigToSmall, bigPubSocket, ex, startBigFormatter);
                                            }
                                            //using (problemToSolve.Solve(bigToSmall).ToObservable().Subscribe(
                                            //    step => SolutionAction(bigToSmall, bigPubSocket, step, startBigFormatter),
                                            //    ex => SolutionExceptionOccurred(bigToSmall, bigPubSocket, ex, startBigFormatter),
                                            //    () => SolutionCompletion(bigToSmall, bigPubSocket, ref bigIsComplete, startBigFormatter)))
                                            //{
                                            //    // All activity is done by the subscribe handlers, we use the using clause to efficiently clean up 
                                            //    // the IDisposable from the Subscribe method
                                            //    //while (!bigIsComplete)
                                            //    //    yield;
                                            //}
                                        }
                                    });
                                // Wait for the work of both SolutionStrategies to complete before deleting the Queue message
                                Task.WhenAll(startSmallTask, startBigTask)
                                    .ContinueWith(t =>
                                        {
                                            if (t.IsFaulted)
                                            {
                                                // TODO: Log the t.Exception
                                            }
                                            else if (t.IsCanceled)
                                            {
                                                // TODO: Log the Cancellation of the Task
                                            }
                                            else if (t.IsCompleted)
                                            {
                                                // We can remove the message from the Queue so it's not executed again
                                                queue.DeleteMessage(msg);
                                            }
                                        }).Wait();
                            }
                            catch (Exception ex)
                            {
                                // log the Exception
                            }
                            //}
                        }
                    }
                }
                catch (ZmqDeviceException zde)
                {
                    throw new Exception("Encountered ZmqDeviceException [" + zde.ToString() + " - " + zde.Message + "]", zde);
                }
                catch (ZmqSocketException zse)
                {
                    throw new Exception("Encountered ZmqSocketException [" + zse.ToString() + " - " + zse.Message + "]", zse);
                }
            }
        }

        private static void SolutionAction(ISolutionStrategy strategy, ZmqSocket socket, BucketActionStep step, IFormatter formatter = null)
        {
            SendUpdateMessage(strategy, step.ActionTaken == BucketActions.Init ? ProblemUpdateType.Initial : ProblemUpdateType.Action, socket, step as IFrameable);
        }

        private static void SolutionExceptionOccurred(ISolutionStrategy strategy, ZmqSocket socket, Exception ex, IFormatter formatter = null)
        {
            SendUpdateMessage(strategy, ProblemUpdateType.Error, socket, ex, formatter);
        }

        private static void SolutionCompletion(ISolutionStrategy strategy, ZmqSocket socket, IFormatter formatter = null)
        {
            SendUpdateMessage(strategy, ProblemUpdateType.Completion, socket, strategy.Result as IFrameable);
        }

        private static void SolutionCompletion(ISolutionStrategy strategy, ZmqSocket socket, ref bool completed, IFormatter formatter = null)
        {
            SendUpdateMessage(strategy, ProblemUpdateType.Completion, socket, strategy.Result as IFrameable);
            completed = true;
        }

        private static void SendUpdateMessage(ISolutionStrategy strategy, ProblemUpdateType updateType, ZmqSocket socket, object data, IFormatter formatter = null)
        {
            formatter = formatter ?? new BinaryFormatter();
            ZmqMessage message = new ZmqMessage();
            message.Append(Encoding.UTF8.GetBytes(strategy.Signature));
            message.AppendEmptyFrame();
            message.Append(new byte[1] { (byte)updateType });
            message.AppendEmptyFrame();
            message.Append(new byte[1] { 0x7f });
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, data);
                // TODO: Find more efficient way to write byte[] since MemoryStream.ToArray performs a copy
                //       whereas MemoryStream.GetBuffer() returns all allocated bytes whether they are empty or not
                message.Append(ms.ToArray());
            }
            socket.SendMessage(message);
        }

        private static void SendUpdateMessage(ISolutionStrategy strategy, ProblemUpdateType updateType, ZmqSocket socket, IBinaryConvertible data)
        {
            ZmqMessage message = new ZmqMessage();
            message.Append(Encoding.UTF8.GetBytes(strategy.Signature));
            message.AppendEmptyFrame();
            message.Append(new byte[1] { (byte)updateType });
            message.AppendEmptyFrame();
            message.Append(new byte[1] { 0x7f });
            message.Append(data.GetBytes());
            socket.SendMessage(message);
        }

        private static void SendUpdateMessage(ISolutionStrategy strategy, ProblemUpdateType updateType, ZmqSocket socket, IFrameable data)
        {
            ZmqMessage message = GetUpdateMessage(strategy, updateType, data);
            socket.SendMessage(message);
        }

        private static ZmqMessage GetUpdateMessage(ISolutionStrategy strategy, ProblemUpdateType updateType, IFrameable data)
        {
            var dataFrames = data.GetFrames(Encoding.UTF8).ToList();
            List<Frame> msgFrames = new List<Frame>(dataFrames.Count + 5);
            msgFrames.Add(new Frame(Encoding.UTF8.GetBytes(strategy.Signature)));
            msgFrames.Add(Frame.Empty);
            msgFrames.Add(new Frame(new byte[1] { (byte)updateType }));
            msgFrames.Add(Frame.Empty);
            msgFrames.Add(new Frame(new byte[1] { 0x7f }));
            msgFrames.AddRange(dataFrames);
            ZmqMessage message = new ZmqMessage(msgFrames);
            return message;
        }

        //private ZmqMessage GetUpdateMessage(ISolutionStrategy strategy, ProblemUpdateType updateType, IFrameable data)
        //{

        //}

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // This code sets up a handler to update CloudStorageAccount instances when their corresponding
            // configuration settings change in the service configuration file.
            //CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            //{
            //    // Provide the configSetter with the initial value
            //    configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));

            //    RoleEnvironment.Changed += (sender, arg) =>
            //    {
            //        if (arg.Changes.OfType<RoleEnvironmentConfigurationSettingChange>()
            //            .Any((change) => (change.ConfigurationSettingName == configName)))
            //        {
            //            // The corresponding configuration setting has changed, propagate the value
            //            if (!configSetter(RoleEnvironment.GetConfigurationSettingValue(configName)))
            //            {
            //                // In this case, the change to the storage account credentials in the
            //                // service configuration is significant enough that the role needs to be
            //                // recycled in order to use the latest settings. (for example, the 
            //                // endpoint has changed)
            //                RoleEnvironment.RequestRecycle();
            //            }
            //        }
            //    };
            //});

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }
    }
}
