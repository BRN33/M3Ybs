using M3_Ybs.BackGroundServices;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//MPU_Received.StartAsync();
//Manager.Worker_DoWork();


builder.Services.AddSingleton<JsonController>();
//builder.Services.AddSingleton<Manager>();


builder.Services.AddControllers();

//builder.Services.AddHostedService<MPU_Received>();
builder.Services.AddHostedService<Manager>(); //Manager Bakground Worker servisini sürekli çalýþmasý için buraya ekledik
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});



var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();  
}


app.UseCors();
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();   

app.Run();

