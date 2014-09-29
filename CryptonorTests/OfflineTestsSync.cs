﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptonorClient;
namespace CryptonorTests
{
    [TestClass]
    public class OfflineTests:BucketTestsSync
    {
        public OfflineTests():base()
        {

        }
        public override CryptonorClient.IBucket GetBucket()
        {

            CryptonorClient.CryptonorClient client = new CryptonorClient.CryptonorClient(Init.Api_URL, Init.Username, Init.Password);
            IBucket bucket = client.GetLocalBucket("unit_tests", @"c:\work\temp\unitests\");
            return bucket;
        }
    }
}