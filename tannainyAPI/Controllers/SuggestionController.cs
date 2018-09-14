using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Entities;
using Microsoft.Azure.Documents;
using System.Security;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;

namespace tannainyAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SuggestionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        
        //Gets CosmosDB Key ja Uri from Azure
        private static readonly string endpointUri = Environment.GetEnvironmentVariable("APPSETTING_EndpointUri");
        private static readonly SecureString key = toSecureString(Environment.GetEnvironmentVariable("APPSETTING_PrimaryKey"));


        private readonly DocumentClient _documentClient;
        private const string _dbName = "tannainy";
        private const string _collectionName = "suggestion";
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
        public SuggestionController(IConfiguration configuration)
        {
            _configuration = configuration;
         
            //Creates new database and document collection if not exists
            _documentClient = new DocumentClient(new Uri(endpointUri), key);
            _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = _dbName }).Wait();
            _documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_dbName),
                                                new DocumentCollection { Id = _collectionName });
        }

        //Get suggestion by id
        [HttpGet]
        public ActionResult<List<InputSuggestion>> GetBySuggestionId(string id)
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };
            IQueryable<InputSuggestion> query = _documentClient.CreateDocumentQuery<InputSuggestion>(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            $"SELECT * FROM C WHERE C.id = {id}",
            queryOptions);
            return Ok(query.ToList());
        }

        // Get all suggestions from cosmosDB
        [HttpGet]
        public ActionResult<List<InputSuggestion>> GetAllSuggestions()
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };
            IQueryable<InputSuggestion> query = _documentClient.CreateDocumentQuery<InputSuggestion>(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            $"SELECT * FROM C",
            queryOptions);
            return Ok(query.ToList());
        }

        // Get all suggestions from cosmosDB with most likes
        [HttpGet]
        public ActionResult<List<InputSuggestion>> GetTop3()
        {
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };
            IQueryable<InputSuggestion> query = _documentClient.CreateDocumentQuery<InputSuggestion>(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            $"SELECT TOP 3 * FROM C ORDER BY C.Likes DESC",
            queryOptions);
            return Ok(query.ToList());
        }




        // Post suggestion to cosmosDB
        [HttpPost]
        public async Task<ActionResult<string>> Post([FromBody] InputSuggestion value)
        {
            Document document = await _documentClient.CreateDocumentAsync(
            UriFactory.CreateDocumentCollectionUri(_dbName, _collectionName),
            value);
            return Ok(document.Id);
        }

        // Update suggestion like
        [HttpPut]
        public async Task<ActionResult<string>> UpdateSuggestionLike(string id, int Likes)
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

        // Delete suggestion from cosmosDB
        [HttpDelete]

        public async Task<ActionResult<string>> Delete(string Id)
        {
            try
            {
                await _documentClient.DeleteDocumentAsync(
                    UriFactory.CreateDocumentUri(_dbName, _collectionName, Id));
                return Ok($"Deleted document id {Id}");
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
    }
}
