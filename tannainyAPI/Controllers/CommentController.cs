using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Entities;
using System.Security;
using System.Linq;
using System.Collections.Generic;

namespace tannainyAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        //Gets CosmosDB Key ja Uri from Azure
        private static readonly string endpointUri = Environment.GetEnvironmentVariable("APPSETTING_EndpointUri");
        private static readonly SecureString key = toSecureString(Environment.GetEnvironmentVariable("APPSETTING_PrimaryKey"));


        private readonly DocumentClient _documentClient;
        private const string _dbName = "tannainy";
        private const string _collectionName = "comment";
        Uri uri = UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName);

        private static SecureString toSecureString(string s)
        {
            SecureString ss = new SecureString();
            foreach (var item in s)
            {
                ss.AppendChar(item);
            }
            return ss.Copy();
        }



        public CommentController(IConfiguration configuration)
        {
            _configuration = configuration;
            //var endpointUri =
            //_configuration["ConnectionStrings:CosmosDbConnection:EndpointUri"];
            //var key =
            //_configuration["ConnectionStrings:CosmosDbConnection:PrimaryKey"];
            //Creates new database and document collection if not exists
            _documentClient = new DocumentClient(new Uri(endpointUri), key);
            _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = _dbName }).Wait();
            _documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_dbName),
                                                new DocumentCollection { Id = _collectionName });

        }



        // Get all comments from cosmosDB
       [HttpGet]
        public ActionResult<List<InputComment>> GetAllComments()
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };
            IQueryable<InputComment> query = _documentClient.CreateDocumentQuery<InputComment>(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            $"SELECT * FROM C",
            queryOptions);
            return Ok(query.ToList());
        }
        //Get all comment with same suggestion id
        [HttpGet]
        public ActionResult<List<InputComment>> GetCommentsBySuggestionId(string SuggestionId)
        {

            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };
            IQueryable<InputComment> query = _documentClient.CreateDocumentQuery<InputComment>(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            $"SELECT * FROM C WHERE C.SuggestionId = {SuggestionId}",
            queryOptions);
            return Ok(query.ToList());
        }

        // Get all suggestions from cosmosDB with most comments
        //[HttpGet]
        //public ActionResult<List<InputComment>> GetCommentCount(string SuggestionId)
        //{
        //    FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };
        //    IQueryable<InputComment> query = _documentClient.CreateDocumentQuery<InputComment>(
        //    UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
        //    $"SELECT COUNT(*) FROM C WHERE C.SuggestionId = {SuggestionId}",
        //    queryOptions);
        //    return Ok();
        //}

        // Post comment to cosmosDB
        [HttpPost]
        public async Task<ActionResult<string>> Post([FromBody] InputComment value)
        {
            Document document = await _documentClient.CreateDocumentAsync(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            value);
            return Ok(document.Id);
        }

        // Delete comment from cosmosDB
        [HttpDelete]

        public async Task<ActionResult<string>> Delete(string id)
        {
            try
            {
                await _documentClient.DeleteDocumentAsync(
                    UriFactory.CreateDocumentUri(_dbName, _collectionName, id));
                return Ok($"Deleted document id {id}");
            }
            catch (DocumentClientException de)
            {
                switch (de.StatusCode.Value)
                {
                    case System.Net.HttpStatusCode.NotFound:
                        return NotFound();
                }
            }
            return BadRequest();
        }

             [HttpGet]
        public ActionResult<List<InputComment>> GetByCommentId(string id)
        {

            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };
            IQueryable<InputComment> query = _documentClient.CreateDocumentQuery<InputComment>(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            $"SELECT * FROM C WHERE C.id = {id}",
            queryOptions);
            return Ok(query.ToList());
        }
        //Update comment likes
        [HttpPut]
        public async Task<ActionResult<string>> UpdateCommentLike(string id, int Likes)
        {
            //Fetch the Document to be updated
            Document document = _documentClient.CreateDocumentQuery<Document>(uri)
                                 .Where(r => r.Id == id)
                                 .AsEnumerable()
                                 .SingleOrDefault();

            //Update some properties on the found resource
            document.SetPropertyValue("Likes", Likes);

            //Now persist these changes to the database by replacing the original resource
            Document updated = await _documentClient.ReplaceDocumentAsync(document);
            return Ok();
        }
      
    }
}

