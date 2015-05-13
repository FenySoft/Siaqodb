﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Sqo;
using Sqo.Core;
using Sqo.Meta;
using Sqo.Exceptions;
using Sqo.Utilities;
using System.Collections;
using System.Reflection;
using LightningDB;

#if ASYNC
using System.Threading.Tasks;
#endif

namespace Sqo.Core
{
    partial class ObjectSerializer
    {

       
        public byte[] SerializeObject(ObjectInfo oi, RawdataSerializer rawSerializer,LightningTransaction transaction)
        {

            if (oi.Oid > oi.SqoTypeInfo.Header.numberOfRecords)
            {
                oi.Oid = 0;
            }
            if (oi.Oid == 0)
            {

                oi.Oid = GetNextOID(oi.SqoTypeInfo);
                //SaveOID(oi.SqoTypeInfo, oi.Oid);
                oi.SqoTypeInfo.Header.numberOfRecords++; //it is needed here if exists a nested object of same type

                oi.Inserted = true;
            }
            else if (oi.Oid < 0)
            {
                throw new SiaqodbException("Object is already deleted from database");
            }

            byte[] buffer = GetObjectBytes(oi, rawSerializer, transaction);

            return buffer;


        }
#if ASYNC
        public async Task SerializeObjectAsync(ObjectInfo oi, RawdataSerializer rawSerializer)
        {

            if (oi.Oid > oi.SqoTypeInfo.Header.numberOfRecords)
            {
                oi.Oid = 0;
            }
            if (oi.Oid == 0)
            {

                oi.Oid = GetNextOID(oi.SqoTypeInfo);
                //SaveOID(oi.SqoTypeInfo, oi.Oid);
                oi.SqoTypeInfo.Header.numberOfRecords++; //it is needed here if exists a nested object of same type

                oi.Inserted = true;
            }
            else if (oi.Oid < 0)
            {
                throw new SiaqodbException("Object is already deleted from database");
            }

            long position = MetaHelper.GetSeekPosition(oi.SqoTypeInfo, oi.Oid);

            byte[] buffer = await GetObjectBytesAsync(oi, rawSerializer).ConfigureAwait(false);

            await file.WriteAsync(position, buffer).ConfigureAwait(false);

            if (oi.Inserted)
            {
                await SaveNrRecordsAsync(oi.SqoTypeInfo, oi.SqoTypeInfo.Header.numberOfRecords).ConfigureAwait(false);
            }


        }
#endif
        public int SerializeObjectWithNewOID(byte[] objectData, SqoTypeInfo ti)
        {
            return -1;
            //TODO LMDB
            /* int oid = GetNextOID(ti);
            byte[] oidBuff = ByteConverter.IntToByteArray(oid);
            Array.Copy(oidBuff, 0, objectData, 0, oidBuff.Length);
           
            long position = MetaHelper.GetSeekPosition(ti, oid);

            file.Write(position, objectData);

            SaveNrRecords(ti, ti.Header.numberOfRecords + 1);
            return oid;*/

        }
#if ASYNC
        public async Task<int> SerializeObjectWithNewOIDAsync(byte[] objectData, SqoTypeInfo ti)
        {
            int oid = GetNextOID(ti);
            byte[] oidBuff = ByteConverter.IntToByteArray(oid);
            Array.Copy(oidBuff, 0, objectData, 0, oidBuff.Length);

            long position = MetaHelper.GetSeekPosition(ti, oid);

            await file.WriteAsync(position, objectData).ConfigureAwait(false);

            await SaveNrRecordsAsync(ti, ti.Header.numberOfRecords + 1).ConfigureAwait(false);
            return oid;

        }
#endif
        internal byte[] GetObjectBytes(ObjectInfo oi, RawdataSerializer rawSerializer, LightningTransaction transaction)
        {

            byte[] oidBuff = ByteConverter.IntToByteArray(oi.Oid);
            byte[] buffer = new byte[oi.SqoTypeInfo.Header.lengthOfRecord];

            int curentIndex = 0;
            Array.Copy(oidBuff, 0, buffer, curentIndex, oidBuff.Length);
            curentIndex += oidBuff.Length;

            bool oidToParentSet = false;

            foreach (FieldSqoInfo ai in oi.AtInfo.Keys)
            {
                byte[] by = null;
                if (ai.AttributeTypeId == MetaExtractor.complexID || ai.AttributeTypeId == (MetaExtractor.ArrayTypeIDExtra + MetaExtractor.complexID) || ai.AttributeTypeId==MetaExtractor.documentID)
                {
                    //to be able to cache for circular reference, we need to asign OID to it
                    if (!oidToParentSet)
                    {
                        //just set OID to parentObject, do not save anything
                        ComplexObjectEventArgs args = new ComplexObjectEventArgs(true, oi);
                        this.OnNeedSaveComplexObject(args);
            
                        oidToParentSet = true;
                    }

                }
                
                IByteTransformer byteTransformer = ByteTransformerFactory.GetByteTransformer(this, rawSerializer, ai, oi.SqoTypeInfo, oi.Oid);

                by = byteTransformer.GetBytes(oi.AtInfo[ai],transaction);

                Array.Copy(by, 0, buffer, curentIndex, by.Length);
                curentIndex += by.Length;


            }
            return buffer;
        }
#if ASYNC
        internal async Task<byte[]> GetObjectBytesAsync(ObjectInfo oi, RawdataSerializer rawSerializer)
        {

            byte[] oidBuff = ByteConverter.IntToByteArray(oi.Oid);
            byte[] buffer = new byte[oi.SqoTypeInfo.Header.lengthOfRecord];

            int curentIndex = 0;
            Array.Copy(oidBuff, 0, buffer, curentIndex, oidBuff.Length);
            curentIndex += oidBuff.Length;

            bool oidToParentSet = false;

            foreach (FieldSqoInfo ai in oi.AtInfo.Keys)
            {
                byte[] by = null;
                if (ai.AttributeTypeId == MetaExtractor.complexID || ai.AttributeTypeId == (MetaExtractor.ArrayTypeIDExtra + MetaExtractor.complexID) || ai.AttributeTypeId == MetaExtractor.documentID)
                {
                    //to be able to cache for circular reference, we need to asign OID to it
                    if (!oidToParentSet)
                    {
                        //just set OID to parentObject, do not save anything
                        ComplexObjectEventArgs args = new ComplexObjectEventArgs(true, oi);
                        await this.OnNeedSaveComplexObjectAsync(args).ConfigureAwait(false);

                        oidToParentSet = true;
                    }

                }
                int parentOID = -1;
                if (!oi.Inserted)
                {
                    parentOID = oi.Oid;
                }
                IByteTransformer byteTransformer = ByteTransformerFactory.GetByteTransformer(this, rawSerializer, ai, oi.SqoTypeInfo, parentOID);

                by = await byteTransformer.GetBytesAsync(oi.AtInfo[ai]).ConfigureAwait(false);

                Array.Copy(by, 0, buffer, curentIndex, by.Length);
                curentIndex += by.Length;


            }
            return buffer;
        }
        
#endif
        public byte[] GetComplexObjectBytes(object obj, bool returnOnlyOID_TID, LightningTransaction transaction)
        {
            ComplexObjectEventArgs args = new ComplexObjectEventArgs(obj, returnOnlyOID_TID, transaction);
            this.OnNeedSaveComplexObject(args);
            byte[] by = new byte[MetaExtractor.GetAbsoluteSizeOfField(MetaExtractor.complexID)];
            byte[] complexOID = ByteConverter.IntToByteArray(args.SavedOID);
            byte[] complexTID = ByteConverter.IntToByteArray(args.TID);
            Array.Copy(complexOID, 0, by, 0, complexOID.Length);
            Array.Copy(complexTID, 0, by, 4, complexTID.Length);
            return by;
        }
#if ASYNC
        public async Task<byte[]> GetComplexObjectBytesAsync(object obj, bool returnOnlyOID_TID)
        {
            ComplexObjectEventArgs args = new ComplexObjectEventArgs(obj, returnOnlyOID_TID);
            await this.OnNeedSaveComplexObjectAsync(args).ConfigureAwait(false);
            byte[] by = new byte[MetaExtractor.GetAbsoluteSizeOfField(MetaExtractor.complexID)];
            byte[] complexOID = ByteConverter.IntToByteArray(args.SavedOID);
            byte[] complexTID = ByteConverter.IntToByteArray(args.TID);
            Array.Copy(complexOID, 0, by, 0, complexOID.Length);
            Array.Copy(complexTID, 0, by, 4, complexTID.Length);
            return by;
        }
#endif
        public byte[] GetComplexObjectBytes(object obj,LightningTransaction transaction)
        {
            return this.GetComplexObjectBytes(obj, false,transaction);
        }
#if ASYNC
        public async Task<byte[]> GetComplexObjectBytesAsync(object obj)
        {
            return await this.GetComplexObjectBytesAsync(obj, false).ConfigureAwait(false);
        }
#endif
        private int GetNextOID(SqoTypeInfo typeInfo)
        {

            return typeInfo.Header.numberOfRecords + 1;
        }
        
