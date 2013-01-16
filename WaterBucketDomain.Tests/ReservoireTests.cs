using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WaterBucket.Domain;

namespace WaterBucketDomain.Tests
{
    [TestClass]
    public class ReservoireTests
    {
        public Random Rand { get; set; }
        public bool CaughtEx { get; set; }

        [TestInitialize]
        public void InitTests()
        {
            Rand = new Random((int)DateTime.Now.Ticks);
            CaughtEx = false;
        }

        [TestMethod]
        public void TestReservoireStateConstructorZeros()
        {
            // Arrange
            ReservoireState utStateEmpty = null, utStateArgs = null;

            // Act
            utStateEmpty = new ReservoireState();
            utStateArgs = new ReservoireState(0, 0);

            // Assert
            Assert.IsNotNull(utStateEmpty, "Somehow new constructor for ReservoireState without arguments returned null");
            Assert.AreEqual(0, utStateEmpty.VolumeUsed, "VolumeUsed not equal to 0 passed to constructor without arguments");
            Assert.AreEqual(0, utStateEmpty.VolumeThrownOut, "VolumeThrownOut not equal to 0 passed to constructor without arguments");
            Assert.IsNotNull(utStateArgs, "Somehow new constructor for ReservoireState with arguments returned null");
            Assert.AreEqual(0, utStateArgs.VolumeUsed, "VolumeUsed not equal to 0 passed to constructor with arguments");
            Assert.AreEqual(0, utStateArgs.VolumeThrownOut, "VolumeThrownOut not equal to 0 passed to constructor with arguments");
        }

        [TestMethod]
        public void TestReservoireStateConstructorIntMax()
        {
            // Arrange
            ReservoireState utStateMaxZero = null, utStateZeroMax = null, utStateMaxMax = null;

            // Act
            utStateMaxZero = new ReservoireState(int.MaxValue, 0);
            utStateZeroMax = new ReservoireState(0, int.MaxValue);
            utStateMaxMax = new ReservoireState(int.MaxValue, int.MaxValue);

            // Assert
            Assert.IsNotNull(utStateMaxZero, "Somehow new constructor for ReservoireState without arguments returned null");
            Assert.AreEqual(int.MaxValue, utStateMaxZero.VolumeUsed, "VolumeUsed not equal to int.MaxValue passed to constructor without arguments");
            Assert.AreEqual(0, utStateMaxZero.VolumeThrownOut, "VolumeThrownOut not equal to 0 passed to constructor without arguments");
            Assert.IsNotNull(utStateZeroMax, "Somehow new constructor for ReservoireState with arguments returned null");
            Assert.AreEqual(0, utStateZeroMax.VolumeUsed, "VolumeUsed not equal to 0 passed to constructor with arguments");
            Assert.AreEqual(int.MaxValue, utStateZeroMax.VolumeThrownOut, "VolumeThrownOut not equal to int.MaxValue passed to constructor with arguments");
            Assert.IsNotNull(utStateMaxMax, "Somehow new constructor for ReservoireState without arguments returned null");
            Assert.AreEqual(int.MaxValue, utStateMaxMax.VolumeUsed, "VolumeUsed not equal to int.MaxValue passed to constructor without arguments");
            Assert.AreEqual(int.MaxValue, utStateMaxMax.VolumeThrownOut, "VolumeThrownOut not equal to int.MaxValue passed to constructor without arguments");
        }

        [TestMethod]
        public void TestReservoireStateConstructorSuccessful()
        {
            // Arrange
            ReservoireState utState = null;
            int numIterations = Rand.Next(5000, 10000);

            for (int i = 0; i < numIterations; i++)
            {
                // Act
                int vUsed = Rand.Next(), vThrownOut = Rand.Next();
                utState = new ReservoireState(vUsed, vThrownOut);

                // Assert
                Assert.IsNotNull(utState, "Somehow new constructor for ReservoireState returned null [" + i + "/" + numIterations + "]");
                Assert.AreEqual(vUsed, utState.VolumeUsed, "VolumeUsed not equal to value passed to constructor [" + i + "/" + numIterations + "]");
                Assert.AreEqual(vThrownOut, utState.VolumeThrownOut, "VolumeThrownOut not equal to value passed to constructor [" + i + "/" + numIterations + "]");
            }
        }

