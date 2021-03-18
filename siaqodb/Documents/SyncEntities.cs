using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Sqo.Documents.Sync
{
    
    public class ChangeSet
    {
        public List<Document> ChangedDocuments { get; set; }
        public List<DeletedDocument> DeletedDocuments { get; set; }
        public string Anchor { get; set; }
    }
    
    public class DeletedDocument
    {
        public string Key { get; set; }
        public string Version { get; set; }
    }
}