        internal byte[] MarkObjectAsDelete(int oid, SqoTypeInfo ti)
        {

            int deletedOID = (-1) * oid;
            byte[] deletedOidBuff = ByteConverter.IntToByteArray(deletedOID);
            return deletedOidBuff;
           
        }
#if ASYNC
        internal async Task MarkObjectAsDeleteAsync(int oid, SqoTypeInfo ti)
        {

            long position = MetaHelper.GetSeekPosition(ti, oid);
            int deletedOID = (-1) * oid;
            byte[] deletedOidBuff = ByteConverter.IntToByteArray(deletedOID);

            await file.WriteAsync(position, deletedOidBuff).ConfigureAwait(false);

        }
#endif
       
        internal ATuple<int,byte[]> SaveFieldValue(int oid, string field, SqoTypeInfo ti, object value, RawdataSerializer rawSerializer,LightningTransaction transaction)
        {
           
            long position = MetaHelper.GetSeekPosition(ti, oid);
            int recordLength = ti.Header.lengthOfRecord;
            FieldSqoInfo ai = FindField(ti.Fields, field);
            if (ai == null)
            {
                throw new SiaqodbException("Field:" + field + " not exists in the Type Definition, if you use a Property you have to use UseVariable Attribute");
            }
            else if (value != null && ai.AttributeType != value.GetType())
            {
                try
                {

                    object valConvert = Convertor.ChangeType(value, ai.AttributeType);

                    value = valConvert;
                }
                catch
                {
                    string msg="Type of value should be:" + ai.AttributeType.ToString();
                    SiaqodbConfigurator.LogMessage(msg,VerboseLevel.Error);
                    throw new SiaqodbException(msg);
                }
            }
            byte[] by = null;
            
            IByteTransformer byteTransformer = ByteTransformerFactory.GetByteTransformer(this, rawSerializer, ai, ti, oid);
            by = byteTransformer.GetBytes(value,transaction);

            return new ATuple<int, byte[]>(ai.Header.PositionInRecord, by);
            


        }

#if ASYNC
        internal async Task<bool> SaveFieldValueAsync(int oid, string field, SqoTypeInfo ti, object value, RawdataSerializer rawSerializer)
        {

            long position = MetaHelper.GetSeekPosition(ti, oid);
            int recordLength = ti.Header.lengthOfRecord;
            FieldSqoInfo ai = FindField(ti.Fields, field);
            if (ai == null)
            {
                throw new SiaqodbException("Field:" + field + " not exists in the Type Definition, if you use a Property you have to use UseVariable Attribute");
            }
            else if (value != null && ai.AttributeType != value.GetType())
            {
                try
                {

                    object valConvert = Convertor.ChangeType(value, ai.AttributeType);

                    value = valConvert;
                }
                catch
                {
                    string msg = "Type of value should be:" + ai.AttributeType.ToString();
                    SiaqodbConfigurator.LogMessage(msg, VerboseLevel.Error);
                    throw new SiaqodbException(msg);
                }
            }
            byte[] by = null;

            IByteTransformer byteTransformer = ByteTransformerFactory.GetByteTransformer(this, rawSerializer, ai, ti, oid);
            by = await byteTransformer.GetBytesAsync(value).ConfigureAwait(false);

            await file.WriteAsync((long)(position + (long)ai.Header.PositionInRecord), by).ConfigureAwait(false);

            return true;


        }
#endif

