using System.ServiceProcess;

namespace CoffeeFreedom.Worker
{
    internal class Program
    {
        private static void Main()
        {
            ServiceBase.Run(new ServiceBase[] {new Service()});
        }
    }
}
