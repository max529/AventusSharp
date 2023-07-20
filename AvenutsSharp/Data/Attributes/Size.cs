using System;

namespace AventusSharp.Data.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Size : ValidationAttribute
    {
        public int Nb { get; private set; }
        public bool Max { get; private set; }
        private string Msg { get; set; }
        /**
         * if nb = -1
         */
        public Size(int nb, string msg = "")
        {
            this.Nb = nb;
            if (nb <= 0)
            {
                Max = true;
            }
            this.Msg = msg;
        }
        public Size(bool max, string msg = "")
        {
            this.Max = max;
            if (!max)
            {
                Nb = 255;
            }
            this.Msg = msg;
        }

        public override ValidationResult IsValid(object? value, ValidationContext context)
        {
            if (value is string casted)
            {
                if (casted.Length > Nb)
                {
                    string msg = this.Msg == "" ? $"The field {context.FieldName} must be shorter than {Max} chars." : this.Msg;
                    return new ValidationResult(msg);
                }
            }
            return ValidationResult.Success;
        }
    }
}