        [TestMethod]
        public void TestReservoireStateConstructorThrowsArgumentOutOfRangeExceptionForMinusOne()
        {
            // Arrange
            ReservoireState utStateMinusZero = null, utStateZeroMinus = null, utStateMinusMinus = null;

            // Act
            try
            {
                utStateMinusZero = new ReservoireState(-1, 0);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("volumeUsed", ex.ParamName, "ReservoireState constructor(-1, 0) did not identify volumeUsed param as out of range");
            }
            catch (Exception ex)
            {
                Assert.Fail("ReservoireState constructor(-1, 0) threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException");
            }
            Assert.IsTrue(CaughtEx, "ReservoireState constructor(-1, 0) did not throw ArgumentOutOfRangeException");
            Assert.IsNull(utStateMinusZero, "ReservoireState constructor(-1, 0) did return an object");

            CaughtEx = false;

            // Act
            try
            {
                utStateZeroMinus = new ReservoireState(0, -1);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("volumeThrownOut", ex.ParamName, "ReservoireState constructor(0, -1) did not identify volumeThrownOut param as out of range");
            }
            catch (Exception ex)
            {
                Assert.Fail("ReservoireState constructor(0, -1) threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException");
            }
            Assert.IsTrue(CaughtEx, "ReservoireState constructor(0, -1) did not throw ArgumentOutOfRangeException");
            Assert.IsNull(utStateZeroMinus, "ReservoireState constructor(0, -1) did return an object");

            CaughtEx = false;

            // Act
            try
            {
                utStateMinusMinus = new ReservoireState(-1, -1);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("volumeUsed", ex.ParamName, "ReservoireState constructor(-1, -1) did not identify volumeUsed param as out of range");
            }
            catch (Exception ex)
            {
                Assert.Fail("ReservoireState constructor(-1, -1) threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException");
            }
            Assert.IsTrue(CaughtEx, "ReservoireState constructor(-1, -1) did not throw ArgumentOutOfRangeException");
            Assert.IsNull(utStateMinusMinus, "ReservoireState constructor(-1, -1) did return an object");
        }

        [TestMethod]
        public void TestReservoireStateConstructorThrowsArgumentOutOfRangeExceptionForIntMin()
        {
            // Arrange
            ReservoireState utStateMinZero = null, utStateZeroMin = null, utStateMinMin = null;

            // Act
            try
            {
                utStateMinZero = new ReservoireState(int.MinValue, 0);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("volumeUsed", ex.ParamName, "ReservoireState constructor(int.MinValue, 0) did not identify volumeUsed param as out of range");
            }
            catch (Exception ex)
            {
                Assert.Fail("ReservoireState constructor(int.MinValue, 0) threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException");
            }
            Assert.IsTrue(CaughtEx, "ReservoireState constructor(int.MinValue, 0) did not throw ArgumentOutOfRangeException");
            Assert.IsNull(utStateMinZero, "ReservoireState constructor(int.MinValue, 0) did return an object");

            CaughtEx = false;

            // Act
            try
            {
                utStateZeroMin = new ReservoireState(0, int.MinValue);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("volumeThrownOut", ex.ParamName, "ReservoireState constructor(0, int.MinValue) did not identify volumeThrownOut param as out of range");
            }
            catch (Exception ex)
            {
                Assert.Fail("ReservoireState constructor(0, int.MinValue) threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException");
            }
            Assert.IsTrue(CaughtEx, "ReservoireState constructor(0, int.MinValue) did not throw ArgumentOutOfRangeException");
            Assert.IsNull(utStateZeroMin, "ReservoireState constructor(0, int.MinValue) did return an object");

            CaughtEx = false;

            // Act
            try
            {
                utStateMinMin = new ReservoireState(int.MinValue, int.MinValue);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("volumeUsed", ex.ParamName, "ReservoireState constructor(int.MinValue, int.MinValue) did not identify volumeUsed param as out of range");
            }
            catch (Exception ex)
            {
                Assert.Fail("ReservoireState constructor(int.MinValue, int.MinValue) threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException");
            }
            Assert.IsTrue(CaughtEx, "ReservoireState constructor(int.MinValue, int.MinValue) did not throw ArgumentOutOfRangeException");
            Assert.IsNull(utStateMinMin, "ReservoireState constructor(int.MinValue, int.MinValue) did return an object");
        }

