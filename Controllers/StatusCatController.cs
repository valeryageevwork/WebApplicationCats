using System.Net.Mime; //MediaTypeNames
using Microsoft.AspNetCore.Mvc; //Mvc - пространство имен, содержащее ControllerBase. MVC - паттерн, который здесь мало соблюден
using Microsoft.Extensions.Caching.Memory; //пространтсво имен, в котором находится IMemoryCache

namespace StatusCat.Controllers;

[ApiController] //нужно для того, чтоб контроллер работал
[Route("[controller]")] //нужно для того, чтобы к контроллеру можно было обратиться по адресу https://IP:PORT/StatusCat

//когда серверу поступит запрос, будет создан экземпляр StatusCatController
public class StatusCatController : ControllerBase
{
    private readonly IMemoryCache _cache;

    public StatusCatController(IMemoryCache cache)
    {
        _cache = cache;
    }

    //https://localhost:PORTиз консоли/StatusCat/GetImage?url=https://google.com/ - пример
    //https:/IP:PORT/StatusCat/GetImage?url=value
    [HttpGet("GetImage")] //указывает, что этот метод будет вызываться тогда,
                          //когда будет отправляен GET запрос по адресу .../GetImage
    public async Task<IResult> GetImage(string url)
    //IResult - results api
    {
        using var httpClient = new HttpClient(); //HttpClient может отправлять HTTP(s) запросы

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return Results.BadRequest($"url некорректный ({url})");
        }

        int statusCode;
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(uri);
            statusCode = Convert.ToInt32(response.StatusCode);
        }
        catch (HttpRequestException hre)
        {
            if (hre.StatusCode is null)
                return Results.BadRequest($"ошибка при обращении к {url}");

            statusCode = Convert.ToInt32(hre.StatusCode);
        }
        catch
        {
            return Results.BadRequest($"ошибка при обращении к {url}");
        }

        //2.a
        //получение картинки из кеша и проверки
        if (_cache.TryGetValue<byte[]?>(statusCode, out byte[]? imageBytes) && imageBytes is not null)
        {
            return Results.File(imageBytes, MediaTypeNames.Image.Jpeg);
        }

        //3
        imageBytes = await GetImageByStatusCode(statusCode);

        await Task.Run
        (
            () =>
            {
                _cache.Set
                (
                    statusCode, //ключ
                    imageBytes, //то что надо закешировать
                    TimeSpan.FromMinutes(1) //через какое время оно удалится из кеша
                ); 
            }
        );

        return Results.File(imageBytes, MediaTypeNames.Image.Jpeg);
    }

    //этот метод не вызвать по адресу
    public async Task<byte[]> GetImageByStatusCode(int statusCode)
    {
        using HttpClient httpClient = new HttpClient();
        
        using HttpResponseMessage responseMessage = await httpClient.GetAsync(new Uri($"https://http.cat/{statusCode}.jpg"));
        
        var bytes = await responseMessage.Content.ReadAsByteArrayAsync();

        return bytes;
    }
}
