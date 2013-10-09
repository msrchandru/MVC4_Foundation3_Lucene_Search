using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVC4_Foundation3_Lucene_Search.Models
{
    public class IndexingModel
    {
        public string SearchValue { get; set; }
        public string wiithallwords { get; set; }
        public string exactphrase { get; set; }
        public string leastWords { get; set; }
        public string withoutWords { get; set; }



         //public int Id { get; set; }
         //public string Name { get; set; }
         //public string Email { get; set; }
            
    }
}