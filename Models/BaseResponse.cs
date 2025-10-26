namespace ClarkAI.Models
{
    public class BaseResponse
    {
        public string Message { get; set; }
        public bool IsSuccessful { get; set; }
    }

    public class BaseResponse<T>
    { 
        public string Message { get; set; }
        public bool IsSuccessful { get; set; }
        public T Value { get; set; }
    }

}
