﻿// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.

#if SERVER
namespace Microsoft.Synchronization.Services
#elif CLIENT
namespace Microsoft.Synchronization.ClientServices
#endif
{
    /// <summary>
    /// Represents the base interface that all offline cacheable object should derive from.
    /// </summary>
    public interface IOfflineEntity
    {
        /// <summary>
        /// Represents the sync and OData metadata used for the entity
        /// </summary>
        OfflineEntityMetadata ServiceMetadata { get; set; }
    }
}