        [TestMethod]
        public void TestReservoireStateConstructorThrowsArgumentOutOfRangeException()
        {
            // Arrange
            int numIterations = Rand.Next(5000, 10000);
            ReservoireState utStateMinusOk = null, utStateOkMinus = null, utStateMinusMinus = null;

            for (int i = 0; i < numIterations; i++)
            {
                // Arrange
                int vUsedMinus = Rand.Next(int.MinValue + 1, -2), vUsedOk = Rand.Next(),
                    vThrownOutMinus = Rand.Next(int.MinValue + 1, -2), vThrownOutOk = Rand.Next();

                // Act
                try
                {
                    utStateMinusOk = new ReservoireState(vUsedMinus, vThrownOutOk);
                }
                // Assert
                catch (ArgumentOutOfRangeException ex)
                {
                    CaughtEx = true;
                    Assert.AreEqual("volumeUsed", ex.ParamName, "ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutOk + ") did not identify volumeUsed param as out of range [i=" + i + "/" + numIterations + "]");
                }
                catch (Exception ex)
                {
                    Assert.Fail("ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutOk + ") threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException [i=" + i + "/" + numIterations + "]");
                }
                Assert.IsTrue(CaughtEx, "ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutOk + ") did not throw ArgumentOutOfRangeException [i=" + i + "/" + numIterations + "]");
                Assert.IsNull(utStateMinusOk, "ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutOk + ") did return an object [i=" + i + "/" + numIterations + "]");

                CaughtEx = false;

                // Act
                try
                {
                    utStateOkMinus = new ReservoireState(vUsedOk, vThrownOutMinus);
                }
                // Assert
                catch (ArgumentOutOfRangeException ex)
                {
                    CaughtEx = true;
                    Assert.AreEqual("volumeThrownOut", ex.ParamName, "ReservoireState constructor(" + vUsedOk + ", " + vThrownOutMinus + ") did not identify volumeThrownOut param as out of range [i=" + i + "/" + numIterations + "]");
                }
                catch (Exception ex)
                {
                    Assert.Fail("ReservoireState constructor(" + vUsedOk + ", " + vThrownOutMinus + ") threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException [i=" + i + "/" + numIterations + "]");
                }
                Assert.IsTrue(CaughtEx, "ReservoireState constructor(" + vUsedOk + ", " + vThrownOutMinus + ") did not throw ArgumentOutOfRangeException [i=" + i + "/" + numIterations + "]");
                Assert.IsNull(utStateOkMinus, "ReservoireState constructor(" + vUsedOk + ", " + vThrownOutMinus + ") did return an object [i=" + i + "/" + numIterations + "]");

                CaughtEx = false;

                // Act
                try
                {
                    utStateMinusMinus = new ReservoireState(vUsedMinus, vThrownOutMinus);
                }
                // Assert
                catch (ArgumentOutOfRangeException ex)
                {
                    CaughtEx = true;
                    Assert.AreEqual("volumeUsed", ex.ParamName, "ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutMinus + ") did not identify volumeUsed param as out of range [i=" + i + "/" + numIterations + "]");
                }
                catch (Exception ex)
                {
                    Assert.Fail("ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutMinus + ") threw different Exception - " + ex.GetType().Name + ": " + ex.Message + " instead of ArgumentOutOfRangeException [i=" + i + "/" + numIterations + "]");
                }
                Assert.IsTrue(CaughtEx, "ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutMinus + ") did not throw ArgumentOutOfRangeException [i=" + i + "/" + numIterations + "]");
                Assert.IsNull(utStateMinusMinus, "ReservoireState constructor(" + vUsedMinus + ", " + vThrownOutMinus + ") did return an object [i=" + i + "/" + numIterations + "]");
            }
        }

        [TestMethod]
        public void TestReservoireSuccessfulConstruction()
        {
            // Arrange
            Reservoire utReservoire = null;
            ReservoireState newState = new ReservoireState(0, 0), testState = null;

            try
            {
                // Act
                utReservoire = new Reservoire();
                testState = utReservoire.CurrentState;
            }
            catch (Exception ex)
            {
                Assert.Fail("Reservoire constructor threw an Exception - " + ex.GetType().Name + ": " + ex.Message);
            }

            // Assert
            Assert.IsNotNull(utReservoire, "Constructor for Reservoire returned null");
            Assert.AreEqual(0, utReservoire.VolumeUsed, "New Reservoire has VolumeUsed != 0");
            Assert.AreEqual(0, utReservoire.VolumeThrownOut, "New Reservoire has VolumeThrownOut != 0");
            Assert.AreEqual(newState.VolumeUsed, testState.VolumeUsed, "Test state VolumeUsed != New state VolumeUsed");
            Assert.AreEqual(newState.VolumeThrownOut, testState.VolumeThrownOut, "Test state VolumeThrownOut != New state VolumeThrownOut");
        }

