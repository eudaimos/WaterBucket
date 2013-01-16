using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Core;
using WaterBucket.Domain;
using System.Threading;
using ZeroMQ;
using System.Text;
using System.Threading.Tasks;

namespace WaterBucketWorker.Tests
{
    [TestClass]
    public class WorkerRoleTests
    {
        private const int SMALL_TO_BIG = 0;
        private const int BIG_TO_SMALL = 1;
        private static readonly int[] NUM_ACTIONS = new int[2] { 6, 8 };

        string _storageAccountConStr;
        CloudQueue _queue;
        string _publishAddress;
        string _signalAddress;
        bool _useSignal = false;
        int _sleepTimeout = 500;

        [TestInitialize]
        public void ArrangeTests()
        {
            try
            {
                _storageAccountConStr = RoleEnvironment.GetConfigurationSettingValue("StorageAccount");
            }
            catch (Exception ex)
            {
                _storageAccountConStr = "UseDevelopmentStorage=true";
            }

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageAccountConStr);
            CloudQueueClient qClient = storageAccount.CreateCloudQueueClient();
            _queue = qClient.GetQueueReference("problemstosolve");
            _queue.CreateIfNotExists();

            try
            {
                _publishAddress = RoleEnvironment.GetConfigurationSettingValue("PublisherAddress");
            }
            catch (Exception ex)
            {
                //ErrorSignal.
                _publishAddress = "tcp://127.0.0.1:9898";
            }

            try
            {
                _signalAddress = RoleEnvironment.GetConfigurationSettingValue("SignallerAddress");
                _useSignal = !string.IsNullOrWhiteSpace(_signalAddress);
            }
            catch (Exception ex)
            {
                _useSignal = false;
            }

        }

        [TestMethod]
        public void TestQueue()
        {
            // Arrange
            Problem testProblem = new Problem(3, 5, 4);
            byte[] qMsgBytes = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(testProblem.FirstBucket.Capacity), 0, qMsgBytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(testProblem.SecondBucket.Capacity), 0, qMsgBytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(testProblem.GoalWaterVolume), 0, qMsgBytes, 8, 4);
            CloudQueueMessage qMessage = new CloudQueueMessage(qMsgBytes);

            // Act
            _queue.AddMessage(qMessage);
            Thread.Sleep(6000);

            // Assert
            // Assertion done by watching the WorkerRole in Debugger
        }

        [TestMethod]
        public void TestSocketReceive()
        {
            // Arrange
            var ctx = ZmqContext.Create();
            var sub = ctx.CreateSocket(SocketType.SUB);
            Problem testProblem = new Problem(3, 5, 4);
            byte[] qMsgBytes = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(testProblem.FirstBucket.Capacity), 0, qMsgBytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(testProblem.SecondBucket.Capacity), 0, qMsgBytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(testProblem.GoalWaterVolume), 0, qMsgBytes, 8, 4);
            CloudQueueMessage qMessage = new CloudQueueMessage(qMsgBytes);

            var smallToBig = new SmallToBigSingleBucketSolutionStrategy(testProblem);
            var bigToSmall = new BigToSmallSingleBucketSolutionStrategy(testProblem);

            sub.Connect(_publishAddress);
            sub.Subscribe(Encoding.UTF8.GetBytes(smallToBig.Signature));
            sub.Subscribe(Encoding.UTF8.GetBytes(bigToSmall.Signature));

            // Act

            var subTask = Task.Run(() =>
                {
                    ProblemUpdate update = null;
                    ZmqMessage zqm = sub.ReceiveMessage(TimeSpan.FromSeconds(2));
                    if (zqm == null)
                    {
                        Assert.Fail("Did not receive a message from the subscription socket within 2 seconds of adding message to Queue");
                    }
                    int[] numActions = new int[2] { 0, 0 };
                    bool[] completed = new bool[2] { false, false };

                    while ((zqm != null) && (!completed.All(c => c)))
                    {
                        zqm = sub.ReceiveMessage();
                        string msgSig = Encoding.UTF8.GetString(zqm.Unwrap().Buffer);
                        update = new ProblemUpdate(zqm.Select(f => f.Buffer).ToArray());
                        if (update.IsAction)
                        {
                            if (msgSig.Equals(smallToBig.Signature))
                            {
                                numActions[SMALL_TO_BIG]++;
                            }
                            else if (msgSig.Equals(bigToSmall.Signature))
                            {
                                numActions[BIG_TO_SMALL]++;
                            }
                        }
                        else if (update.IsCompletion)
                        {
                            if (msgSig.Equals(smallToBig.Signature))
                            {
                                completed[SMALL_TO_BIG] = true;
                            }
                            else if (msgSig.Equals(bigToSmall.Signature))
                            {
                                completed[BIG_TO_SMALL] = true;
                            }
                        }
                    }

                    // Assert
                    Assert.IsFalse(update.IsError, "Received an Exception from the Socket");
                    Assert.IsTrue(completed.All(c => c), "Not all strategies completed");
                    Assert.AreEqual(NUM_ACTIONS[SMALL_TO_BIG], numActions[SMALL_TO_BIG], "Small to Big strategy received wrong number of action messages (" + numActions[SMALL_TO_BIG] + ") from socket - expected " + NUM_ACTIONS[SMALL_TO_BIG]);
                    Assert.AreEqual(NUM_ACTIONS[BIG_TO_SMALL], numActions[BIG_TO_SMALL], "Big to Small strategy received wrong number of action messages (" + numActions[BIG_TO_SMALL] + ") from socket - expected " + NUM_ACTIONS[BIG_TO_SMALL]);
                });
            //if ((subTask.Status == TaskStatus.WaitingToRun))// || (subTask.Status == TaskStatus.WaitingForActivation))
            //  subTask.Start();
            _queue.AddMessage(qMessage);
            subTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Assert.Fail("subTask threw Exception: " + t.Exception.ToString() + " " + t.Exception.Message);

                    sub.UnsubscribeAll();
                    sub.Disconnect(_publishAddress);
                    sub.Dispose();
                    ctx.Dispose();
                });
        }
    }
}