        internal ATuple<int,byte[]> InsertEmptyObject(SqoTypeInfo tinf)
        {
            int oid = GetNextOID(tinf);
            byte[] objBytes = new byte[tinf.Header.lengthOfRecord];
            byte[] oidBuff = ByteConverter.IntToByteArray(oid);
            Array.Copy(oidBuff, 0, objBytes, 0, oidBuff.Length);
            tinf.Header.numberOfRecords++;

            return new ATuple<int,byte[]>(oid,objBytes);
        }

        internal void SaveObjectTable(SqoTypeInfo actualTypeinfo, SqoTypeInfo oldSqoTypeInfo, ObjectTable table, RawdataSerializer rawSerializer,LightningDatabase db,LightningTransaction transaction)
        {
            //TODO LMDB
            
            Dictionary<FieldSqoInfo, FieldSqoInfo> joinedFields = JoinFieldsSqoInfo(actualTypeinfo, oldSqoTypeInfo);

            foreach (ObjectRow row in table.Rows)
            {
                int oid = (int)row["OID"];
               
                byte[] oidBuff = ByteConverter.IntToByteArray(oid);
                byte[] buffer = new byte[actualTypeinfo.Header.lengthOfRecord];

                int curentIndex = 0;
                Array.Copy(oidBuff, 0, buffer, curentIndex, oidBuff.Length);
                curentIndex += oidBuff.Length;
                var arrayDbName = string.Format("raw.{0}", actualTypeinfo.GetDBName());
                foreach (FieldSqoInfo ai in actualTypeinfo.Fields)
                {
                    byte[] by = null;

                    object fieldVal = null;
                    bool existed = false;
                    if (table.Columns.ContainsKey(ai.Name))
                    {
                        fieldVal = row[ai.Name];
                        existed = true;
                    }
                    else
                    {
                        if (ai.AttributeTypeId == MetaExtractor.complexID || ai.AttributeTypeId==MetaExtractor.documentID)
                        {
                            fieldVal = null;
                        }
                        else if (typeof(string) == ai.AttributeType)
                        {
                            fieldVal = string.Empty;
                        }
                        else if (ai.AttributeType.IsArray)
                        {
                            fieldVal = Array.CreateInstance(ai.AttributeType.GetElementType(), 0);
                        }
                        else
                        {
                            fieldVal = Activator.CreateInstance(ai.AttributeType);
                        }

                    }
                    if (joinedFields[ai] != null) //existed in old Type
                    {
                        if (ai.AttributeTypeId != joinedFields[ai].AttributeTypeId )
                        {
                            if (typeof(IList).IsAssignableFrom(ai.AttributeType) || ai.AttributeTypeId == MetaExtractor.dictionaryID || joinedFields[ai].AttributeTypeId==MetaExtractor.dictionaryID)
                            {
                                throw new TypeChangedException("Change array or dictionary type it is not supported");
                            }
                            else
                            {
                                fieldVal = Convertor.ChangeType(fieldVal, ai.AttributeType);
                            }
                        }
                    }
                    if (ai.AttributeTypeId == MetaExtractor.complexID || ai.AttributeTypeId==MetaExtractor.documentID)
                    {
                        if (existed)
                        {
                            by = (byte[])fieldVal;
                        }
                        else
                        {
                            by = this.GetComplexObjectBytes(fieldVal,null);
                        }
                    }
                    else if (typeof(IList).IsAssignableFrom(ai.AttributeType))//array
                    {
                        if (existed)
                        {
                            by = (byte[])fieldVal;
                        }
                        else
                        {
                            by = rawSerializer.SerializeArray(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version, arrayDbName, "", this, ai.IsText, oid, transaction);
                        }
                    }
                    else if (ai.IsText)
                    {
                        if (existed)
                        {
                            FieldSqoInfo oldAi = joinedFields[ai];
                            if (oldAi != null && oldAi.IsText)
                            {
                                by = (byte[])fieldVal;
                            }
                            else
                            {
                                by = rawSerializer.SerializeArray(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version, arrayDbName, "", this, ai.IsText, oid, transaction);
                            }
                        }
                        else
                        {
                            by = rawSerializer.SerializeArray(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version, arrayDbName, "", this, ai.IsText, oid, transaction);
                        }
                    }
                    else if (ai.AttributeTypeId == MetaExtractor.dictionaryID)
                    {

                        if (existed)
                        {
                            by = (byte[])fieldVal;
                        }
                        else
                        {
                            IByteTransformer byteTransformer = ByteTransformerFactory.GetByteTransformer(this, rawSerializer, ai, actualTypeinfo, 0);
                            by = byteTransformer.GetBytes(fieldVal,null);
                        }
                    }
                    else
                    {
                        by = ByteConverter.SerializeValueType(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version);
                    }
                    Array.Copy(by, 0, buffer, ai.Header.PositionInRecord, ai.Header.Length);
                    curentIndex += by.Length;
                }
                transaction.Put(db, oidBuff, buffer);
            }
            CleanOldRemovedFields(actualTypeinfo, oldSqoTypeInfo,table,rawSerializer,transaction);
        }

