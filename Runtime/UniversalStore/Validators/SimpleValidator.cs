namespace UniStore
{
    public class SimpleValidator : AbstractValidator<ValidationResponse>
    {
        public SimpleValidator(string url) : base(url)
        {
        }

        protected override bool IsResponseValid(ValidationResponse response) => response?.Status == "0";
    }
}