        [TestMethod]
        public void TestReservoireBasicGet()
        {
            // Arrange
            int numAmounts = Rand.Next(5000, 10000), iterations = 0;
            int[] amounts = new int[numAmounts], totals = new int[numAmounts];
            int amount = 0;
            long longTotal = 0;
            for (int a = 0; a < numAmounts; a++)
            {
                amount = Rand.Next(1, 30000);
                longTotal += amount;
                if ((longTotal >> 32) > 0)
                {
                    iterations = a;
                    break;
                }
                amounts[a] = amount;
                totals[a] = (int)longTotal;
            }
            if (iterations == 0)
            {
                iterations = numAmounts;
                amount = Rand.Next(int.MaxValue - totals[totals.Length - 1] + 2, int.MaxValue - 1);
                longTotal += amount;
            }
            Reservoire utReservoire = new Reservoire();

            int amountGotten = 0;
            for (int i = 0; i < iterations; i++)
            {
                // Act
                amountGotten = utReservoire.GetWater(amounts[i]);

                // Assert
                Assert.AreEqual(amounts[i], amountGotten, "GetWater returned different amount that requested [i=" + i + "/" + iterations + "]");
                Assert.AreEqual(totals[i], utReservoire.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + iterations + "]");
            }
            int noGet = -1;
            try
            {
                noGet = utReservoire.GetWater(amount);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "GetWater ArgumentOutOfRangeException didn't report 'amount' parameter");
                Assert.AreEqual(-1, noGet, "Reservoire GetWater returned value == " + noGet + " even though ArgumentOutOfRangeException was thrown for getting too much water");
            }
            catch (Exception ex)
            {
                Assert.Fail("GetWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when VolumeUsed == " + totals[iterations - 1] + " && get amount == " + amount);
            }
            Assert.IsTrue(CaughtEx, "GetWater did not throw ArgumentOutOfRangeException when VolumeUsed == " + totals[iterations - 1] + " && get amount == " + amount);
        }