        private void CleanOldRemovedFields(SqoTypeInfo actualTypeinfo, SqoTypeInfo oldSqoTypeInfo,ObjectTable table,RawdataSerializer rawSerializer,LightningTransaction transaction)
        {
            List<FieldSqoInfo> oldRemovedFields = RemovedFieldsSqu(actualTypeinfo, oldSqoTypeInfo);
                var arrayDbName = string.Format("raw.{0}", oldSqoTypeInfo.GetDBName());
            foreach(var oi in oldRemovedFields){
                foreach (ObjectRow row in table.Rows)
                {
                    int oid = (int)row["OID"];
                    if (oi.AttributeTypeId == MetaExtractor.complexID || oi.AttributeTypeId == MetaExtractor.documentID)
                    {
                         // do nothing for the moment
                    }
                    else if (oi.AttributeTypeId == (MetaExtractor.ArrayTypeIDExtra + MetaExtractor.complexID) ||
                        oi.AttributeTypeId == MetaExtractor.ArrayTypeIDExtra + MetaExtractor.jaggedArrayID || oi.IsText 
                        || oi.AttributeTypeId == MetaExtractor.dictionaryID)//array text or dictionary
                    {
                        rawSerializer.DeleteRawRecord(oid,arrayDbName,oi.Name,transaction);
                    }
                }
            }
        }

