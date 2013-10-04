using System.Web;
using System.Web.Mvc;

namespace MVC4_Foundation3_Lucene_Search
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}