var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// добавление кэширования
builder.Services.AddMemoryCache();

var app = builder.Build();

//после этого при запросах типа https://PORT:IP/controllerName/methodName
//будут вызываться соответствующие методы в контроллерах, контроллеры будут работать
app.MapControllers();

//сервер начнет слушать порты, указанные в файлах конфигурации
app.Run();
