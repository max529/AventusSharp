using System;

namespace AventusSharp.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ValidationAttribute : System.Attribute
    {
        public abstract ValidationResult IsValid(object? value, ValidationContext context);
    }

    public class ValidationContext
    {
        public string FieldName { get; set; }
        public Type FieldType { get; set; }

        public ValidationContext(string fieldName, Type fieldType)
        {
            FieldName = fieldName;
            FieldType = fieldType;
        }
    }

    public class ValidationResult
    {
        private static ValidationResult success = new("");
        internal string Msg { get; private set; }
        public static ValidationResult Success { get => success; set => success = value; }

        public ValidationResult(string msg)
        {
            this.Msg = msg;
        }
    }

}
