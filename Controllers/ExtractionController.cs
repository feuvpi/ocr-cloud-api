using Microsoft.AspNetCore.Mvc;

namespace ocr_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExtractionController
    {
        private readonly IOCRApplication _ocrApplication;

        readonly string endpointSecret = "1fd608b32bb247b0ba85dcc34937ae793d083583eac7a2013f46831d644edccc";

        /// <summary>
        /// Initializes a new instance of the <see cref="OCRController"/> class.
        /// </summary>
        /// <param name="ocrApplication">The application layer service for OCR operations.</param>
        public ExtractionController(IOCRApplication awsApplication)
        {
            _ocrApplication = awsApplication;
        }

        #region [ Azure Document Intelligence Endpoints ]

        /// <summary>
        /// Extracts text and structured data from documents using Azure OCR.
        /// </summary>
        /// <param name="file">Document file (PDF, JPEG, PNG, TIFF, or BMP)</param>
        /// <returns>Processed document data containing extracted text and key-value pairs</returns>
        /// <response code="200">Returns the extracted document data</response>
        /// <response code="400">If the file is null or empty</response>
        /// <response code="500">If processing fails</response>
        [HttpPost("ocr-1")]
        public async Task<IActionResult> UploadImageToAzure(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo recebido.");

            try
            {
                var result = await _ocrApplication.AnalyzeDocumentWithAzure(file);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocorreu um erro ao tentar processar o documento: " + ex.Message);
            }
        }


        /// <summary>
        /// Uploads an image and analyzes it using OCR.
        /// </summary>
        /// <param name="file">The image file to be analyzed.</param>
        /// <param name="modelId">The ID of the custom model to use for analysis.</param>
        /// <returns>An <see cref="IActionResult"/> containing the analysis result or an error message.</returns>
        /// <response code="200">Returns the analysis result.</response>
        /// <response code="400">If the file is null or empty, or if the model ID is not provided.</response>
        /// <response code="500">If an error occurs during processing.</response>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImageToOCR(IFormFile file, [FromForm] string secret, [FromForm] string CNPJ, [FromForm] string usuarioId, [FromForm] string convenioId, [FromForm] string tipoGuiaId, string modelId)
        {
            if (secret != endpointSecret)
                return BadRequest("Credenciais invalidas");

            if (string.IsNullOrEmpty(CNPJ) || !Utils.IsValidCNPJ(CNPJ))
                return BadRequest("CNPJ invalido.");

            if (!int.TryParse(usuarioId, out int _usuarioId))
                return BadRequest("Usuário invalido.");

            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo recebido.");

            if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(convenioId) || string.IsNullOrEmpty(tipoGuiaId))
                return BadRequest("A requisição não contém informações obrigatorias.");

            if (!int.TryParse(convenioId, out int convenioIdInt) || !int.TryParse(tipoGuiaId, out int tipoGuiaIdInt))
            {
                return BadRequest("A requisição contém informações invalidas.");
            }

            OCR_Cooperativa requestInfo = new OCR_Cooperativa(CNPJ, _usuarioId, convenioIdInt, tipoGuiaIdInt);

            try
            {
                using Stream fileStream = file.OpenReadStream();
                var result = false;
                //var result = await _azureApplication.AnalyzeDocument(modelId, fileStream, requestInfo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocorreu um erro ao tentar processar o documento: " + ex.Message);
            }
        }

        #endregion

        #region [ AWS Textract Endpoints ]
        /// <summary>
        /// Uploads an image and analyzes it using OCR.
        /// </summary>
        /// <param name="file">The image file to be analyzed.</param>
        /// <param name="modelId">The ID of the custom model to use for analysis.</param>
        /// <returns>An <see cref="IActionResult"/> containing the analysis result or an error message.</returns>
        /// <response code="200">Returns the analysis result.</response>
        /// <response code="400">If the file is null or empty, or if the model ID is not provided.</response>
        /// <response code="500">If an error occurs during processing.</response>
        [HttpPost("ocr-2")]
        public async Task<IActionResult> AnalyzeDocumentWithAWS(IFormFile file)
        {

            //if (secret != endpointSecret)
            //    return BadRequest("Credenciais invalidas.");

            //if (string.IsNullOrEmpty(CNPJ) || !Utils.IsValidCNPJ(CNPJ))
            //    return BadRequest("CNPJ invalido.");

            //if (!int.TryParse(usuarioId, out int _usuarioId))
            //    return BadRequest("Usuário invalido.");

            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo recebido.");

            try
            {
                using Stream fileStream = file.OpenReadStream();
                using MemoryStream memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();  // This converts the stream to byte[]
                var result = await _ocrApplication.AnalyzeDocument(fileBytes);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocorreu um erro ao tentar processar o documento: " + ex.Message);
            }
        }

        /// <summary>
        /// Initiates asynchronous document analysis using AWS Textract, waits for completion, and returns results.
        /// </summary>
        /// <param name="file">The document file to be analyzed</param>
        /// <param name="secret">Authentication key required to access the endpoint</param>
        /// <returns>
        /// 200 (OK) with the analysis results if successful
        /// 400 (BadRequest) if credentials are invalid or no file is provided
        /// 500 (InternalServerError) if analysis fails or an error occurs during processing
        /// </returns>
        /// <remarks>
        /// Process flow:
        /// 1. Validates credentials and file
        /// 2. Starts async Textract analysis
        /// 3. Polls for job completion
        /// 4. Retrieves and returns results if successful
        /// </remarks>
        [HttpPost("ocr-2-async")]
        public async Task<IActionResult> AnalyzeDocumentWithAWSAsync(IFormFile file, [FromForm] string CNPJ, [FromForm] string usuarioId, [FromForm] string secret)
        {

            if (secret != endpointSecret)
                return BadRequest("Credenciais invalidas.");

            if (string.IsNullOrEmpty(CNPJ) || !Utils.IsValidCNPJ(CNPJ))
                return BadRequest("CNPJ invalido.");

            if (!int.TryParse(usuarioId, out int _usuarioId))
                return BadRequest("Usuário invalido.");

            if (file == null || file.Length == 0)
                return BadRequest("Nenhum arquivo recebido.");

            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();
                int numberOfPages = await _ocrApplication.GetPDFPageCount(fileBytes);
                OCR_Cooperativa extracao = new OCR_Cooperativa(CNPJ, _usuarioId, "aws-async", numberOfPages);

                // Start the async analysis
                DocumentAnalysisStartResponse startResponse = await _ocrApplication.StartAsyncDocumentAnalysis(extracao, fileBytes);

                // Wait for the job to complete
                var analysisResponse = await _ocrApplication.PollForCompletion(startResponse.JobId);

                if (analysisResponse.Status == "SUCCEEDED")
                {
                    // Get the results
                    var result = await _ocrApplication.GetAsyncDocumentResults(startResponse);
                    extracao.Id = startResponse.ExtracaoId;
                    extracao.Result = result.ToString();
                    await _ocrApplication.RegisterExtractionAsync(extracao);
                    return Ok(result);
                }
                else
                {
                    return StatusCode(500, $"Análise do documento falhou: {analysisResponse.Message}");
                }


            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ocorreu um erro ao tentar processar o documento: " + ex.Message);
            }

        }

        /// <summary>
        /// Retrieves the status of a Textract job by its ID.
        /// </summary>
        /// <param name="jobId">The unique identifier of the Textract job</param>
        /// <param name="secret">Authentication key required to access the endpoint</param>
        /// <returns>
        /// 200 (OK) with the job status
        /// 400 (BadRequest) if credentials are invalid
        /// 500 (InternalServerError) if an error occurs during status check
        /// </returns>
        //[HttpGet("status/{jobId}")]
        //public async Task<IActionResult> GetStatus(string jobId, [FromQuery] string secret)
        //{
        //    if (secret != endpointSecret)
        //        return BadRequest("Invalid Credentials!");

        //    try
        //    {
        //        var status = await _awsApplication.CheckJobStatus(jobId);
        //        return Ok(status);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "An error occurred while checking status: " + ex.Message);
        //    }
        //}

        /// <summary>
        /// Retrieves the results of a completed Textract job using its ID and S3 file location.
        /// </summary>
        /// <param name="jobId">The unique identifier of the Textract job</param>
        /// <param name="s3Key">The S3 key/path where the processed document is stored</param>
        /// <param name="secret">Authentication key required to access the endpoint</param>
        /// <returns>
        /// 200 (OK) with the job results
        /// 400 (BadRequest) if credentials are invalid
        /// 500 (InternalServerError) if an error occurs during results retrieval
        /// </returns>
        //[HttpGet("results/{jobId}")]
        //public async Task<IActionResult> GetResults(string jobId, string s3Key, [FromQuery] string secret)
        //{
        //    if (secret != endpointSecret)
        //        return BadRequest("Invalid Credentials!");

        //    try
        //    {
        //        var results = await _awsApplication.GetJobResults(jobId, s3Key);
        //        return Ok(results);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "An error occurred while retrieving results: " + ex.Message);
        //    }
        //}

        #endregion 
    }
}
