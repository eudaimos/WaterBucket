using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WaterBucket.Domain;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using System.Collections;
using ZeroMQ;
using System.Text;
using System.Collections.Generic;

namespace WaterBucketDomain.Tests
{
    [TestClass]
    public class BucketTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestCleanup]
        public void TearDown()
        {
        }

        [TestMethod]
        public void TestBucketSuccessfulContruction()
        {
            // Arrange
            int bucketCapacity = 5;

            // Act
            Bucket utBucket = new Bucket(bucketCapacity);

            //Assert
            Assert.AreEqual(bucketCapacity, utBucket.Capacity, "Bucket contructed with wrong Capacity");
            Assert.AreEqual(0, utBucket.CurrentFill, "Bucket constructed with incorrect CurrentFill");
            Assert.IsTrue(utBucket.IsEmpty, "Bucket contructed and reporting it is not empty");
            Assert.IsFalse(utBucket.IsFull, "Bucket constructed and reporting it is full");
        }

        [TestMethod]
        public void TestBucketName()
        {
            // Arrange
            string name = "utBucket", rename = "testingBucket";
            int bucketCapacity = 5;

            // Act
            Bucket utBucket = new Bucket(bucketCapacity, name);

            //Assert
            Assert.AreEqual(name, utBucket.Name, "Bucket constructed with '" + name + "' but having Name == '" + utBucket.Name + "'");
            Assert.AreNotEqual(rename, utBucket.Name, "Somehow the bucket was constructed with the rename");

            // Act 2
            utBucket.Name = rename;

            // Assert 2
            Assert.AreEqual(rename, utBucket.Name, "Bucket rename to rename didn't work");
            Assert.AreNotEqual(name, utBucket.Name, "Bucket rename to rename didn't work");

            // Act 3
            utBucket.Name = name;

            // Assert 3
            Assert.AreEqual(name, utBucket.Name, "Bucket rename back to name didn't work");
            Assert.AreNotEqual(rename, utBucket.Name, "Bucket rename back to name didn't work");

            // Assert Name didn't break normal constructor logic
            Assert.AreEqual(bucketCapacity, utBucket.Capacity, "Bucket contructed with wrong Capacity");
            Assert.AreEqual(0, utBucket.CurrentFill, "Bucket constructed with incorrect CurrentFill");
            Assert.IsTrue(utBucket.IsEmpty, "Bucket contructed and reporting it is not empty");
            Assert.IsFalse(utBucket.IsFull, "Bucket constructed and reporting it is full");
        }

        [TestMethod]
        public void TestBucketConstructionArgumentOutOfRange()
        {
            // Arrange
            int lessThanZero = -1;
            int wayLessThanZero = int.MinValue;
            int zero = 0;
            Random rand = new Random((int)DateTime.Now.Ticks);
            int randomLessThanZero = 0 - rand.Next(int.MaxValue - 1);
            Bucket utBucket = null;

            try
            {
                // Act
                utBucket = new Bucket(lessThanZero);
                Assert.Fail("able to construct a Bucket with capacity = " + lessThanZero);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // Assert
                Assert.IsNull(utBucket, "Constructor threw ArgumentOutOfRangeException on capacity = " + lessThanZero + " but still created a reference to a Bucket");
            }
            try
            {
                // Act
                utBucket = new Bucket(wayLessThanZero);
                Assert.Fail("able to construct a Bucket with capacity = " + wayLessThanZero);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Assert.IsNull(utBucket, "Constructor threw ArgumentOutOfRangeException on capacity = " + wayLessThanZero + " but still created a reference to a Bucket");
            }
            try
            {
                // Act
                utBucket = new Bucket(zero);
                Assert.Fail("able to construct a Bucket with capacity = " + zero);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Assert.IsNull(utBucket, "Constructor threw ArgumentOutOfRangeException on capacity = " + zero + " but still created a reference to a Bucket");
            }
            try
            {
                // Act
                utBucket = new Bucket(randomLessThanZero);
                Assert.Fail("able to construct a Bucket with capacity = " + randomLessThanZero);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Assert.IsNull(utBucket, "Constructor threw ArgumentOutOfRangeException on capacity = " + randomLessThanZero + " but still created a reference to a Bucket");
            }
        }

        [TestMethod]
        public void TestBucketFill()
        {
            // Arrange
            int bucketCapacity = 5;
            Bucket utBucket = new Bucket(bucketCapacity);
            Reservoire waterSource = new Reservoire();

            // Act
            utBucket.Fill(waterSource);

            //Assert
            Assert.AreEqual(bucketCapacity, utBucket.CurrentFill, "Filled the Bucket but it is not at Capacity");
            Assert.AreNotEqual(0, utBucket.CurrentFill, "Filled the bucket but it is reporting 0 CurrentFill");
            Assert.AreEqual(utBucket.CurrentFill, utBucket.Capacity, "Filled bucket but CurrentFill not equal to Capacity");
            Assert.IsTrue(utBucket.IsFull, "Filled bucket to capacity but it is reporting not full");
            Assert.IsFalse(utBucket.IsEmpty, "Filled bucket to capacity but it is reporting empty");
        }

        [TestMethod]
        public void TestBucketEmpty()
        {
            // Arrange
            int bucketCapacity = 5;
            Bucket utBucket = new Bucket(bucketCapacity);
            Reservoire waterSource = new Reservoire();

            // Act
            utBucket.Fill(waterSource);
            utBucket.Empty(waterSource);

            //Assert
            Assert.AreEqual(0, utBucket.CurrentFill, "Emptied the Bucket but CurrentFill = " + utBucket.CurrentFill);
            Assert.AreNotEqual(bucketCapacity, utBucket.CurrentFill, "Emptied the bucket but it still has CurrentFill as test capacity");
            Assert.AreNotEqual(utBucket.CurrentFill, utBucket.Capacity, "Emptied the bucket but it is reporting CurrentFill == Capacity");
            Assert.IsTrue(utBucket.IsEmpty, "Emptied the bucket but reporting it is not empty");
            Assert.IsFalse(utBucket.IsFull, "Emptied the bucket but it is reporting it is full");
        }

        [TestMethod]
        public void TestBucketTransferBigToSmall()
        {
            // Arrange
            int bucketCapacityBig = 5, bucketCapacitySmall = 3;
            int bucketDifference = Math.Abs(bucketCapacityBig - bucketCapacitySmall);
            Bucket utBucketBig = new Bucket(bucketCapacityBig), utBucketSmall = new Bucket(bucketCapacitySmall);
            Reservoire waterSource = new Reservoire();

            // Act
            utBucketBig.Fill(waterSource);
            Assert.IsTrue(utBucketSmall.IsEmpty, "Something went wrong is Arrange as small bucket is not empty");
            utBucketBig.TransferTo(utBucketSmall);

            //Assert
            Assert.AreEqual(bucketCapacitySmall, utBucketSmall.CurrentFill, "Transferred from big to small but small bucket not at test capacity");
            Assert.AreEqual(utBucketSmall.Capacity, utBucketSmall.CurrentFill, "Transferred from big to small but small bucket not at reported capacity");
            Assert.AreNotEqual(0, utBucketSmall.CurrentFill, "Transferred big to small but small reporting CurrentFill == 0");
            Assert.IsTrue(utBucketSmall.IsFull, "Transferred big to small but small reporting is not full");
            Assert.IsFalse(utBucketSmall.IsEmpty, "Transferred big to small but small reporting it is empty");
            Assert.AreNotEqual(0, utBucketBig.CurrentFill, "Transferred from big to small but big reporting CurrentFill == 0");
            Assert.AreEqual(bucketDifference, utBucketBig.CurrentFill, "Transferred from big to small but big CurrentFill != bucketDifference(" + bucketDifference + ")");
            Assert.AreNotEqual(bucketCapacityBig, utBucketBig.CurrentFill, "Transferred from big to small but big has CurrentFill == test capacity big");
            Assert.AreNotEqual(utBucketBig.CurrentFill, utBucketBig.Capacity, "Transferred from big to small but big has CurrentFill == Capacity");
            Assert.IsFalse(utBucketBig.IsFull, "Transferred big to small but big reporting full");
            Assert.IsFalse(utBucketBig.IsEmpty, "Transferred from big to small but big reporting is empty");
        }

        [TestMethod]
        public void TestBucketClone()
        {
            // Arrange
            Bucket sourceBucket = new Bucket(20, "SourceBucket");
            Bucket otherBucket = new Bucket(8, "OtherBucket");
            Reservoire waterSrc = new Reservoire();
            Bucket utNewBucket, utFullBucket, utTransBucket, utEmptiedBucket;
            Bucket utNewBucketFromClone, utFullBucketFromClone, utTransBucketFromClone, utEmptiedBucketFromClone;

            // Act
            utNewBucket = sourceBucket.Clone() as Bucket;
            sourceBucket.Fill(waterSrc);
            utFullBucket = sourceBucket.Clone() as Bucket;
            sourceBucket.TransferTo(otherBucket);
            utTransBucket = sourceBucket.Clone() as Bucket;
            sourceBucket.Empty(waterSrc);
            utEmptiedBucket = sourceBucket.Clone() as Bucket;
            utNewBucketFromClone = utNewBucket.Clone() as Bucket;
            utFullBucketFromClone = utFullBucket.Clone() as Bucket;
            utTransBucketFromClone = utTransBucket.Clone() as Bucket;
            utEmptiedBucketFromClone = utEmptiedBucket.Clone() as Bucket;

            // Assert
            Assert.AreEqual(sourceBucket.Capacity, utNewBucket.Capacity, "Cloned new bucket has different capacity");
            Assert.AreEqual(0, utNewBucket.CurrentFill, "Cloned new bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utNewBucket.Name, "Cloned new bucket has wrong name");
            Assert.IsTrue(utNewBucket.IsEmpty, "Cloned new bucket is not empty");
            Assert.IsFalse(utNewBucket.IsFull, "Cloned new bucket is full");

            Assert.AreEqual(sourceBucket.Capacity, utFullBucket.Capacity, "Cloned full bucket has different capacity");
            Assert.AreEqual(sourceBucket.Capacity, utFullBucket.CurrentFill, "Cloned full bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utFullBucket.Name, "Cloned full bucket has wrong name");
            Assert.IsFalse(utFullBucket.IsEmpty, "Cloned full bucket is empty");
            Assert.IsTrue(utFullBucket.IsFull, "Cloned full bucket is not full");

            Assert.AreEqual(sourceBucket.Capacity, utTransBucket.Capacity, "Cloned transferred bucket has different capacity");
            Assert.AreEqual(sourceBucket.Capacity - otherBucket.Capacity, utTransBucket.CurrentFill, "Cloned transferred bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utTransBucket.Name, "Cloned transferred bucket has wrong name");
            Assert.IsFalse(utTransBucket.IsEmpty, "Cloned transferred bucket is empty");
            Assert.IsFalse(utTransBucket.IsFull, "Cloned transferred bucket is full");

            Assert.AreEqual(sourceBucket.Capacity, utEmptiedBucket.Capacity, "Cloned emptied bucket has different capacity");
            Assert.AreEqual(0, utEmptiedBucket.CurrentFill, "Cloned emptied bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utEmptiedBucket.Name, "Cloned emptied bucket has wrong name");
            Assert.IsTrue(utEmptiedBucket.IsEmpty, "Cloned emptied bucket is not empty");
            Assert.IsFalse(utEmptiedBucket.IsFull, "Cloned emptied bucket is full");

            Assert.AreEqual(utNewBucket.Capacity, utNewBucketFromClone.Capacity, "Cloned new bucket from Clone has different capacity");
            Assert.AreEqual(utNewBucket.CurrentFill, utNewBucketFromClone.CurrentFill, "Cloned new bucket from Clone has wrong current fill");
            Assert.AreEqual(utNewBucket.Name, utNewBucketFromClone.Name, "Cloned new bucket from Clone has wrong name");
            Assert.IsTrue(utNewBucketFromClone.IsEmpty, "Cloned new bucket from Clone is not empty");
            Assert.IsFalse(utNewBucketFromClone.IsFull, "Cloned new bucket from Clone is full");

            Assert.AreEqual(utFullBucket.Capacity, utFullBucketFromClone.Capacity, "Cloned Full bucket from Clone has different capacity");
            Assert.AreEqual(utFullBucket.CurrentFill, utFullBucketFromClone.CurrentFill, "Cloned Full bucket from Clone has wrong current fill");
            Assert.AreEqual(utFullBucket.Name, utFullBucketFromClone.Name, "Cloned Full bucket from Clone has wrong name");
            Assert.IsFalse(utFullBucketFromClone.IsEmpty, "Cloned Full bucket from Clone is empty");
            Assert.IsTrue(utFullBucketFromClone.IsFull, "Cloned Full bucket from Clone is not full");

            Assert.AreEqual(utTransBucket.Capacity, utTransBucketFromClone.Capacity, "Cloned Transferred bucket from Clone has different capacity");
            Assert.AreEqual(utTransBucket.CurrentFill, utTransBucketFromClone.CurrentFill, "Cloned Transferred bucket from Clone has wrong current fill");
            Assert.AreEqual(utTransBucket.Name, utTransBucketFromClone.Name, "Cloned Transferred bucket from Clone has wrong name");
            Assert.IsFalse(utTransBucketFromClone.IsEmpty, "Cloned Transferred bucket from Clone is empty");
            Assert.IsFalse(utTransBucketFromClone.IsFull, "Cloned Transferred bucket from Clone is full");

            Assert.AreEqual(utEmptiedBucket.Capacity, utEmptiedBucketFromClone.Capacity, "Cloned Emptied bucket from Clone has different capacity");
            Assert.AreEqual(utEmptiedBucket.CurrentFill, utEmptiedBucketFromClone.CurrentFill, "Cloned Emptied bucket from Clone has wrong current fill");
            Assert.AreEqual(utEmptiedBucket.Name, utEmptiedBucketFromClone.Name, "Cloned Emptied bucket from Clone has wrong name");
            Assert.IsTrue(utEmptiedBucketFromClone.IsEmpty, "Cloned Emptied bucket from Clone is not empty");
            Assert.IsFalse(utEmptiedBucketFromClone.IsFull, "Cloned Emptied bucket from Clone is full");


            // Act 2


            // Assert 2


            // Act 3


            // Assert 3

        }

        [TestMethod]
        public void TestBucketSerialization()
        {
            // Arrange
            Bucket utBucket = new Bucket(88, "utBucket");
            Bucket otherBucket = new Bucket(32, "other Bucket");
            Reservoire waterSource = new Reservoire();
            utBucket.Fill(waterSource);
            utBucket.TransferTo(otherBucket);

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                try
                {
                    // Act - Serialize
                    formatter.Serialize(ms, utBucket);
                    // Assert - Serialize
                }
                catch (Exception ex)
                {
                    Assert.Fail("Serialization failed for Bucket with message: " + ex.Message);
                }

                ms.Position = 0;

                try
                {
                    // Act - Deserialize
                    Bucket deserializedBucket = (Bucket)formatter.Deserialize(ms);

                    // Assert - Deserialize
                    Assert.IsTrue(utBucket.Equals(deserializedBucket), "Deserialized Bucket not equal to utBucket that was Serialized");
                }
                catch (Exception ex)
                {
                    Assert.Fail("Deserialization failed for Bucket with message: " + ex.Message);
                }
            }
        }

        [TestMethod]
        public void TestBucketToFromBytes()
        {
            // Arrange
            Bucket sourceBucket = new Bucket(54, "SourceBucket");
            Bucket otherBucket = new Bucket(4, "OtherBucket");
            Encoding textEncoding = Encoding.UTF8;
            Bucket utNewBucket = new Bucket();
            Bucket utFullBucket = new Bucket();
            Bucket utTransBucket = new Bucket();
            Bucket utEmptiedBucket = new Bucket();
            Bucket utNewBucketFromBuffer = new Bucket();
            Bucket utFullBucketFromBuffer = new Bucket();
            Bucket utTransBucketFromBuffer = new Bucket();
            Bucket utEmptiedBucketFromBuffer = new Bucket();
            Reservoire waterSrc = new Reservoire();

            // Act
            byte[] utNewBytes = sourceBucket.GetBytes(textEncoding);
            sourceBucket.Fill(waterSrc);
            byte[] utFullBytes = sourceBucket.GetBytes(textEncoding);
            sourceBucket.TransferTo(otherBucket);
            byte[] utTransBytes = sourceBucket.GetBytes(textEncoding);
            sourceBucket.Empty(waterSrc);
            byte[] utEmptiedBytes = sourceBucket.GetBytes(textEncoding);

            utNewBucket.FromBytes(utNewBytes, textEncoding: textEncoding);
            utFullBucket.FromBytes(utFullBytes, textEncoding: textEncoding);
            utTransBucket.FromBytes(utTransBytes, textEncoding: textEncoding);
            utEmptiedBucket.FromBytes(utEmptiedBytes, textEncoding: textEncoding);

            byte[] utBuffer = new byte[utNewBytes.Length + utFullBytes.Length + utTransBytes.Length + utEmptiedBytes.Length];
            int toBufferOffset = 0;
            toBufferOffset = utNewBucket.OntoBuffer(utBuffer, toBufferOffset, textEncoding);
            toBufferOffset = utFullBucket.OntoBuffer(utBuffer, toBufferOffset, textEncoding);
            toBufferOffset = utTransBucket.OntoBuffer(utBuffer, toBufferOffset, textEncoding);
            toBufferOffset = utEmptiedBucket.OntoBuffer(utBuffer, toBufferOffset, textEncoding);
            int fromBufferOffset = 0;
            fromBufferOffset = utNewBucketFromBuffer.FromBytes(utBuffer, fromBufferOffset, textEncoding);
            fromBufferOffset = utFullBucketFromBuffer.FromBytes(utBuffer, fromBufferOffset, textEncoding);
            fromBufferOffset = utTransBucketFromBuffer.FromBytes(utBuffer, fromBufferOffset, textEncoding);
            fromBufferOffset = utEmptiedBucketFromBuffer.FromBytes(utBuffer, fromBufferOffset, textEncoding);

            // Assert
            Assert.AreEqual(sourceBucket.Capacity, utNewBucket.Capacity, "Retrieved new bucket has different capacity");
            Assert.AreEqual(0, utNewBucket.CurrentFill, "Retrieved new bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utNewBucket.Name, "Retrieved new bucket has wrong name");
            Assert.IsTrue(utNewBucket.IsEmpty, "Retrieved new bucket is not empty");
            Assert.IsFalse(utNewBucket.IsFull, "Retrieved new bucket is full");

            Assert.AreEqual(sourceBucket.Capacity, utFullBucket.Capacity, "Retrieved full bucket has different capacity");
            Assert.AreEqual(sourceBucket.Capacity, utFullBucket.CurrentFill, "Retrieved full bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utFullBucket.Name, "Retrieved full bucket has wrong name");
            Assert.IsFalse(utFullBucket.IsEmpty, "Retrieved full bucket is empty");
            Assert.IsTrue(utFullBucket.IsFull, "Retrieved full bucket is not full");

            Assert.AreEqual(sourceBucket.Capacity, utTransBucket.Capacity, "Retrieved transferred bucket has different capacity");
            Assert.AreEqual(sourceBucket.Capacity - otherBucket.Capacity, utTransBucket.CurrentFill, "Retrieved transferred bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utTransBucket.Name, "Retrieved transferred bucket has wrong name");
            Assert.IsFalse(utTransBucket.IsEmpty, "Retrieved transferred bucket is empty");
            Assert.IsFalse(utTransBucket.IsFull, "Retrieved transferred bucket is full");

            Assert.AreEqual(sourceBucket.Capacity, utEmptiedBucket.Capacity, "Retrieved emptied bucket has different capacity");
            Assert.AreEqual(0, utEmptiedBucket.CurrentFill, "Retrieved emptied bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utEmptiedBucket.Name, "Retrieved emptied bucket has wrong name");
            Assert.IsTrue(utEmptiedBucket.IsEmpty, "Retrieved emptied bucket is not empty");
            Assert.IsFalse(utEmptiedBucket.IsFull, "Retrieved emptied bucket is full");

            Assert.AreEqual(utNewBucket.Capacity, utNewBucketFromBuffer.Capacity, "Retrieved new bucket from buffer has different capacity");
            Assert.AreEqual(utNewBucket.CurrentFill, utNewBucketFromBuffer.CurrentFill, "Retrieved new bucket from buffer has wrong current fill");
            Assert.AreEqual(utNewBucket.Name, utNewBucketFromBuffer.Name, "Retrieved new bucket from buffer has wrong name");
            Assert.IsTrue(utNewBucketFromBuffer.IsEmpty, "Retrieved new bucket from buffer is not empty");
            Assert.IsFalse(utNewBucketFromBuffer.IsFull, "Retrieved new bucket from buffer is full");

            Assert.AreEqual(utFullBucket.Capacity, utFullBucketFromBuffer.Capacity, "Retrieved Full bucket from buffer has different capacity");
            Assert.AreEqual(utFullBucket.CurrentFill, utFullBucketFromBuffer.CurrentFill, "Retrieved Full bucket from buffer has wrong current fill");
            Assert.AreEqual(utFullBucket.Name, utFullBucketFromBuffer.Name, "Retrieved Full bucket from buffer has wrong name");
            Assert.IsFalse(utFullBucketFromBuffer.IsEmpty, "Retrieved Full bucket from buffer is empty");
            Assert.IsTrue(utFullBucketFromBuffer.IsFull, "Retrieved Full bucket from buffer is not full");

            Assert.AreEqual(utTransBucket.Capacity, utTransBucketFromBuffer.Capacity, "Retrieved Transferred bucket from buffer has different capacity");
            Assert.AreEqual(utTransBucket.CurrentFill, utTransBucketFromBuffer.CurrentFill, "Retrieved Transferred bucket from buffer has wrong current fill");
            Assert.AreEqual(utTransBucket.Name, utTransBucketFromBuffer.Name, "Retrieved Transferred bucket from buffer has wrong name");
            Assert.IsFalse(utTransBucketFromBuffer.IsEmpty, "Retrieved Transferred bucket from buffer is empty");
            Assert.IsFalse(utTransBucketFromBuffer.IsFull, "Retrieved Transferred bucket from buffer is full");

            Assert.AreEqual(utEmptiedBucket.Capacity, utEmptiedBucketFromBuffer.Capacity, "Retrieved Emptied bucket from buffer has different capacity");
            Assert.AreEqual(utEmptiedBucket.CurrentFill, utEmptiedBucketFromBuffer.CurrentFill, "Retrieved Emptied bucket from buffer has wrong current fill");
            Assert.AreEqual(utEmptiedBucket.Name, utEmptiedBucketFromBuffer.Name, "Retrieved Emptied bucket from buffer has wrong name");
            Assert.IsTrue(utEmptiedBucketFromBuffer.IsEmpty, "Retrieved Emptied bucket from buffer is not empty");
            Assert.IsFalse(utEmptiedBucketFromBuffer.IsFull, "Retrieved Emptied bucket from buffer is full");

            Assert.AreEqual(toBufferOffset, fromBufferOffset, "Buffer offsets differ");

        }

        [TestMethod]
        public void TestBucketFraming()
        {
            // Arrange
            Bucket sourceBucket = new Bucket(54, "SourceBucket");
            Bucket otherBucket = new Bucket(4, "OtherBucket");
            Encoding textEncoding = Encoding.UTF8;
            Bucket utNewBucket = new Bucket();
            Bucket utFullBucket = new Bucket();
            Bucket utTransBucket = new Bucket();
            Bucket utEmptiedBucket = new Bucket();
            Bucket utNewBucketFromMessage = new Bucket();
            Bucket utFullBucketFromMessage = new Bucket();
            Bucket utTransBucketFromMessage = new Bucket();
            Bucket utEmptiedBucketFromMessage = new Bucket();
            Reservoire waterSrc = new Reservoire();

            // Act
            // Use ToList since IEnumerable/yield methods are lazy evaluated
            IEnumerable<Frame> utNewFrames = sourceBucket.GetFrames(textEncoding).ToList();
            sourceBucket.Fill(waterSrc);
            IEnumerable<Frame> utFullFrames = sourceBucket.GetFrames(textEncoding).ToList();
            sourceBucket.TransferTo(otherBucket);
            IEnumerable<Frame> utTransFrames = sourceBucket.GetFrames(textEncoding).ToList();
            sourceBucket.Empty(waterSrc);
            IEnumerable<Frame> utEmptiedFrames = sourceBucket.GetFrames(textEncoding).ToList();
            utNewBucket.FromFrames(utNewFrames, textEncoding: textEncoding);
            utFullBucket.FromFrames(utFullFrames, textEncoding: textEncoding);
            utTransBucket.FromFrames(utTransFrames, textEncoding: textEncoding);
            utEmptiedBucket.FromFrames(utEmptiedFrames, textEncoding: textEncoding);

            int numFrames = utNewFrames.Count() + utFullFrames.Count() + utTransFrames.Count() + utEmptiedFrames.Count();
            List<Frame> msgFrames = new List<Frame>(numFrames);
            msgFrames.AddRange(utNewBucket.GetFrames(textEncoding));
            msgFrames.AddRange(utFullBucket.GetFrames(textEncoding));
            msgFrames.AddRange(utTransBucket.GetFrames(textEncoding));
            msgFrames.AddRange(utEmptiedBucket.GetFrames(textEncoding));
            int offset = 0;
            offset = utNewBucketFromMessage.FromFrames(msgFrames, offset, textEncoding);
            offset = utFullBucketFromMessage.FromFrames(msgFrames, offset, textEncoding);
            offset = utTransBucketFromMessage.FromFrames(msgFrames, offset, textEncoding);
            offset = utEmptiedBucketFromMessage.FromFrames(msgFrames, offset, textEncoding);

            // Assert
            Assert.AreEqual(sourceBucket.Capacity, utNewBucket.Capacity, "Retrieved new bucket has different capacity");
            Assert.AreEqual(0, utNewBucket.CurrentFill, "Retrieved new bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utNewBucket.Name, "Retrieved new bucket has wrong name");
            Assert.IsTrue(utNewBucket.IsEmpty, "Retrieved new bucket is not empty");
            Assert.IsFalse(utNewBucket.IsFull, "Retrieved new bucket is full");

            Assert.AreEqual(sourceBucket.Capacity, utFullBucket.Capacity, "Retrieved full bucket has different capacity");
            Assert.AreEqual(sourceBucket.Capacity, utFullBucket.CurrentFill, "Retrieved full bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utFullBucket.Name, "Retrieved full bucket has wrong name");
            Assert.IsFalse(utFullBucket.IsEmpty, "Retrieved full bucket is empty");
            Assert.IsTrue(utFullBucket.IsFull, "Retrieved full bucket is not full");

            Assert.AreEqual(sourceBucket.Capacity, utTransBucket.Capacity, "Retrieved transferred bucket has different capacity");
            Assert.AreEqual(sourceBucket.Capacity - otherBucket.Capacity, utTransBucket.CurrentFill, "Retrieved transferred bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utTransBucket.Name, "Retrieved transferred bucket has wrong name");
            Assert.IsFalse(utTransBucket.IsEmpty, "Retrieved transferred bucket is empty");
            Assert.IsFalse(utTransBucket.IsFull, "Retrieved transferred bucket is full");

            Assert.AreEqual(sourceBucket.Capacity, utEmptiedBucket.Capacity, "Retrieved emptied bucket has different capacity");
            Assert.AreEqual(0, utEmptiedBucket.CurrentFill, "Retrieved emptied bucket has wrong current fill");
            Assert.AreEqual(sourceBucket.Name, utEmptiedBucket.Name, "Retrieved emptied bucket has wrong name");
            Assert.IsTrue(utEmptiedBucket.IsEmpty, "Retrieved emptied bucket is not empty");
            Assert.IsFalse(utEmptiedBucket.IsFull, "Retrieved emptied bucket is full");

            Assert.AreEqual(utNewBucket.Capacity, utNewBucketFromMessage.Capacity, "Retrieved new bucket from message has different capacity");
            Assert.AreEqual(utNewBucket.CurrentFill, utNewBucketFromMessage.CurrentFill, "Retrieved new bucket from message has wrong current fill");
            Assert.AreEqual(utNewBucket.Name, utNewBucketFromMessage.Name, "Retrieved new bucket from message has wrong name");
            Assert.IsTrue(utNewBucketFromMessage.IsEmpty, "Retrieved new bucket from message is not empty");
            Assert.IsFalse(utNewBucketFromMessage.IsFull, "Retrieved new bucket from message is full");

            Assert.AreEqual(utFullBucket.Capacity, utFullBucketFromMessage.Capacity, "Retrieved Full bucket from message has different capacity");
            Assert.AreEqual(utFullBucket.CurrentFill, utFullBucketFromMessage.CurrentFill, "Retrieved Full bucket from message has wrong current fill");
            Assert.AreEqual(utFullBucket.Name, utFullBucketFromMessage.Name, "Retrieved Full bucket from message has wrong name");
            Assert.IsFalse(utFullBucketFromMessage.IsEmpty, "Retrieved Full bucket from message is empty");
            Assert.IsTrue(utFullBucketFromMessage.IsFull, "Retrieved Full bucket from message is not full");

            Assert.AreEqual(utTransBucket.Capacity, utTransBucketFromMessage.Capacity, "Retrieved Transferred bucket from message has different capacity");
            Assert.AreEqual(utTransBucket.CurrentFill, utTransBucketFromMessage.CurrentFill, "Retrieved Transferred bucket from message has wrong current fill");
            Assert.AreEqual(utTransBucket.Name, utTransBucketFromMessage.Name, "Retrieved Transferred bucket from message has wrong name");
            Assert.IsFalse(utTransBucketFromMessage.IsEmpty, "Retrieved Transferred bucket from message is empty");
            Assert.IsFalse(utTransBucketFromMessage.IsFull, "Retrieved Transferred bucket from message is full");

            Assert.AreEqual(utEmptiedBucket.Capacity, utEmptiedBucketFromMessage.Capacity, "Retrieved Emptied bucket from message has different capacity");
            Assert.AreEqual(utEmptiedBucket.CurrentFill, utEmptiedBucketFromMessage.CurrentFill, "Retrieved Emptied bucket from message has wrong current fill");
            Assert.AreEqual(utEmptiedBucket.Name, utEmptiedBucketFromMessage.Name, "Retrieved Emptied bucket from message has wrong name");
            Assert.IsTrue(utEmptiedBucketFromMessage.IsEmpty, "Retrieved Emptied bucket from message is not empty");
            Assert.IsFalse(utEmptiedBucketFromMessage.IsFull, "Retrieved Emptied bucket from message is full");

            Assert.AreEqual(numFrames, offset, "Wrong number of frames after retrieving data");
        }
    }
}