        [TestMethod]
        public void TestReservoireBasicPut()
        {
            // Arrange
            int numAmounts = Rand.Next(5000, 10000), iterations = 0;
            int[] amounts = new int[numAmounts], totals = new int[numAmounts];
            int amount = 0;
            long longTotal = 0;
            for (int a = 0; a < numAmounts; a++)
            {
                amount = Rand.Next(1, 30000);
                longTotal += amount;
                if ((longTotal >> 32) > 0)
                {
                    iterations = a;
                    break;
                }
                amounts[a] = amount;
                totals[a] = (int)longTotal;
            }
            if (iterations == 0)
            {
                iterations = numAmounts;
                amount = Rand.Next(int.MaxValue - totals[totals.Length - 1] + 2, int.MaxValue - 1);
                longTotal += amount;
            }
            Reservoire utReservoire = new Reservoire();

            for (int i = 0; i < iterations; i++)
            {
                // Act
                utReservoire.PutWater(amounts[i]);

                // Assert
                Assert.AreEqual(totals[i], utReservoire.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + iterations + "]");
            }
            try
            {
                // Act
                utReservoire.PutWater(amount);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "PutWater ArgumentOutOfRangeException didn't report 'amount' parameter");
            }
            catch (Exception ex)
            {
                Assert.Fail("PutWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when VolumeThrownOut == " + totals[iterations - 1] + " && put amount == " + amount);
            }
            Assert.IsTrue(CaughtEx, "PutWater did not throw ArgumentOutOfRangeException when VolumeThrownOut == " + totals[iterations - 1] + " && put amount == " + amount);

        }

        [TestMethod]
        public void TestReservoireGetWaterIntMax()
        {
            // Arrange
            int wayBig = int.MaxValue;
            Reservoire utReservoire = new Reservoire();

            // Act
            int gotWayBig = utReservoire.GetWater(wayBig);

            // Assert
            Assert.AreEqual(wayBig, gotWayBig, "Getting int.MaxValue water returned different amount of water");
            Assert.AreEqual(wayBig, utReservoire.VolumeUsed, "VolumeUsed isn't int.MaxValue when getting int.MaxValue water");
        }

        [TestMethod]
        public void TestReservoireGetWaterGreaterThanIntMax()
        {
            // Arrange
            int getAmount = 1,
                wayBig = int.MaxValue;
            Reservoire utReservoire = new Reservoire();


            // Act
            int gotAmount = utReservoire.GetWater(getAmount);
            int gotVolume = utReservoire.VolumeUsed;
            int tooMuch = -1;
            try
            {
                tooMuch = utReservoire.GetWater(wayBig);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
                Assert.AreEqual(-1, tooMuch, "Reservoire GetWater returned value == " + tooMuch + " even though ArgumentOutOfRangeException was thrown for getting too much water");
            }
            catch (Exception ex)
            {
                Assert.Fail("GetWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when VolumeUsed == " + gotVolume + " && get amount == " + wayBig);
            }
            Assert.IsTrue(CaughtEx, "GetWater did not throw ArgumentOutOfRangeException when VolumeUsed == " + gotVolume + " && get amount == " + wayBig);

        }

        [TestMethod]
        public void TestReservoireGetWaterRandomLargeInts()
        {
            // Arrange
            int numIterations = Rand.Next(5000, 10000);

            for (int i = 0; i < numIterations; i++)
            {
                Reservoire utReservoire = new Reservoire();

                // Act
                int randWayBig = Rand.Next(1000000, int.MaxValue - 1);
                int randWayBigGot = utReservoire.GetWater(randWayBig);

                // Assert
                Assert.AreEqual(randWayBig, randWayBigGot, "Getting random way big (" + randWayBig + ") water returned different amount of water [i=" + i + "/" + numIterations + "]");
                Assert.AreEqual(randWayBig, utReservoire.VolumeUsed, "VolumeUsed isn't random way big (" + randWayBig + ") when getting random way big (" + randWayBig + ") water [i=" + i + "/" + numIterations + "]");
            }
        }

        [TestMethod]
        public void TestReservoireGetWaterZero()
        {
            // Arrange
            int zeroGet = 0;
            Reservoire utReservoire = new Reservoire();

            // Act
            int noGet = -1;
            try
            {
                noGet = utReservoire.GetWater(zeroGet);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
                Assert.AreEqual(-1, noGet, "Reservoire GetWater returned value == " + noGet + " even though ArgumentOutOfRangeException was thrown for getting amount == 0");
            }
            catch (Exception ex)
            {
                Assert.Fail("GetWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when getting amount == 0");
            }
            Assert.IsTrue(CaughtEx, "GetWater did not throw ArgumentOutOfRangeException when getting 0 water");
        }

        [TestMethod]
        public void TestReservoireGetWaterMinusOne()
        {
            // Arrange
            int minusGet = -1;
            Reservoire utReservoire = new Reservoire();

            // Act
            int noGet = 0;
            try
            {
                noGet = utReservoire.GetWater(minusGet);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
                Assert.AreEqual(0, noGet, "Reservoire GetWater returned value == " + noGet + " even though ArgumentOutOfRangeException was thrown for getting amount == -1");
            }
            catch (Exception ex)
            {
                Assert.Fail("GetWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when getting amount == -1");
            }
            Assert.IsTrue(CaughtEx, "GetWater did not throw ArgumentOutOfRangeException when getting -1 water");
        }

        [TestMethod]
        public void TestReservoireGetWaterIntMin()
        {
            // Arrange
            int minusGet = int.MinValue;
            Reservoire utReservoire = new Reservoire();
            int noGet = 0;

            // Act
            try
            {
                noGet = utReservoire.GetWater(minusGet);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
                Assert.AreEqual(0, noGet, "Reservoire GetWater returned value == " + noGet + " even though ArgumentOutOfRangeException was thrown for getting amount == int.MinValue");
            }
            catch (Exception ex)
            {
                Assert.Fail("GetWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when getting amount == int.MinValue");
            }
            Assert.IsTrue(CaughtEx, "GetWater did not throw ArgumentOutOfRangeException when getting int.MinValue water");
        }

        [TestMethod]
        public void TestReservoireGetWaterRandomNegatives()
        {
            // Arrange
            int numIterations = Rand.Next(5000, 10000);
            Reservoire utReservoire = new Reservoire();

            for (int i = 0; i < numIterations; i++)
            {
                int noGet = 0, minusAmount = Rand.Next(int.MinValue + 1, -2);
                CaughtEx = false;
                // Act
                try
                {
                    noGet = utReservoire.GetWater(minusAmount);
                }
                // Assert
                catch (ArgumentOutOfRangeException aore)
                {
                    CaughtEx = true;
                    Assert.AreEqual("amount", aore.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter [i=" + i + "/" + numIterations + "]");
                    Assert.AreEqual(0, noGet, "Reservoire GetWater returned value == " + noGet + " even though ArgumentOutOfRangeException was thrown for getting amount == " + minusAmount + " [i=" + i + "/" + numIterations + "]");
                }
                catch (Exception ex)
                {
                    Assert.Fail("GetWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when getting amount == " + minusAmount + " [i=" + i + "/" + numIterations + "]");
                }
                Assert.IsTrue(CaughtEx, "GetWater did not throw ArgumentOutOfRangeException when getting " + minusAmount + " water [i=" + i + "/" + numIterations + "]");
            }
        }

        [TestMethod]
        public void TestReservoirePutWaterIntMax()
        {
            // Arrange
            int wayBig = int.MaxValue;
            Reservoire utReservoire = new Reservoire();

            // Act
            utReservoire.PutWater(wayBig);

            // Assert
            Assert.AreEqual(wayBig, utReservoire.VolumeThrownOut, "VolumeThrownOut isn't int.MaxValue when putting int.MaxValue water");
        }

        [TestMethod]
        public void TestReservoirePutWaterGreaterThanIntMax()
        {
            // Arrange
            int putAmount = 1,
                wayBig = int.MaxValue;
            Reservoire utReservoire = new Reservoire();


            // Act
            utReservoire.PutWater(putAmount);
            int putVolume = utReservoire.VolumeThrownOut;
            try
            {
                utReservoire.PutWater(wayBig);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
            }
            catch (Exception ex)
            {
                Assert.Fail("PutWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when VolumeThrownOut == " + putVolume + " && put amount == " + wayBig);
            }
            Assert.IsTrue(CaughtEx, "PutWater did not throw ArgumentOutOfRangeException when VolumeThrownOut == " + putVolume + " && put amount == " + wayBig);
        }

        [TestMethod]
        public void TestReservoirePutWaterRandomLargeInts()
        {
            // Arrange
            int numIterations = Rand.Next(5000, 10000);

            for (int i = 0; i < numIterations; i++)
            {
                Reservoire utReservoire = new Reservoire();

                // Act
                int randWayBig = Rand.Next(1000000, int.MaxValue - 1);
                utReservoire.PutWater(randWayBig);

                // Assert
                Assert.AreEqual(randWayBig, utReservoire.VolumeThrownOut, "VolumeThrownOut isn't random way big (" + randWayBig + ") when putting random way big (" + randWayBig + ") water [i=" + i + "/" + numIterations + "]");
            }
        }

        [TestMethod]
        public void TestReservoirePutWaterZero()
        {
            // Arrange
            int zeroPut = 0;
            Reservoire utReservoire = new Reservoire();

            // Act
            try
            {
                utReservoire.PutWater(zeroPut);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
            }
            catch (Exception ex)
            {
                Assert.Fail("PutWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when putting amount == 0");
            }
            Assert.IsTrue(CaughtEx, "PutWater did not throw ArgumentOutOfRangeException when putting 0 water");
        }

        [TestMethod]
        public void TestReservoirePutWaterMinusOne()
        {
            // Arrange
            int minusPut = -1;
            Reservoire utReservoire = new Reservoire();

            // Act
            try
            {
                utReservoire.PutWater(minusPut);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
            }
            catch (Exception ex)
            {
                Assert.Fail("PutWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when putting amount == -1");
            }
            Assert.IsTrue(CaughtEx, "PutWater did not throw ArgumentOutOfRangeException when putting -1 water");
        }

        [TestMethod]
        public void TestReservoirePutWaterIntMin()
        {
            // Arrange
            int minusPut = int.MinValue;
            Reservoire utReservoire = new Reservoire();
            
            // Act
            try
            {
                utReservoire.PutWater(minusPut);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter");
            }
            catch (Exception ex)
            {
                Assert.Fail("PutWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when putting amount == int.MinValue");
            }
            Assert.IsTrue(CaughtEx, "PutWater did not throw ArgumentOutOfRangeException when putting int.MinValue water");
        }

        [TestMethod]
        public void TestReservoirePutWaterRandomNegatives()
        {
            // Arrange
            int numIterations = Rand.Next(5000, 10000), lowest = int.MinValue + 1;
            Reservoire utReservoire = new Reservoire();

            for (int i = 0; i < numIterations; i++)
            {
                CaughtEx = false;
                int minusAmount = Rand.Next(lowest, -2);
                // Act
                try
                {
                    utReservoire.PutWater(minusAmount);
                }
                // Assert
                catch (ArgumentOutOfRangeException ex)
                {
                    CaughtEx = true;
                    Assert.AreEqual("amount", ex.ParamName, "ArgumentOutOfRangeException didn't report 'amount' parameter [i=" + i + "/" + numIterations + "]");
                }
                catch (Exception ex)
                {
                    Assert.Fail("PutWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when putting amount == " + minusAmount + " [i=" + i + "/" + numIterations + "]");
                }
                Assert.IsTrue(CaughtEx, "PutWater did not throw ArgumentOutOfRangeException when putting " + minusAmount + " water [i=" + i + "/" + numIterations + "]");
            }
        }

        [TestMethod]
        public void TestReservoireGetThenPutWater()
        {
            // Arrange
            int numAmounts = Rand.Next(5000, 10000), getIterations = 0, putIterations = 0;
            int[] getAmounts = new int[numAmounts], gotTotals = new int[numAmounts],
                  putAmounts = new int[numAmounts], putTotals = new int[numAmounts];
            int getAmount = 0;
            long longGetTotal = 0;
            for (int a = 0; a < numAmounts; a++)
            {
                getAmount = Rand.Next(1, 30000);
                longGetTotal += getAmount;
                if ((longGetTotal >> 32) > 0)
                {
                    getIterations = a;
                    break;
                }
                getAmounts[a] = getAmount;
                gotTotals[a] = (int)longGetTotal;
            }
            if (getIterations == 0)
            {
                getIterations = numAmounts;
                getAmount = Rand.Next(int.MaxValue - gotTotals[gotTotals.Length - 1] + 2, int.MaxValue - 1);
                longGetTotal += getAmount;
            }
            int putAmount = 0;
            long longPutTotal = 0;
            for (int a = 0; a < numAmounts; a++)
            {
                putAmount = Rand.Next(1, 30000);
                longPutTotal += putAmount;
                if ((longPutTotal >> 32) > 0)
                {
                    putIterations = a;
                    break;
                }
                putAmounts[a] = putAmount;
                putTotals[a] = (int)longPutTotal;
            }
            if (putIterations == 0)
            {
                putIterations = numAmounts;
                putAmount = Rand.Next(int.MaxValue - putTotals[putTotals.Length - 1] + 2, int.MaxValue - 1);
                longPutTotal += putAmount;
            }
            Reservoire utReservoire = new Reservoire();

            int amountGotten = 0;
            int initIterations = Math.Min(getIterations, putIterations);
            int finishIterations = Math.Max(getIterations, putIterations) - initIterations;
            for (int i = 0; i < initIterations; i++)
            {
                // Act
                amountGotten = utReservoire.GetWater(getAmounts[i]);
                utReservoire.PutWater(putAmounts[i]);

                // Assert
                Assert.AreEqual(getAmounts[i], amountGotten, "GetWater returned different amount that requested [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.AreEqual(gotTotals[i], utReservoire.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.AreEqual(putTotals[i], utReservoire.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
            }
            if (initIterations < getIterations)
            {
                for (int i = initIterations; i < finishIterations; i++)
                {
                    // Act
                    amountGotten = utReservoire.GetWater(getAmounts[i]);

                    // Assert
                    Assert.AreEqual(getAmounts[i], amountGotten, "GetWater returned different amount that requested [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(gotTotals[i], utReservoire.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(putTotals[i], utReservoire.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                }
            }
            else if (initIterations < putIterations)
            {
                for (int i = initIterations; i < finishIterations; i++)
                {
                    // Act
                    utReservoire.PutWater(putAmounts[i]);

                    // Assert
                    Assert.AreEqual(gotTotals[i], utReservoire.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(putTotals[i], utReservoire.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                }
            }
            int noGet = -1;
            try
            {
                // Act
                noGet = utReservoire.GetWater(getAmount);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "GetWater ArgumentOutOfRangeException didn't report 'amount' parameter");
                Assert.AreEqual(-1, noGet, "Reservoire GetWater returned value == " + noGet + " even though ArgumentOutOfRangeException was thrown for getting too much water");
            }
            catch (Exception ex)
            {
                Assert.Fail("GetWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when VolumeUsed == " + gotTotals[getIterations - 1] + " && get amount == " + getAmount);
            }
            Assert.IsTrue(CaughtEx, "GetWater did not throw ArgumentOutOfRangeException when VolumeUsed == " + gotTotals[getIterations - 1] + " && get amount == " + getAmount);

            // Arrange
            CaughtEx = false;

            try
            {
                // Act
                utReservoire.PutWater(putAmount);
            }
            // Assert
            catch (ArgumentOutOfRangeException ex)
            {
                CaughtEx = true;
                Assert.AreEqual("amount", ex.ParamName, "PutWater ArgumentOutOfRangeException didn't report 'amount' parameter");
            }
            catch (Exception ex)
            {
                Assert.Fail("PutWater threw a different Exception (" + ex.GetType().Name + ") instead of ArgumentOutOfRangeException when VolumeThrownOut == " + putTotals[putIterations - 1] + " && put amount == " + putAmount);
            }
            Assert.IsTrue(CaughtEx, "PutWater did not throw ArgumentOutOfRangeException when VolumeUsed == " + putTotals[putIterations - 1] + " && put amount == " + putAmount);
        }

        [TestMethod]
        public void TestReservoireCurrentState()
        {
            // Arrange
            int numAmounts = Rand.Next(5000, 10000), getIterations = 0, putIterations = 0;
            int[] getAmounts = new int[numAmounts], gotTotals = new int[numAmounts],
                  putAmounts = new int[numAmounts], putTotals = new int[numAmounts];
            int getAmount = 0;
            long longGetTotal = 0;
            for (int a = 0; a < numAmounts; a++)
            {
                getAmount = Rand.Next(1, 30000);
                longGetTotal += getAmount;
                if ((longGetTotal >> 32) > 0)
                {
                    getIterations = a;
                    break;
                }
                getAmounts[a] = getAmount;
                gotTotals[a] = (int)longGetTotal;
            }
            if (getIterations == 0)
            {
                getIterations = numAmounts;
                getAmount = Rand.Next(int.MaxValue - gotTotals[gotTotals.Length - 1] + 2, int.MaxValue - 1);
                longGetTotal += getAmount;
            }
            int putAmount = 0;
            long longPutTotal = 0;
            for (int a = 0; a < numAmounts; a++)
            {
                putAmount = Rand.Next(1, 30000);
                longPutTotal += putAmount;
                if ((longPutTotal >> 32) > 0)
                {
                    putIterations = a;
                    break;
                }
                putAmounts[a] = putAmount;
                putTotals[a] = (int)longPutTotal;
            }
            if (putIterations == 0)
            {
                putIterations = numAmounts;
                putAmount = Rand.Next(int.MaxValue - putTotals[putTotals.Length - 1] + 2, int.MaxValue - 1);
                longPutTotal += putAmount;
            }
            Reservoire utReservoire = new Reservoire(), utReservoireFromState = null;
            ReservoireState utStateInitial = null, utState = null;

            // Act
            utStateInitial = utReservoire.CurrentState;
            utReservoireFromState = new Reservoire(utStateInitial);
            // Assert
            Assert.AreEqual(0, utStateInitial.VolumeUsed, "CurrentState from empty constructed initial Reservoire VolumeUsed not equal to 0");
            Assert.AreEqual(0, utStateInitial.VolumeThrownOut, "CurrentState from empty constructed initial Reservoire VolumeThrownOut not equal to 0");
            Assert.AreEqual(0, utReservoireFromState.VolumeUsed, "ReservoireState constructor from empty constructed initial Reservoire VolumeUsed not equal to 0");
            Assert.AreEqual(0, utReservoireFromState.VolumeThrownOut, "ReservoireState constructor from empty constructed initial Reservoire VolumeThrownOut not equal to 0");

            // Arrange
            int initIterations = Math.Min(getIterations, putIterations);
            int finishIterations = Math.Max(getIterations, putIterations) - initIterations;
            for (int i = 0; i < initIterations; i++)
            {
                // Arrange
                utState = null;
                utReservoireFromState = null;

                // Act
                utReservoire.GetWater(getAmounts[i]);
                utReservoire.PutWater(putAmounts[i]);
                utState = utReservoire.CurrentState;
                utReservoireFromState = new Reservoire(utState);

                // Assert
                Assert.AreEqual(gotTotals[i], utReservoire.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.AreEqual(putTotals[i], utReservoire.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.IsNotNull(utState, "CurrentState returned null [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.AreEqual(utReservoire.VolumeUsed, utState.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.AreEqual(utReservoire.VolumeThrownOut, utState.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.IsNotNull(utReservoireFromState, "Constructing a new Reservoire from ReservoireState returned null (should be impossible with new call) [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.AreEqual(utReservoire.VolumeUsed, utReservoireFromState.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
                Assert.AreEqual(utReservoire.VolumeThrownOut, utReservoireFromState.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + initIterations + ":" + finishIterations + "]");
            }
            if (initIterations < getIterations)
            {
                for (int i = initIterations; i < finishIterations; i++)
                {
                    // Arrange
                    utState = null;
                    utReservoireFromState = null;

                    // Act
                    utReservoire.GetWater(getAmounts[i]);
                    utState = utReservoire.CurrentState;
                    utReservoireFromState = new Reservoire(utState);

                    // Assert
                    Assert.AreEqual(gotTotals[i], utReservoire.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(putTotals[i], utReservoire.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.IsNotNull(utState, "CurrentState returned null");
                    Assert.AreEqual(utReservoire.VolumeUsed, utState.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(utReservoire.VolumeThrownOut, utState.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.IsNotNull(utReservoireFromState, "Constructing a new Reservoire from ReservoireState returned null (should be impossible with new call)");
                    Assert.AreEqual(utReservoire.VolumeUsed, utReservoireFromState.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(utReservoire.VolumeThrownOut, utReservoireFromState.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                }
            }
            else if (initIterations < putIterations)
            {
                for (int i = initIterations; i < finishIterations; i++)
                {
                    // Arrange
                    utState = null;
                    utReservoireFromState = null;

                    // Act
                    utReservoire.PutWater(putAmounts[i]);
                    utState = utReservoire.CurrentState;
                    utReservoireFromState = new Reservoire(utState);

                    // Assert
                    Assert.AreEqual(gotTotals[i], utReservoire.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(putTotals[i], utReservoire.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.IsNotNull(utState, "CurrentState returned null");
                    Assert.AreEqual(utReservoire.VolumeUsed, utState.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(utReservoire.VolumeThrownOut, utState.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.IsNotNull(utReservoireFromState, "Constructing a new Reservoire from ReservoireState returned null (should be impossible with new call)");
                    Assert.AreEqual(utReservoire.VolumeUsed, utReservoireFromState.VolumeUsed, "Current VolumeUsed not equal to expected total [i=" + i + "/" + finishIterations + "]");
                    Assert.AreEqual(utReservoire.VolumeThrownOut, utReservoireFromState.VolumeThrownOut, "Current VolumeThrownOut not equal to expected total [i=" + i + "/" + finishIterations + "]");
                }
            }
        }
    }
}
