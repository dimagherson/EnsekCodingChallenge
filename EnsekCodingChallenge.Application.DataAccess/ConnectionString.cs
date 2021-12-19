namespace EnsekCodingChallenge.Application.DataAccess
{
    public class ConnectionString
    {
        public string Value { get; }

        public ConnectionString(string value)
        {
            Value = value;
        }

        public static implicit operator string(ConnectionString connectionString)
        {
            return connectionString?.Value;
        }
    }
}
