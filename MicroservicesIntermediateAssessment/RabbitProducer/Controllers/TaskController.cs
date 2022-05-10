using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitProducer.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RabbitProducer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        static readonly HttpClient client = new HttpClient();

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] TaskMessage taskMessage)
        {
            var factory = new ConnectionFactory()
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
            };

            Console.WriteLine(factory.HostName + ":" + factory.Port);

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "task",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                TokenResponse tokenObject;

                try
                {
                    var request = WebRequest.Create("https://reqres.in/api/register");
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        string json = JsonConvert.SerializeObject(new SendApi(taskMessage.Email, taskMessage.Password));
                        streamWriter.Write(json);
                    }

                    var httpResponse = (HttpWebResponse) request.GetResponse();

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        tokenObject = JsonConvert.DeserializeObject<TokenResponse>(result);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    return Unauthorized();
                }
                

                string message = JsonConvert.SerializeObject(taskMessage);

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "task",
                                     basicProperties: null,
                                     body: body); 

                return Ok(tokenObject);
            }
        }
    }
}
