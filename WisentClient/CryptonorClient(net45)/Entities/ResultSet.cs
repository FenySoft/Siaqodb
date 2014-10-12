﻿using Cryptonor;
using Sqo;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CryptonorClient
{
    public class ResultSet
    {
        public int Total { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public int Count { get; set; }
        public List<CryptonorObject> Objects { get; set; }
        public List<T> GetValues<T>()
        { 
            List<T> list = new List<T>();
            foreach (CryptonorObject current in Objects)
            {
                list.Add(current.GetValue<T>());
            }
            return list;
        }
    }
    public class WriteResponse
    {
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
        public string ErrorDesc { get; set; }
        public string Version { get; set; }
        public string Key { get; set; }

    }
    public class BatchResponse
    {

        public List<WriteResponse> WriteResponses { get; set; }
        public bool IsSuccess { get; set; }
    }
}