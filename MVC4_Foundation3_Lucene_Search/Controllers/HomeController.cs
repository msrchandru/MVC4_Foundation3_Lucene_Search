using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MVC4_Foundation3_Lucene_Search.Models;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using Lucene.Net.Index;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using MVC4_Foundation3_Lucene_Search.Models;
using System.Reflection;
using System.Collections.Generic;

namespace MVC4_Foundation3_Lucene_Search.Controllers
{
    public class HomeController : Controller
    {
  
        string strcon = ConfigurationManager.ConnectionStrings["dbconn"].ConnectionString;
    //    private readonly List<> clients = new List<>()
    //{
    //    new IndexingModel { Id = 1, Name = "Julio Avellaneda", Email = "julito_gtu@hotmail.com" },
    //    new IndexingModel { Id = 2, Name = "Juan Torres", Email = "jtorres@hotmail.com" },
    //    new IndexingModel { Id = 3, Name = "Oscar Camacho", Email = "oscar@hotmail.com" },
    //    new IndexingModel { Id = 4, Name = "Gina Urrego", Email = "ginna@hotmail.com" },
    //    new IndexingModel { Id = 5, Name = "Nathalia Ramirez", Email = "natha@hotmail.com" },
    //    new IndexingModel { Id = 6, Name = "Raul Rodriguez", Email = "rodriguez.raul@hotmail.com" },
    //    new IndexingModel { Id = 7, Name = "Johana Espitia", Email = "johana_espitia@hotmail.com" }
    //};
        /// <summary>
        /// Creating an object to store the searched data
        /// </summary>
        public class SearchResults
        {
            public string PageName { get; set; }
            public string Tag { get; set; }
            public string ContentText { get; set; }
            public int Priority { get; set; }
        }
        /// <summary>
        /// Set value for minimum value for prefix match
        /// </summary>
        public enum MinValue
        {
            MinPrefexvalue = 5
        }

        #region Indexing methods
        // The query fetch all person details
        public DataSet GetPersons()
        {
            String sqlQuery = @"SELECT [PageName],[Tag],[ContentText],[Priority] FROM [dbo].[tblCrawlerData]";

            return GetDataSet(sqlQuery);
        }

        // Returns the dataset
        public DataSet GetDataSet(string sqlQuery)
        {
            DataSet ds = new DataSet();
            SqlConnection sqlCon = new SqlConnection(strcon);
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.Connection = sqlCon;
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.CommandText = sqlQuery;
            SqlDataAdapter sqlAdap = new SqlDataAdapter(sqlCmd);
            sqlAdap.Fill(ds);
            return ds;
        }

