namespace FogSoft.LicenseGenerator
{
	public interface IProgress
	{
		int StepCount { get; set;}
		void StepCompleted();
		void WorkCompleted();
	}
}
