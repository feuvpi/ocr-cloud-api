using Microsoft.OpenApi.Models;
using Amazon.Runtime.CredentialManagement;
using Amazon.Textract;
using Amazon;
using Amazon.S3;
using Amazon.SQS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Amazon.Extensions.NETCore.Setup;
using Azure.AI.DocumentIntelligence;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header usando o esquema Bearer."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                 {
                     {
                           new OpenApiSecurityScheme
                             {
                                 Reference = new OpenApiReference
                                 {
                                     Type = ReferenceType.SecurityScheme,
                                     Id = "Bearer"
                                 }
                             },
                             new string[] {}
                     }
                 });
});

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,

          ValidIssuer = builder.Configuration["Jwt:Issuer"],
          ValidAudience = builder.Configuration["Jwt:Audience"],
          IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
      };
  });


// -- Access environment variables
var formRecognizerEndpoint = Environment.GetEnvironmentVariable("FORM_RECOGNIZER_ENDPOINT");
var formRecognizerApiKey = Environment.GetEnvironmentVariable("FORM_RECOGNIZER_API_KEY");

builder.Services.AddTransient((serviceProvider) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var endpoint = builder.Configuration["FormRecognizer:endpoint"];
    var apiKey = builder.Configuration["FormRecognizer:apiKey"];

    var client = new DocumentIntelligenceClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
    //var client = new DocumentAnalysisClient(new Uri(endpoint), new Azure.AzureKeyCredential(apiKey));
    return client;
});

// Configuração AWS
var awsOptions = new AWSOptions
{
    Region = RegionEndpoint.GetBySystemName(builder.Configuration["AWS:Region"])
};

// Tentar obter credenciais do profile
var chain = new CredentialProfileStoreChain();
if (chain.TryGetAWSCredentials("default", out var awsCredentials))
{
    awsOptions.Credentials = awsCredentials;
}

// Registrar os serviços AWS
builder.Services.AddAWSService<IAmazonTextract>(awsOptions);
builder.Services.AddAWSService<IAmazonS3>(awsOptions);
builder.Services.AddAWSService<IAmazonSQS>(awsOptions);

//builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

//builder.Services.AddRepositories();
builder.Services.AddControllers();

builder.Services.AddControllers();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
