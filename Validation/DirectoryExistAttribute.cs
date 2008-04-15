using System.Windows.Forms;

using JetBrains.CommonControls.Validation;

namespace ReSharper.Scout.Validation
{
	public class DirectoryExistsAttribute : ValidationAttribute
	{
		public DirectoryExistsAttribute(string message, ValidatorSeverity severity)
			: base(message, severity)
		{
		}

		public override IValidator BuildValidator(Control control, object host)
		{
			return ValidatorFactory.CreateTextValidator(control,
				Severity, Message, System.IO.Directory.Exists);
		}
	}
}
