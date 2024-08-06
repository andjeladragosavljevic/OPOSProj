using DokanNet;

namespace OposFS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new OposFileSystem().Mount("Y:\\", DokanOptions.DebugMode | DokanOptions.StderrOutput);

        }
    }
}
