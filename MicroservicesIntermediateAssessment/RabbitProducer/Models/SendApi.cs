namespace RabbitProducer.Models
{
    public class SendApi
    {
        public string username;
        public string password;

        public SendApi(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
    }
}