        // Creates the lucene.net index with person details
        public void CreatePersonsIndex(DataSet ds)
        {
            //Specify the index file location where the indexes are to be stored
            string indexFileLocation = @"D:\Lucene.Net\Data\Persons";
            Lucene.Net.Store.Directory dir = Lucene.Net.Store.FSDirectory.GetDirectory(indexFileLocation, true);
            IndexWriter indexWriter = new IndexWriter(dir, new StandardAnalyzer(), true);
            indexWriter.SetRAMBufferSizeMB(10.0);
            indexWriter.SetUseCompoundFile(false);
            indexWriter.SetMaxMergeDocs(10000);
            indexWriter.SetMergeFactor(100);

            if (ds.Tables[0] != null)
            {
                DataTable dt = ds.Tables[0];
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        //Create the Document object
                        Document doc = new Document();
                        foreach (DataColumn dc in dt.Columns)
                        {
                            //Populate the document with the column name and value from our query
                            doc.Add(new Field(dc.ColumnName, dr[dc.ColumnName].ToString(), Field.Store.YES, Field.Index.TOKENIZED));
                        }
                        // Write the Document to the catalog
                        indexWriter.AddDocument(doc);
                    }
                }
            }
            // Close the writer
            indexWriter.Close();
        }
        #endregion

        #region Searching Methods
        /// <summary>
        /// for simple searching
        /// </summary>
        /// <param name="searchString"></param>
        public DataTable SearchPersons(string searchString)
        {
            // Results are collected as a List
            List<SearchResults> Searchresults = new List<SearchResults>();

            // Specify the location where the index files are stored
            string indexFileLocation = @"D:\Lucene.Net\Data\Persons";
            Lucene.Net.Store.Directory dir = Lucene.Net.Store.FSDirectory.GetDirectory(indexFileLocation);
            // specify the search fields, lucene search in multiple fields
            string[] searchfields = new string[] { "ContentText" };
            IndexSearcher indexSearcher = new IndexSearcher(dir);

            // Making a boolean query for searching and get the searched hits
            BooleanQuery objbool = QueryMaker(searchString, searchfields);

            var hits = indexSearcher.Search(objbool); // ~ symbol is used for fuzzy search. * for wildcard search

            List<SearchResults> searchlist = new List<SearchResults>();
            SearchResults result = null;
            //add to list
            for (int i = 0; i < hits.Length(); i++)
            {
                result = new SearchResults();
                result.PageName = hits.Doc(i).GetField("PageName").StringValue();
                result.Tag = hits.Doc(i).GetField("Tag").StringValue();
                result.ContentText = hits.Doc(i).GetField("ContentText").StringValue();
                result.Priority = Convert.ToInt32(hits.Doc(i).GetField("Priority").StringValue());
                searchlist.Add(result);
            }
            //sort by priority
            searchlist = searchlist.OrderBy(x => x.Priority).ToList();

            indexSearcher.Close();
            //GridView1.DataSource = searchlist;
            //GridView1.DataBind();
            ListtoDataTableConverter converter = new ListtoDataTableConverter();
            DataTable dt = converter.ToDataTable(searchlist);



            return dt;
        }

        /// <summary>
        /// Making the query for simple search
        /// </summary>
        /// <param name="searchString">text for search</param>
        /// <param name="searchfields">passing fields for search</param>
        /// <returns></returns>
        public BooleanQuery QueryMaker(string searchString, string[] searchfields)
        {
            var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_29, searchfields, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));

            var finalQuery = new BooleanQuery();

            string searchText;
            searchText = searchString.Replace("+", "");
            searchText = searchText.Replace("\"", "");
            searchText = searchText.Replace("\'", "");
            searchText = searchText.Replace("~", "");

            //Split the search string into separate search terms by word
            string[] terms = searchText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string term in terms)
            {

                //if (Typeofwords.iWithallwords == typeofword)
                //    finalQuery.Add(parser.Parse(term.Replace("*", "") + "*"), BooleanClause.Occur.SHOULD);
                //else if (Typeofwords.iexactphase == typeofword)
                //    finalQuery.Add(parser.Parse(term.Replace("*", "") + "*"), BooleanClause.Occur.MUST);
                //else if (Typeofwords.atleastoneword == typeofword)
                //    finalQuery.Add(parser.Parse(term.Replace("*", "") + "*"), BooleanClause.Occur.SHOULD);
                //else if (Typeofwords.withoutwords == typeofword) { }
                //else
                finalQuery.Add(parser.Parse(term.Replace("*", "") + "*"), BooleanClause.Occur.SHOULD);

                ////for without word
                //if (Typeofwords.withoutwords == typeofword)
                //{
                //    finalQuery.Add(new BooleanClause(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD));
                //    finalQuery.Add(new BooleanClause(new TermQuery(new Term("ContentText", term)), BooleanClause.Occur.MUST_NOT));

                //}
                //else
                //{
                //    Query query = new FuzzyQuery(new Term("ContentText", term), 0.5f, 4);
                //    finalQuery.Add(query, BooleanClause.Occur.SHOULD);
                //}
                Query query = new FuzzyQuery(new Term("ContentText", term), 0.5f, Convert.ToInt32(MinValue.MinPrefexvalue));
                finalQuery.Add(query, BooleanClause.Occur.SHOULD);
            }

            return finalQuery;
        }

        #region Advance search Methods

        /// <summary>
        /// For Advance searching- with group search
        /// </summary>
        public DataTable SearchPersons_Multiple(MVC4_Foundation3_Lucene_Search.Models.IndexingModel indexingModel)
        {
            // Results are collected as a List
            List<SearchResults> Searchresults = new List<SearchResults>();

            // Specify the location where the index files are stored
            string indexFileLocation = @"D:\Lucene.Net\Data\Persons";
            Lucene.Net.Store.Directory dir = Lucene.Net.Store.FSDirectory.GetDirectory(indexFileLocation);
            // specify the search fields, lucene search in multiple fields
            string[] searchfields = new string[] { "ContentText" };
            IndexSearcher indexSearcher = new IndexSearcher(dir);

            // Making a boolean query for searching and get the searched hits

            BooleanQuery objbool = QueryMaker_Multiple(searchfields, indexingModel);

            var hits = indexSearcher.Search(objbool); // ~ symbol is used for fuzzy search. * for wildcard search

            List<SearchResults> searchlist = new List<SearchResults>();
            SearchResults result = null;

            for (int i = 0; i < hits.Length(); i++)
            {
                result = new SearchResults();
                result.PageName = hits.Doc(i).GetField("PageName").StringValue();
                result.Tag = hits.Doc(i).GetField("Tag").StringValue();
                result.ContentText = hits.Doc(i).GetField("ContentText").StringValue();
                result.Priority = Convert.ToInt32(hits.Doc(i).GetField("Priority").StringValue());

                searchlist.Add(result);

            }
            //sort by priority
            searchlist = searchlist.OrderBy(x => x.Priority).ToList();

            indexSearcher.Close();
            //GridView1.DataSource = searchlist;
            //GridView1.DataBind();
            ListtoDataTableConverter converter = new ListtoDataTableConverter();
            DataTable dt = converter.ToDataTable(searchlist);



            return dt;
        }

        /// <summary>
        /// Making the query for multiple search
        /// </summary>
        /// <param name="searchfields"></param>
        /// <returns></returns>
        public BooleanQuery QueryMaker_Multiple(string[] searchfields, MVC4_Foundation3_Lucene_Search.Models.IndexingModel indexingModel)
        {
            //var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_29, searchfields, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));
            var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, searchfields[0], new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_29));

            var finalQuery = new BooleanQuery();
            //MVC4_Foundation3_Lucene_Search.Models.IndexingModel indexingModel;
          
            //for Text with all words
            if (indexingModel.wiithallwords != null)
            {
                string searchText = indexingModel.wiithallwords;
                searchText = searchText.Replace("+", "");
                searchText = searchText.Replace("\"", "");
                searchText = searchText.Replace("\'", "");
                searchText = searchText.Replace("~", "");

                //Split the search string into separate search terms by word
                string[] terms = searchText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var finalQuery_sub = new BooleanQuery();

                foreach (string term in terms)
                {
                    Query query = new FuzzyQuery(new Term("ContentText", term), 0.5f, Convert.ToInt32(MinValue.MinPrefexvalue));
                    finalQuery_sub.Add(query, BooleanClause.Occur.SHOULD);
                }

                finalQuery.Add(finalQuery_sub, BooleanClause.Occur.SHOULD);

            }
            // with exact phrase
            if (indexingModel.exactphrase != null)
            {
                string searchText = indexingModel.exactphrase;
                searchText = searchText.Replace("+", "");
                searchText = searchText.Replace("\"", "");
                searchText = searchText.Replace("\'", "");
                searchText = searchText.Replace("~", "");

                //Split the search string into separate search terms by word
                string[] terms = searchText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var finalQuery_sub = new BooleanQuery();

                foreach (string term in terms)
                {
                    finalQuery_sub.Add(parser.Parse(term.Replace("*", "") + ""), BooleanClause.Occur.MUST);
                    //Query query = new FuzzyQuery(new Term("ContentText", term), 0.5f, 4);
                    //finalQuery.Add(query, BooleanClause.Occur.MUST);
                }
                finalQuery.Add(finalQuery_sub, BooleanClause.Occur.SHOULD);
            }
            // for atleast one word
            if (indexingModel.leastWords != null)
            {
                string searchText = indexingModel.leastWords;
                searchText = searchText.Replace("+", "");
                searchText = searchText.Replace("\"", "");
                searchText = searchText.Replace("\'", "");
                searchText = searchText.Replace("~", "");

                //Split the search string into separate search terms by word
                string[] terms = searchText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                var finalQuery_sub = new BooleanQuery();

                foreach (string term in terms)
                {
                    //  finalQuery.Add(parser.Parse(term.Replace("*", "") + "*"), BooleanClause.Occur.SHOULD);
                    Query query = new FuzzyQuery(new Term("ContentText", term), 0.5f, Convert.ToInt32(MinValue.MinPrefexvalue));
                    finalQuery_sub.Add(query, BooleanClause.Occur.SHOULD);
                }
                finalQuery.Add(finalQuery_sub, BooleanClause.Occur.SHOULD);
            }
            // for "without word"
            if (indexingModel.withoutWords != null)
            {
                string searchText = indexingModel.withoutWords;
                searchText = searchText.Replace("+", "");
                searchText = searchText.Replace("\"", "");
                searchText = searchText.Replace("\'", "");
                searchText = searchText.Replace("~", "");

                //Split the search string into separate search terms by word
                string[] terms = searchText.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string term in terms)
                {

                    finalQuery.Add(parser.Parse(term.Replace("*", "") + ""), BooleanClause.Occur.MUST_NOT);
                    //previous code- to get all data then remove word from that list.
                    //finalQuery.Add(new BooleanClause(new MatchAllDocsQuery(), BooleanClause.Occur.SHOULD));
                    //finalQuery.Add(new BooleanClause(new TermQuery(new Term("ContentText", term)), BooleanClause.Occur.MUST_NOT));
                }
            }

            return finalQuery;
        }
        #endregion

        #endregion


      



        public ActionResult Index(MVC4_Foundation3_Lucene_Search.Models.SearchModels searchValue)
        {


            //return View(clients);
            DataTable dt = SearchPersons("");
            ViewBag.AuthorList = dt;
            return View();



            //ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            //return View();
        }
        /// <summary>
        /// For Creating a Index
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateIndex()
        {
            CreatePersonsIndex(GetPersons());


            return RedirectToAction("Index", "Home");
        }

           [HttpPost]
        public ActionResult Search(MVC4_Foundation3_Lucene_Search.Models.IndexingModel indexingModel)
        {
            DataTable dt = SearchPersons(indexingModel.SearchValue);
            ViewBag.AuthorList = dt;
            return View("Index");
           // SearchPersons(indexingModel.SearchValue, Typeofwords.defaultwords);
           // return RedirectToAction("Index", "Home");
        }
            
        public class ListtoDataTableConverter
           {
               public DataTable ToDataTable<T>(List<T> items)
               {
                   DataTable dataTable = new DataTable(typeof(T).Name);
                   //Get all the properties
                   PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                   foreach (PropertyInfo prop in Props)
                   {
                       //Setting column names as Property names
                       dataTable.Columns.Add(prop.Name);
                   }
                   foreach (T item in items)
                   {
                       var values = new object[Props.Length];
                       for (int i = 0; i < Props.Length; i++)
                       {
                           //inserting property values to datatable rows
                           values[i] = Props[i].GetValue(item, null);
                       }
                       dataTable.Rows.Add(values);
                   }
                   //put a breakpoint here and check datatable
                   return dataTable;
               }
           }
          
        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        public ActionResult advanceSearch(MVC4_Foundation3_Lucene_Search.Models.IndexingModel indexingModel)
        {
            DataTable dt = new DataTable();

            if (indexingModel.wiithallwords == string.Empty && indexingModel.exactphrase == string.Empty && indexingModel.leastWords == string.Empty && indexingModel.withoutWords == string.Empty)
            {
                dt = SearchPersons("");
            }
            else
            {//alteast one word in entered in the group
               dt= SearchPersons_Multiple(indexingModel);
            }         
           
            ViewBag.AuthorList = dt;
            return View("Index");
        }
    }
}
