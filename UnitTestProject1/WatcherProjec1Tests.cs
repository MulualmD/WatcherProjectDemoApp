using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using WatcherProject1;

namespace UnitTestProject1
{
    [TestFixture]
    public class WatcherProjec1Tests
    {


        [Test]
        public void TestMethod1()
        {
          
            NUnit.Framework.Assert.AreEqual(8, Program.MsBuild().ExitCode);
        }
    }
}
