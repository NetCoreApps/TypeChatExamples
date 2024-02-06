using TypeChatExamples.ServiceInterface;

AppHost.RegisterKey();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvcBuilder = builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register all services
builder.Services.AddServiceStack(typeof(MusicService).Assembly, c => {
    c.AddSwagger(o => {
        //o.AddJwtBearer();
        o.AddBasicAuth();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.UseServiceStack(new AppHost(), c =>
{
    c.MapEndpoints();
});

app.Run();