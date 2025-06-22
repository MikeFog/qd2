using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FogSoft.WinForm.DataAccess;

namespace FogSoft.WinForm.Passport.Classes
{
	public class PassportException : ApplicationException
	{
		private const string ExceptionCode = "MandatoryFieldViolation";
		private PageControl pageControl;
		public readonly Control NativeControl;

		public PassportException(PageControl pageControl, Control nativeControl)
		{
			this.pageControl = pageControl;
			NativeControl = nativeControl;
		}

		public override string Message
		{
			get
			{
				MessageAccessor.Parameters = PrepareMessageParameters();
				return MessageAccessor.GetMessage(ExceptionCode);
			}
		}

		private Dictionary<string, object> PrepareMessageParameters()
		{
			Dictionary<string, object> msgParameters =
				new Dictionary<string, object>(1, StringComparer.InvariantCultureIgnoreCase);
			msgParameters.Add("controlName", pageControl.Caption);
			return msgParameters;
		}
	}
}