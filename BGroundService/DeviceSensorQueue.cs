using System.Threading.Channels;

namespace HMS_NewProject_Temp_Humdity_processdata.BGroundService
{
	public class DeviceSensorQueue
	{
		public Channel<string> DeviceSensors { get; } = Channel.CreateUnbounded<string>();

	}
}
