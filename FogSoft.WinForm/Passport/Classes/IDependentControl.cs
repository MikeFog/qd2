using System.Collections.Generic;

namespace FogSoft.WinForm.Passport.Classes
{
	public interface IDependentControl
	{
		void SetSourceControl(Dictionary<string, PageControl> controls);
	}
}