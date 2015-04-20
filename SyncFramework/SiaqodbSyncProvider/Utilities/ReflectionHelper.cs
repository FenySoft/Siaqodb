﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiaqodbSyncProvider.Utilities
{
    class ReflectionHelper
    {
        public static string GetDiscoveringTypeName(Type type)
        {

            string onlyTypeName = type.Namespace + "." + type.Name;

#if SILVERLIGHT
            string assemblyName = type.Assembly.FullName.Split(',')[0];
#elif NETFX_CORE
            string assemblyName = type.GetTypeInfo().Assembly.GetName().Name;
#else
            string assemblyName = type.Assembly.GetName().Name;
#endif

            return onlyTypeName + ", " + assemblyName;

        }
        public static Type GetTypeByDiscoveringName(string typeName)
        {
#if SILVERLIGHT
            typeName  += ", Version=0.0.0.1,Culture=neutral, PublicKeyToken=null";
#endif
            return Type.GetType(typeName);
        }
    }
}