        private List<FieldSqoInfo> RemovedFieldsSqu(SqoTypeInfo actualTypeinfo, SqoTypeInfo oldSqoTypeInfo)
        {
            List<FieldSqoInfo> fields = new List<FieldSqoInfo>();
            foreach (FieldSqoInfo fi in oldSqoTypeInfo.Fields)
            {
                FieldSqoInfo presentField = MetaHelper.FindField(actualTypeinfo.Fields, fi.Name);
                if(presentField == null){
                    fields.Add(fi);
                }
            }
            return fields;
        }
        
#if ASYNC
        internal async Task SaveObjectTableAsync(SqoTypeInfo actualTypeinfo, SqoTypeInfo oldSqoTypeInfo, ObjectTable table, RawdataSerializer rawSerializer)
        {
            Dictionary<FieldSqoInfo, FieldSqoInfo> joinedFields = JoinFieldsSqoInfo(actualTypeinfo, oldSqoTypeInfo);

            foreach (ObjectRow row in table.Rows)
            {
                int oid = (int)row["OID"];
                if (oid < 0)//deleted
                {
                    await this.MarkObjectAsDeleteAsync(-oid, actualTypeinfo).ConfigureAwait(false);
                    continue;
                }
                byte[] oidBuff = ByteConverter.IntToByteArray(oid);
                byte[] buffer = new byte[actualTypeinfo.Header.lengthOfRecord];

                int curentIndex = 0;
                Array.Copy(oidBuff, 0, buffer, curentIndex, oidBuff.Length);
                curentIndex += oidBuff.Length;
                foreach (FieldSqoInfo ai in actualTypeinfo.Fields)
                {
                    byte[] by = null;


                    object fieldVal = null;
                    bool existed = false;
                    if (table.Columns.ContainsKey(ai.Name))
                    {
                        fieldVal = row[ai.Name];
                        existed = true;
                    }
                    else
                    {
                        if (ai.AttributeTypeId == MetaExtractor.complexID || ai.AttributeTypeId == MetaExtractor.documentID)
                        {
                            fieldVal = null;
                        }
                        else if (typeof(string) == ai.AttributeType)
                        {
                            fieldVal = string.Empty;
                        }
                        else if (ai.AttributeType.IsArray)
                        {
                            fieldVal = Array.CreateInstance(ai.AttributeType.GetElementType(), 0);
                        }
                        else
                        {
                            fieldVal = Activator.CreateInstance(ai.AttributeType);
                        }

                    }
                    if (joinedFields[ai] != null) //existed in old Type
                    {
                        if (ai.AttributeTypeId != joinedFields[ai].AttributeTypeId)
                        {
                            if (typeof(IList).IsAssignableFrom(ai.AttributeType) || ai.AttributeTypeId == MetaExtractor.dictionaryID || joinedFields[ai].AttributeTypeId == MetaExtractor.dictionaryID)
                            {
                                throw new TypeChangedException("Change array or dictionary type it is not supported");
                            }
                            else
                            {
                                fieldVal = Convertor.ChangeType(fieldVal, ai.AttributeType);
                            }
                        }
                    }
                    if (ai.AttributeTypeId == MetaExtractor.complexID || ai.AttributeTypeId == MetaExtractor.documentID)
                    {
                        if (existed)
                        {
                            by = (byte[])fieldVal;
                        }
                        else
                        {
                            by = await this.GetComplexObjectBytesAsync(fieldVal).ConfigureAwait(false);
                        }
                    }
                    else if (typeof(IList).IsAssignableFrom(ai.AttributeType))//array
                    {
                        if (existed)
                        {
                            by = (byte[])fieldVal;
                        }
                        else
                        {

                            by = await rawSerializer.SerializeArrayAsync(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version, null, this, ai.IsText).ConfigureAwait(false);

                        }
                    }
                    else if (ai.IsText)
                    {
                        if (existed)
                        {
                            FieldSqoInfo oldAi = joinedFields[ai];
                            if (oldAi != null && oldAi.IsText)
                            {
                                by = (byte[])fieldVal;
                            }
                            else
                            {
                                by = await rawSerializer.SerializeArrayAsync(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version, null, this, ai.IsText).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            by = await rawSerializer.SerializeArrayAsync(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version, null, this, ai.IsText).ConfigureAwait(false);

                        }
                    }
                    else if (ai.AttributeTypeId == MetaExtractor.dictionaryID)
                    {

                        if (existed)
                        {
                            by = (byte[])fieldVal;
                        }
                        else
                        {
                            IByteTransformer byteTransformer = ByteTransformerFactory.GetByteTransformer(this, rawSerializer, ai, actualTypeinfo, 0);
                            by = await byteTransformer.GetBytesAsync(fieldVal).ConfigureAwait(false);

                        }


                    }
                    else
                    {
                        by = ByteConverter.SerializeValueType(fieldVal, ai.AttributeType, ai.Header.Length, ai.Header.RealLength, actualTypeinfo.Header.version);
                    }
                    Array.Copy(by, 0, buffer, ai.Header.PositionInRecord, ai.Header.Length);
                    //curentIndex += by.Length;


                }
                long position = MetaHelper.GetSeekPosition(actualTypeinfo, oid);

                await file.WriteAsync(position, buffer).ConfigureAwait(false);

            }

        }
        
#endif
        private Dictionary<FieldSqoInfo, FieldSqoInfo> JoinFieldsSqoInfo(SqoTypeInfo actualTypeinfo, SqoTypeInfo oldTypeinfo)
        {
            Dictionary<FieldSqoInfo, FieldSqoInfo> fields = new Dictionary<FieldSqoInfo, FieldSqoInfo>();
            foreach (FieldSqoInfo fi in actualTypeinfo.Fields)
            {
                FieldSqoInfo oldFi = MetaHelper.FindField(oldTypeinfo.Fields, fi.Name);
                fields[fi] = oldFi;
            }
            return fields;
        }
      
        internal void SaveArrayOIDFieldContent(SqoTypeInfo ti, FieldSqoInfo fi, int objectOID, int newOID)
        {
           //TODO LMDB
            /*byte[] arrayOID = ByteConverter.IntToByteArray(newOID);
            long position = MetaHelper.GetSeekPosition(ti, objectOID);
             //an array field has size=9 (isNull(bool) + oid of array table(int)+ nrElements(int)
              //so write oid after first byte which is null/not null
            long writePosition=(long)(position + (long)fi.Header.PositionInRecord+1L);
            file.Write(writePosition, arrayOID);*/
        }

        [System.Reflection.Obfuscation(Exclude = true)]
        private EventHandler<ComplexObjectEventArgs> needSaveComplexObject;
        [System.Reflection.Obfuscation(Exclude = true)]
        public event EventHandler<ComplexObjectEventArgs> NeedSaveComplexObject
        {
            add
            {
                lock (_syncRoot)
                {
                    if (needSaveComplexObject == null)
                    {
                        needSaveComplexObject += value;
                    }
                }
            }
            remove
            {
                lock (_syncRoot)
                {
                    needSaveComplexObject -= value;
                }
            }
        }
#if ASYNC
        [System.Reflection.Obfuscation(Exclude = true)]
        private ComplexObjectEventHandler needSaveComplexObjectAsync;
        [System.Reflection.Obfuscation(Exclude = true)]
        public event ComplexObjectEventHandler NeedSaveComplexObjectAsync
        {
            add
            {
                lock (_syncRoot)
                {
                    if (needSaveComplexObjectAsync == null)
                    {
                        needSaveComplexObjectAsync += value;
                    }
                }
            }
            remove
            {
                lock (_syncRoot)
                {
                    needSaveComplexObjectAsync -= value;
                }
            }
        }
#endif
        protected void OnNeedSaveComplexObject(ComplexObjectEventArgs args)
        {
            EventHandler<ComplexObjectEventArgs> handler;
            lock (_syncRoot)
            {
                handler = needSaveComplexObject;
            }
            if (handler != null)
            {
                handler(this, args);
            }
        }
#if ASYNC
        protected async Task OnNeedSaveComplexObjectAsync(ComplexObjectEventArgs args)
        {
            ComplexObjectEventHandler handler;
            lock (_syncRoot)
            {
                handler = needSaveComplexObjectAsync;
            }
            if (handler != null)
            {
                await handler(this, args).ConfigureAwait(false);
            }
        }
#endif
    }
}
