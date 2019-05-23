using System;
using System.Collections.Generic;
using GAB.BatchServer.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GAB.BatchServer.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using GAB.BatchServer.API.Exceptions;
using Microsoft.Extensions.Logging;
using GAB.BatchServer.API.Common;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Net.Http.Headers;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace GAB.BatchServer.API.Controllers
{
    /// <summary>
    /// API Controller for downloading and uploading GAB Science Lab tasks
    /// </summary>
    [Route("api/[controller]")]
    public class BatchController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly BatchServerContext _context;
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetry = new TelemetryClient();

        /// <summary>
        /// Constructor for the BatchController class
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public BatchController(IConfiguration configuration, BatchServerContext context, ILoggerFactory logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger.CreateLogger("GAB.BatchServer.API.Controllers");
        }

        #region GetNewBatch

        /// <summary>
        /// Gets a new batch of tasks to process by a GAB Science Lab client
        /// </summary>
        /// <param name="batchSize">Size of the batch</param>
        /// <param name="email">Email of the user deploying the lab</param>
        /// <param name="fullName">Full name of the user deploying the lab</param>
        /// <param name="teamName">Team name of the user deploying the lab</param>
        /// <param name="companyName">Company name of the user deploying the lab</param>
        /// <param name="location">Location of the user deploying the lab</param>
        /// <param name="countryCode">Country of the user deploying the lab</param>
        /// <returns>A batch of tasks to be processed</returns>
        /// <response code="200">Returns the assigned batch items to process</response>
        /// <response code="400">If any of the parameters is invalid</response>
        // GET api/GetNewBatch?batchSize=100&email=john@doe.com&fullName=johndoe&teamName=myTeam&location=91&countryCode=ES
        [Route("GetNewBatch")]
        [HttpGet]
        [ProducesResponseType(typeof(GetNewBatchResult), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> GetNewBatch([Required] int batchSize, [Required] string email,
            [Required] string fullName, [Required] string teamName,
            [Required] string companyName,
            [Required] string location, [Required] string countryCode)
        {
            try
            {
                _logger.LogInformation(LoggingEvents.GET_NEW_BATCH,
                    "Getting new batch with size {batchSize} and email {email}", batchSize, email);

                // Validations
                var clientName = _configuration.GetValue<string>("BatchServer:MinimumClientVersion").Split('/')[0];
                if (!Request.Headers.ContainsKey(HeaderNames.UserAgent) || !Request.Headers[HeaderNames.UserAgent].ToString().StartsWith(clientName) || string.Compare(Request.Headers[HeaderNames.UserAgent].ToString(),
                        _configuration.GetValue<string>("BatchServer:MinimumClientVersion"),
                        StringComparison.InvariantCultureIgnoreCase) < 0)
                    return GetBadRequest(
                        $"Minimum client version must be {_configuration.GetValue<string>("BatchServer:MinimumClientVersion")}. Please, upgrade your container instance to the latest version.");
                if (batchSize == 0 || batchSize > _configuration.GetValue<int>("BatchServer:MaxBatchSize"))
                    return GetBadRequest($"Invalid batch size {batchSize}");
                if (string.IsNullOrEmpty(email) || email.Length > 100)
                    return GetBadRequest("Parameter email is invalid");
                if (string.IsNullOrEmpty(fullName) || fullName.Length > 50)
                    return GetBadRequest("Parameter fullName is invalid");
                if (string.IsNullOrEmpty(companyName) || companyName.Length > 50)
                    return GetBadRequest("Parameter companyName is invalid");
                if (string.IsNullOrEmpty(location) || location.Length > 50)
                    return GetBadRequest("Parameter location is required");
                if (string.IsNullOrEmpty(countryCode) || countryCode.Length > 2 ||
                    !Countries.IsoCodes.ContainsKey(countryCode.ToUpperInvariant()))
                    return GetBadRequest("Parameter countryCode is not valid");

                // Trim parameters
                email = email.Trim().ToLowerInvariant();
                fullName = fullName.Trim();
                teamName = teamName.Trim();
                location = location.Trim();
                companyName = companyName.Trim();
                countryCode = countryCode.ToUpperInvariant();

                // Find the user
                var user = await GetOrCreateLabUserAsync(email, fullName, teamName, location, countryCode, companyName);

                // Check if we allow more than X pending batches per user to avoid abuse
                var maxInputsPerUser = _configuration.GetValue<int>("BatchServer:MaxInputsPerUser");
                if (maxInputsPerUser != 0)
                {
                    var currentInputsCount = await GetAssignedInputsCountByUserAsync(user);
                    if ((currentInputsCount + batchSize) > maxInputsPerUser)
                    {
                        batchSize = (maxInputsPerUser - currentInputsCount);
                    }

                    if (batchSize <= 0)
                    {
                        return GetBadRequest("Maximum inputs per user exceeded");
                    }
                }

                // Return the batch
                var batchId = Guid.NewGuid();
                var batchCollection = await AssignNewBatchAsync(batchId, batchSize, user);

                // Create output sas to allow the client to upload the light curve files (one light curve file per input)
                var adHocSas = new SharedAccessBlobPolicy()
                {
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24),
                    Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
                };                
                var outputList = new List<OutputResult>();
                foreach (var input in batchCollection)
                {
                    var blob = Storage.OutputsContainer.GetBlockBlobReference($"{input.InputId}_data.npz");
                    outputList.Add(new OutputResult()
                    {
                        InputId = input.InputId,
                        LightCurvesUploadUri = blob.Uri + blob.GetSharedAccessSignature(adHocSas)
                    });
                }

                _telemetry.TrackEvent("BatchDelivered");                

                return
                    Ok(new GetNewBatchResult
                    {
                        BatchId = batchId,
                        Inputs = batchCollection,
                        Outputs = outputList
                    });
            }
            catch (NoMoreAvailableInputsException ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogCritical(LoggingEvents.GET_NEW_BATCH, ex, "No more available inputs");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.GET_NEW_BATCH_SERVER_ERROR, ex, ex.Message);
                return StatusCode(500);
            }
        }

        private async Task<int> GetAssignedInputsCountByUserAsync(LabUser user)
        {
            var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText =
                "(SELECT COUNT(InputId) FROM dbo.Inputs WHERE Status=1 and AssignedToLabUserId=@LabUserId)";
            cmd.Parameters.Add(new SqlParameter("@LabUserId", SqlDbType.Int) {Value = user.LabUserId});
            return (int) await cmd.ExecuteScalarAsync();
        }

        private async Task<List<Input>> AssignNewBatchAsync(Guid batchId, int batchSize, LabUser user)
        {
            _logger.LogInformation("Assigning batch {batchId} (size {batchSize}) to user {email}", batchId, batchSize,
                user.EMail);
            var minInputId = _configuration.GetValue<int>("BatchServer:MinInputId");
            var sql = new RawSqlString(
                $"UPDATE dbo.Inputs SET BatchId=@BatchId, AssignedToLabUserId=@LabUserId, Status=1, ModifiedOn=GETUTCDATE() " +
                "WHERE InputId IN " +
                $"(SELECT TOP (@BatchSize) InputId FROM dbo.Inputs WITH (UPDLOCK, ROWLOCK, READPAST) " +
                $"WHERE Status=0 and InputId>@MinInputId)");
            var rows = await _context.Database.ExecuteSqlCommandAsync(sql,
                new SqlParameter("@BatchId", SqlDbType.UniqueIdentifier) {Value = batchId},
                new SqlParameter("@LabUserId", SqlDbType.Int) {Value = user.LabUserId},
                new SqlParameter("@BatchSize", SqlDbType.Int) {Value = batchSize},
                new SqlParameter("@MinInputId", SqlDbType.Int) {Value = minInputId}
            );
            var batchCollection = await (from i in _context.Inputs
                where i.BatchId == batchId
                select i).ToListAsync();
            if (batchCollection.Count == 0)
            {
                throw new NoMoreAvailableInputsException(
                    "There are no more available inputs to process on this location");
            }

            // Generate the input Uris
            var externalStorageBaseUrl = _configuration.GetValue<string>("BatchServer:ExternalStorageBaseUrl");
            if (!string.IsNullOrEmpty(externalStorageBaseUrl) && !externalStorageBaseUrl.EndsWith("/"))
            {
                externalStorageBaseUrl += "/";
            }
            foreach (var input in batchCollection)
            {
                if (string.IsNullOrEmpty(externalStorageBaseUrl))
                {
                    input.Parameters = Storage.InputsContainer.GetBlobReference(input.Parameters).Uri.ToString();
                }
                else
                {
                    input.Parameters = externalStorageBaseUrl + input.Parameters;
                }
            }
            return batchCollection;
        }

        private async Task<LabUser> GetOrCreateLabUserAsync(string email, string fullName, string teamName,
            string location, string countryCode, string companyName)
        {
            var user = await _context.LabUsers.FirstOrDefaultAsync(u => u.EMail == email);
            if (user == null)
            {
                _logger.LogInformation("Creating user {email}", email);
                // Create the user if does not exist
                user = new LabUser
                {
                    EMail = email,
                    FullName = fullName,
                    TeamName = teamName,
                    Location = location,
                    CountryCode = countryCode,
                    CompanyName = companyName
                };
                _context.LabUsers.Add(user);
                await _context.SaveChangesAsync();
            }
            else // Check if the parameters are different from the stored ones
            {
                if (user.FullName != fullName || user.TeamName != teamName || user.CompanyName != companyName ||
                    user.Location != location || user.CountryCode != countryCode)
                {
                    _logger.LogInformation("Updating user {email}", email);
                    user.FullName = fullName;
                    user.TeamName = teamName;
                    user.CompanyName = companyName;
                    user.Location = location;
                    user.CountryCode = countryCode;
                    _context.LabUsers.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            return user;
        }

        #endregion

        #region CancelInputs

        /// <summary>
        /// Uploads the result of a batch process
        /// </summary>
        /// <remarks>
        /// The inputId and email parameter must match the ones used during the GetNewBatch call
        /// </remarks>
        /// <param name="email">The email of the user that requested the input</param>
        /// <param name="inputIds">The list of inputs to cancel</param>
        /// <returns></returns>
        /// <response code="200">Returns the id of the processed ouput</response>
        /// <response code="400">If any of the parameters is invalid</response>
        [Route("CancelInputs")]
        [HttpPost]
        [ProducesResponseType(typeof(UploadOutputResult), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> CancelInputs([Required] string email, [FromBody, Required] int[] inputIds)
        {
            try
            {
                _logger.LogInformation(LoggingEvents.CANCEL_INPUTS,
                    "Canceling inputs from email {email}", email);

                // Validations
                if (string.IsNullOrEmpty(email))
                    return GetBadRequest("Parameter email is required");
                if (inputIds == null || inputIds.Length == 0)
                    return GetBadRequest("Parameter inputIds is required");
                email = email.Trim();

                // Get the user
                var user = await _context.LabUsers.FirstOrDefaultAsync(u => u.EMail == email);
                if (user == null)
                    return GetNotFound($"The user {email} was not found");


                foreach (var inputId in inputIds)
                {
                    // Get the input
                    var input = await _context.Inputs.FirstOrDefaultAsync(i => i.InputId == inputId);
                    if (input == null)
                        return GetNotFound($"The input {inputId} was not found");


                    // Security check
                    if (input.AssignedTo?.LabUserId != user.LabUserId)
                    {
                        return GetBadRequest($"The user {email} can't update the input {inputId} result value");
                    }

                    _logger.LogInformation($"Returning {inputId} to the repository");
                    input.ModifiedOn = DateTime.UtcNow;
                    input.Status = Input.InputStatusEnum.Ready;
                    input.AssignedTo = null;
                    input.BatchId = null;
                    _context.Inputs.Update(input);
                }

                _telemetry.TrackEvent("CancelInputs");

                return Ok();
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.CANCEL_INPUTS_SERVER_ERROR, ex, ex.Message);
                return StatusCode(500);
            }
        }

        #endregion

        #region UpdateBatchResult

        /// <summary>
        /// Uploads the result of a batch process
        /// </summary>
        /// <remarks>
        /// The inputId and email parameter must match the ones used during the GetNewBatch call
        /// </remarks>
        /// <param name="inputId">The id of the input to update</param>
        /// <param name="email">The email of the user that requested the input</param>
        /// <param name="result">The result of the batch process</param>
        /// <returns></returns>
        /// <response code="200">Returns the id of the processed ouput</response>
        /// <response code="400">If any of the parameters is invalid</response>
        [Route("UploadOutput")]
        [HttpPost]
        [ProducesResponseType(typeof(UploadOutputResult), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> UploadOutput([Required] int inputId, [Required] string email,
            [FromBody, Required] OutputContent result)
        {
            string filename = "", outputFolder = "";
            try
            {
                _logger.LogInformation(LoggingEvents.UPLOAD_OUTPUT,
                    "Uploading result for input {inputId} and email {email}", inputId, email);

                // Validations
                if (string.IsNullOrEmpty(email))
                    return GetBadRequest("Parameter email is required");
                if (string.IsNullOrEmpty(result?.lc))
                    return GetBadRequest("Parameter result is required");
                email = email.Trim();

                // Get the input
                var input = await _context.Inputs.FirstOrDefaultAsync(i => i.InputId == inputId);
                if (input == null)
                    return GetNotFound($"The input {inputId} was not found");

                // Get the user
                var user = await _context.LabUsers.FirstOrDefaultAsync(u => u.EMail == email);
                if (user == null)
                    return GetNotFound($"The user {email} was not found");

                // Security check
                if (input.AssignedTo?.LabUserId != user.LabUserId)
                {
                    return GetBadRequest($"The user {email} can't update the input {inputId} result value");
                }

                // Update the output status
                var tuple = await UpdateOuputAsync(input, result);
                var output = tuple.Item1;
                var firstUpdate = tuple.Item2;

                _telemetry.TrackEvent("OutputUploaded");

                // Notify event hub, but only if was the first update to avoid dashboard hacks
                if (firstUpdate)
                {
                    await SendEventHubNotificationAsync(input, user, output, result);
                }

                return Ok(new UploadOutputResult {OutputId = output.OutputId});
            }
            catch (OutputParsingException ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.UPLOAD_OUTPUT_SERVER_ERROR, ex, "Output parse error");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(LoggingEvents.UPLOAD_OUTPUT_SERVER_ERROR, ex, ex.Message);
                return StatusCode(500);
            }
            finally
            {
                // Cleanup temp files
                DeleteTmpFile($"{inputId}.zip");
                DeleteTmpDirectory($"{inputId}");
            }
        }

        private void DeleteTmpFile(string filename)
        {
            try
            {
                var dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
                filename = Path.Combine(dataDir, filename);
                if (!string.IsNullOrEmpty(filename) && System.IO.File.Exists(filename))
                {
                    System.IO.File.Delete(filename);
                }
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
            }
        }

        private void DeleteTmpDirectory(string directory)
        {
            try
            {
                var dataDir = AppDomain.CurrentDomain.GetData("DataDirectory").ToString();
                directory = Path.Combine(dataDir, directory);

                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
            }
        }

        private async Task<string> UploadOutputResultToStorageAsync(string filename, Input input,
            string contentType = "application/json")
        {
            var blobName = Path.GetFileName(filename);
            var blob = Storage.OutputsContainer.GetBlockBlobReference(blobName);
            _logger.LogInformation($"Uploading the output part for input {input.InputId} to {blob.Uri}");
            await Storage.UploadOutputAsync(_configuration, blob, filename);
            return blobName;
        }

        private async Task<Tuple<Output, bool>> UpdateOuputAsync(Input input, OutputContent result)
        {
            _logger.LogInformation($"Updating input {input.InputId} to processed state");
            input.ModifiedOn = DateTime.UtcNow;
            input.Status = Input.InputStatusEnum.Processed;
            _context.Inputs.Update(input);

            var firstUpdate = false;
            var output = await _context.Outputs.FirstOrDefaultAsync(o => o.Input.InputId == input.InputId);
            if (output == null)
            {
                _logger.LogInformation($"Creating output for input {input.InputId}");
                output = new Output
                {
                    Input = input,
                    ContainerId = result.containerid,
                    ClientVersion = result.clientversion,
                    TICId = result.ticid,
                    Sector = result.sector,
                    Camera = result.camera,
                    CCD = result.ccd,
                    RA = result.ra,
                    Dec = result.dec,
                    TMag = result.tmag,
                    Result = result.lc,
                    IsPlanet = result.isplanet,
                    IsNotPlanet = result.isnotplanet,
                    Frequencies = string.Join(';', result.frequencies)
                };
                _context.Outputs.Add(output);
                firstUpdate = true;
            }
            else
            {
                _logger.LogWarning($"Updating output {output.OutputId} for input {input.InputId}");
                output.Result = result.lc;
                output.ContainerId = result.containerid;
                output.ClientVersion = result.clientversion;
                output.TICId = result.ticid;
                output.Sector = result.sector;
                output.Camera = result.camera;
                output.CCD = result.ccd;
                output.RA = result.ra;
                output.Dec = result.dec;
                output.TMag = result.tmag;
                output.IsPlanet = result.isplanet;
                output.IsNotPlanet = result.isnotplanet;
                output.Frequencies = string.Join(';', result.frequencies);
                output.ModifiedOn = DateTime.UtcNow;
                _context.Outputs.Update(output);
            }
            await _context.SaveChangesAsync();
            return new Tuple<Output, bool>(output, firstUpdate);
        }

        private async Task SendEventHubNotificationAsync(Input input, LabUser user, Output output,
            OutputContent result)
        {
            // Set input URL as external URL
            var externalStorageBaseUrl = _configuration.GetValue<string>("BatchServer:ExternalStorageBaseUrl");
            if (!string.IsNullOrEmpty(externalStorageBaseUrl) && !externalStorageBaseUrl.EndsWith("/"))
            {
                externalStorageBaseUrl += "/";
            }
            if (string.IsNullOrEmpty(externalStorageBaseUrl))
            {
                input.Parameters = Storage.InputsContainer.GetBlobReference(input.Parameters).Uri.ToString();
            }
            else
            {
                input.Parameters = externalStorageBaseUrl + input.Parameters;
            }

            // Send the message to the event hub
            var client = EventHubs.EventHubClient(_configuration);
            var messageData = new
            {
                inputId = input.InputId, // int 
                batchId = input.BatchId, // Guid
                outputId = output.OutputId, // int
                deploymentId = _configuration["BatchServer:DeploymentId"], // string (i.e. "North Europe")
                user = new
                {
                    email = user.EMail, // string
                    fullName = user.FullName, // string
                    location = user.Location, // string
                    teamName = user.TeamName, // string
                    companyName = user.CompanyName, // string
                    countryCode = user.CountryCode // string
                },
                score = new
                {
                    containerId = result.containerid,
                    clientVersion = result.clientversion,
                    ticId = result.ticid,
                    sector = result.sector,
                    camera = result.camera,
                    ccd = result.ccd,
                    ra = result.ra,
                    dec = result.dec,
                    tmag = result.tmag,
                    tpf = input.Parameters,
                    lc = output.Result,
                    per = output.Frequencies,
                    isPlanet = result.isplanet, // double
                    isNotPlanet = result.isnotplanet, // double
                    totalScore = (int) (result.isplanet * 1000) + (int) (result.isnotplanet * 100) // int
                }
            };

            var message = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(messageData));

            if (client == null)
            {
                _logger.LogWarning("Event hubs disabled: {message}", message);
            }
            else
            {
                await client.SendAsync(new Microsoft.Azure.EventHubs.EventData(Encoding.UTF8.GetBytes(message)));
            }
        }

        #endregion


        #region Logging

        private BadRequestObjectResult GetBadRequest(string message)
        {
            _telemetry.TrackTrace(message, SeverityLevel.Warning);
            _logger.LogWarning(message);
            return BadRequest(message);
        }

        private NotFoundObjectResult GetNotFound(string message)
        {
            _telemetry.TrackTrace(message, SeverityLevel.Warning);
            _logger.LogWarning(message);
            return NotFound(message);
        }

        private ObjectResult GetForbid(string message)
        {
            _telemetry.TrackTrace(message, SeverityLevel.Warning);
            _logger.LogWarning(message);
            return StatusCode(403, message);
        }

        #endregion
    }
}